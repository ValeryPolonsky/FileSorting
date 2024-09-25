using FileSorting.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FileSorting.Sorter
{
    public class SorterManager
    {
        public SorterManager() 
        { 
        }

        public async Task<(bool, string)> SortFile(string sourceFilePath)
        {
            var result = await DivideFile(sourceFilePath);
            if (!result.Item1) return result;
            
            result = await SortDividedFiles(sourceFilePath);
            if (!result.Item1) return result;
            
            result = await MergeSortedFiles(sourceFilePath);
            if (!result.Item1) return result;
            
            result = await MoveSortedFileToSourceLocation(sourceFilePath);
            return result;
        }

        private Task<(bool,string)> DivideFile(string sourceFilePath)
        {
            return Task.Run(() => 
            {
                try
                {
                    string folderPath = Path.GetDirectoryName(sourceFilePath);
                    string dividedFilesDirectory = Path.Combine(folderPath, Consts.FILE_SORTING_TEMP_FOLDER);  // Number of lines per split file

                    if (Directory.Exists(dividedFilesDirectory))
                        Directory.Delete(dividedFilesDirectory, true);

                    // Ensure the output directory exists
                    Directory.CreateDirectory(dividedFilesDirectory);

                    // Open the input file for reading
                    using (StreamReader reader = new StreamReader(sourceFilePath))
                    {
                        int fileCounter = 1;
                        string line;
                        int lineCount = 0;
                        StreamWriter writer = null;

                        try
                        {
                            while ((line = reader.ReadLine()) != null)
                            {
                                // Create a new file for every `linesPerFile` lines
                                if (lineCount % Consts.READ_BLOCK_SIZE == 0)
                                {
                                    if (writer != null)
                                    {
                                        writer.Close();
                                    }

                                    string outputFilePath = Path.Combine(dividedFilesDirectory, $"output_{fileCounter}.txt");
                                    writer = new StreamWriter(outputFilePath);
                                    fileCounter++;
                                }

                                writer.WriteLine(line);
                                lineCount++;
                            }
                        }
                        finally
                        {
                            // Ensure the last writer is closed properly
                            if (writer != null)
                            {
                                writer.Close();
                            }
                        }
                    }

                    return (true, string.Empty);
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            });                 
        }

        private Task<(bool,string)> SortDividedFiles(string sourceFilePath)
        {
            return Task.Run(async () =>
            {
                try
                {
                    string dividedFilesDirectory = Path.Combine(Path.GetDirectoryName(sourceFilePath), Consts.FILE_SORTING_TEMP_FOLDER);
                    var files = Directory.GetFiles(dividedFilesDirectory);
                    var semaphoreSlim = new SemaphoreSlim(Consts.NUMBER_OF_PARALLEL_READERS);
                    var tasks = files.Select(async file =>
                    {
                        await semaphoreSlim.WaitAsync();
                        try
                        {
                            SortDividedFile(file);
                        }
                        finally
                        {
                            semaphoreSlim.Release();
                        }
                    });

                    await Task.WhenAll(tasks);
                    return (true, string.Empty);
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            });          
        }

        private void SortDividedFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            var linesSplited = new List<Tuple<string, string>>();
            
            foreach(var line in lines)
            {
                var lineSplited = line.Split(". ");
                linesSplited.Add(new Tuple<string, string>(lineSplited[0], lineSplited[1]));
            }

            var sortedSplitedLines = linesSplited.OrderBy(ls => ls.Item2)
                .ThenBy(ls => ls.Item1)
                .ToList();

            var sortedLines = new List<string>();
            foreach (var line in sortedSplitedLines)           
                sortedLines.Add($"{line.Item1}. {line.Item2}");

            File.WriteAllLines(filePath, sortedLines);
        }  
        
        private Task<(bool,string)> MergeSortedFiles(string sourceFilePath)
        {
            return Task.Run(() => 
            {
                try
                {
                    var dividedFilesDirectory = Path.Combine(Path.GetDirectoryName(sourceFilePath), Consts.FILE_SORTING_TEMP_FOLDER);
                    var merged_files_counter = 1;

                    while (true)
                    {
                        var files = Directory.GetFiles(dividedFilesDirectory);
                        if (files.Length == 1)
                            break;

                        for (int i = 0; i < files.Length; i += 2)
                        {
                            if (i < files.Length - 1)
                            {
                                MergeSortedFiles(files[i], files[i + 1], Path.Combine(dividedFilesDirectory, $"merged_{merged_files_counter}.txt"));
                                File.Delete(files[i]);
                                File.Delete(files[i + 1]);
                                merged_files_counter++;
                            }
                        }
                    }

                    return (true, string.Empty);
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            });        
        }

        private void MergeSortedFiles(string file1Path, string file2Path, string outputFilePath)
        {
            using (var reader1 = new StreamReader(file1Path))
            using (var reader2 = new StreamReader(file2Path))
            using (var writer = new StreamWriter(outputFilePath))
            {
                string line1 = reader1.ReadLine();
                string line2 = reader2.ReadLine();

                while (line1 != null || line2 != null)
                {
                    if (line1 == null)
                    {
                        writer.WriteLine(line2);
                        line2 = reader2.ReadLine();
                    }
                    else if (line2 == null)
                    {
                        writer.WriteLine(line1);
                        line1 = reader1.ReadLine();
                    }
                    else
                    {
                        if (IsFirstLineSmaller(line1, line2))
                        {
                            writer.WriteLine(line1);
                            line1 = reader1.ReadLine();
                        }
                        else
                        {
                            writer.WriteLine(line2);
                            line2 = reader2.ReadLine();
                        }
                    }
                }
            }
        }

        private bool IsFirstLineSmaller(string line1, string line2)
        {
            // Split into number part and string part
            var split1 = line1.Split(new[] { ". " }, 2, StringSplitOptions.None);
            var split2 = line2.Split(new[] { ". " }, 2, StringSplitOptions.None);

            string textPart1 = split1[1];
            string textPart2 = split2[1];

            // First compare by string part
            int stringComparison = string.Compare(textPart1, textPart2, StringComparison.Ordinal);
            if (stringComparison != 0)
            {
                return stringComparison < 0;
            }

            // If string part is the same, compare by number part
            string numberPart1 = split1[0];
            string numberPart2 = split2[0];
            int numberComparison = string.Compare(numberPart1, numberPart2, StringComparison.Ordinal);
            return numberComparison < 0;           
        }

        private Task<(bool,string)> MoveSortedFileToSourceLocation(string sourceFilePath)
        {
            return Task.Run(() => 
            {
                try
                {
                    var sourceDirectory = Path.Combine(Path.GetDirectoryName(sourceFilePath), Consts.FILE_SORTING_TEMP_FOLDER);
                    var targetDirectory = Path.GetDirectoryName(sourceFilePath);
                    var files = Directory.GetFiles(sourceDirectory);

                    var newFileName = $"{Path.GetFileNameWithoutExtension(sourceFilePath)}_sorted.txt";
                    var targetFilePath = Path.Combine(targetDirectory, newFileName);
                    File.Move(files.FirstOrDefault(), targetFilePath);
                    Directory.Delete(sourceDirectory);

                    return (true, string.Empty);
                }
                catch(Exception ex)
                {
                    return (false, ex.Message);
                }
            });            
        }
    }
}
