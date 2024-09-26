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
        #region Constructor
        public SorterManager() 
        { 
        }
        #endregion

        #region Sorters
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

        private Task<(bool,string)> SortDividedFiles(string sourceFilePath)
        {
            return Task.Run(async () =>
            {
                try
                {
                    var directoryName = Path.GetDirectoryName(sourceFilePath);
                    if (string.IsNullOrEmpty(directoryName))
                        return (false, "directoryName is null or empty");

                    string dividedFilesDirectory = Path.Combine(directoryName, Consts.FILE_SORTING_TEMP_FOLDER);
                    var files = Directory.GetFiles(dividedFilesDirectory);
                    var tasks = new List<Task>();
                    var bufferCounter = 0;
                  
                    for (int fileIndex = 0; fileIndex < files.Length; fileIndex++)
                    {
                        if (bufferCounter < Consts.NUMBER_OF_PARALLEL_READERS)
                        {
                            int capturedFileIndexI = fileIndex;
                            tasks.Add(Task.Run(() =>
                            {
                                SortDividedFile(files[capturedFileIndexI]);
                            }));
                            bufferCounter++;
                        }

                        if (bufferCounter == Consts.NUMBER_OF_PARALLEL_READERS ||
                            fileIndex == files.Length - 1)
                        {
                            await Task.WhenAll(tasks.ToArray());
                            tasks.Clear();
                            bufferCounter = 0;
                        }
                    }

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
        #endregion

        #region Mergers
        private Task<(bool,string)> MergeSortedFiles(string sourceFilePath)
        {
            return Task.Run(async () => 
            {
                try
                {
                    var directoryName = Path.GetDirectoryName(sourceFilePath);
                    if (string.IsNullOrEmpty(directoryName))
                        return (false, "directoryName is null or empty");

                    var dividedFilesDirectory = Path.Combine(directoryName, Consts.FILE_SORTING_TEMP_FOLDER);
                    var tasks = new List<Task>();
                    var bufferCounter = 0;

                    while (true)
                    {
                        var files = Directory.GetFiles(dividedFilesDirectory);
                        if (files.Length == 1)
                            break;

                        for (int fileIndex = 0; fileIndex < files.Length; fileIndex += 2)
                        {
                            if (bufferCounter < Consts.NUMBER_OF_PARALLEL_READERS &&
                                fileIndex < files.Length - 1)
                            {
                                int capturedI = fileIndex;
                                int capturedIplus1 = fileIndex + 1;
                                tasks.Add(Task.Run(() =>
                                {
                                    MergeSortedFiles(files[capturedI], files[capturedIplus1], Path.Combine(dividedFilesDirectory, $"merged_{Guid.NewGuid()}.txt"));
                                    File.Delete(files[capturedI]);
                                    File.Delete(files[capturedIplus1]);
                                }));
                                bufferCounter++;
                            }

                            if (bufferCounter == Consts.NUMBER_OF_PARALLEL_READERS ||
                                fileIndex + 2 >= files.Length - 1)
                            {
                                await Task.WhenAll(tasks.ToArray());
                                tasks.Clear();
                                bufferCounter = 0;
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
                var line1 = reader1.ReadLine();
                var line2 = reader2.ReadLine();

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
        #endregion

        #region Helpers
        private Task<(bool, string)> DivideFile(string sourceFilePath)
        {
            return Task.Run(() =>
            {
                try
                {
                    var directoryName = Path.GetDirectoryName(sourceFilePath);
                    if (string.IsNullOrEmpty(directoryName))
                        return (false, "directoryName is null or empty");

                    string dividedFilesDirectory = Path.Combine(directoryName, Consts.FILE_SORTING_TEMP_FOLDER);  // Number of lines per split file

                    if (Directory.Exists(dividedFilesDirectory))
                        Directory.Delete(dividedFilesDirectory, true);

                    // Ensure the output directory exists
                    Directory.CreateDirectory(dividedFilesDirectory);

                    // Open the input file for reading
                    using (StreamReader reader = new StreamReader(sourceFilePath))
                    {
                        int fileCounter = 1;
                        string? line;
                        int lineCount = 0;
                        StreamWriter? writer = null;

                        try
                        {
                            while ((line = reader.ReadLine()) != null)
                            {
                                if (lineCount % Consts.READ_BLOCK_SIZE == 0)
                                {
                                    if (writer != null)
                                    {
                                        writer.Close();
                                    }

                                    string outputFilePath = Path.Combine(dividedFilesDirectory, $"output_{Guid.NewGuid()}.txt");
                                    writer = new StreamWriter(outputFilePath);
                                    fileCounter++;
                                }

                                writer?.WriteLine(line);
                                lineCount++;
                            }
                        }
                        finally
                        {
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
                    var targetDirectory = Path.GetDirectoryName(sourceFilePath);
                    if (string.IsNullOrEmpty(targetDirectory))
                        return (false, "directoryName is null or empty");

                    var sourceDirectory = Path.Combine(targetDirectory, Consts.FILE_SORTING_TEMP_FOLDER);

                    var files = Directory.GetFiles(sourceDirectory);
                    if (files == null || !files.Any())
                        return (false, "There is no files in sourceDirectory");

                    var newFileName = $"{Path.GetFileNameWithoutExtension(sourceFilePath)}_sorted.txt";
                    var targetFilePath = Path.Combine(targetDirectory, newFileName);

                    File.Move(files.First(), targetFilePath);
                    Directory.Delete(sourceDirectory);

                    return (true, string.Empty);
                }
                catch(Exception ex)
                {
                    return (false, ex.Message);
                }
            });            
        }
        #endregion
    }
}
