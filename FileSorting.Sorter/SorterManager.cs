using FileSorting.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSorting.Sorter
{
    public class SorterManager
    {
        public SorterManager() 
        { 
        }

        public async Task SortFile(string filePath)
        {
            await DivideFile(filePath);
            await SortDividedFiles(filePath);
        }

        private Task DivideFile(string filePath)
        {
            return Task.Run(() => 
            {
                string folderPath = Path.GetDirectoryName(filePath);
                string dividedFilesDirectory = Path.Combine(folderPath, Consts.FILE_SORTING_TEMP_FOLDER);  // Number of lines per split file

                if (Directory.Exists(dividedFilesDirectory))
                    Directory.Delete(dividedFilesDirectory, true);

                // Ensure the output directory exists
                Directory.CreateDirectory(dividedFilesDirectory);

                // Open the input file for reading
                using (StreamReader reader = new StreamReader(filePath))
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
            });           
        }

        private async Task SortDividedFiles(string filePath)
        {
            string dividedFilesDirectory = Path.Combine(Path.GetDirectoryName(filePath), Consts.FILE_SORTING_TEMP_FOLDER);
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
        }

        private void SortDividedFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            var linesSplited = new List<Tuple<string, string>>();
            
            foreach(var line in lines)
            {
                var lineSplited = line.Split('.');
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

        public LineOrder CompareLines(string line1, string line2)
        {
            var splitedLine1 = line1.Split('.');
            var splitedLine2 = line2.Split('.');

            var stringPartOrderResult = string.Compare(splitedLine1[1], 
                splitedLine2[1], 
                StringComparison.Ordinal);
            var stringPartOrder = LineOrder.Unknown;
            var numberPartOrder = LineOrder.Unknown;
            
            if (stringPartOrderResult < 0)
                stringPartOrder = LineOrder.Line1BeforeLine2;        
            else if (stringPartOrderResult > 0)
                stringPartOrder = LineOrder.Line2BeforeLine1;           
            else
                stringPartOrder = LineOrder.Line1AndLine2AreEqual;

            if (stringPartOrder != LineOrder.Line1AndLine2AreEqual)
                return stringPartOrder;
            else
            {
                var numberPartOrderResult = string.Compare(splitedLine1[0],
                splitedLine2[0],
                StringComparison.Ordinal);

                if (numberPartOrderResult < 0)
                    numberPartOrder = LineOrder.Line1BeforeLine2;
                else if (numberPartOrderResult > 0)
                    numberPartOrder = LineOrder.Line2BeforeLine1;
                else
                    numberPartOrder = LineOrder.Line1AndLine2AreEqual;

                return numberPartOrder;
            }
        }
    }
}
