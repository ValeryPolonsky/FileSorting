using FileSorting.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSorting.Generator
{
    public class GeneratorManager
    {
        private readonly Random random;
        private readonly char[] letters;
        private readonly char[] digits;      

        public GeneratorManager()
        {
            random = new Random();
            //letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ ".ToCharArray();
            letters = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
            digits = "0123456789".ToCharArray();
        }

        public async Task<(bool, string)> GenerateFileAsync(string sourceFilePath, double fileSizeMB)
        {
            var result = await GenerateFileInternalAsync(sourceFilePath, fileSizeMB);
            if (!result.Item1) return result;

            result = await MergeDividedFiles(sourceFilePath);
            if (!result.Item1) return result;

            result = await MoveGeneratedFileToSourceLocation(sourceFilePath);
            return result;
        }

        private async Task<(bool, string)> GenerateFileInternalAsync(string sourceFilePath, double fileSizeMB)
        {
            var folderPath = Path.GetDirectoryName(sourceFilePath);
            var dividedFilesDirectory = Path.Combine(folderPath, Consts.FILE_SORTING_TEMP_FOLDER);
            var tasks = new List<Task>();

            if (Directory.Exists(dividedFilesDirectory))
                Directory.Delete(dividedFilesDirectory, true);

            Directory.CreateDirectory(dividedFilesDirectory);

            for (int i = 1; i <= Consts.NUMBER_OF_PARALLEL_WRITERS; i++)
            {
                var task = Task.Run(async () =>
                {
                    var outputFilePath = Path.Combine(dividedFilesDirectory, $"output_{Guid.NewGuid()}.txt");
                    await GenerateFileSingleAsync(outputFilePath, fileSizeMB / Consts.NUMBER_OF_PARALLEL_WRITERS);
                });
                tasks.Add(task);
            }
            await Task.WhenAll(tasks.ToArray());

            return (true, string.Empty);
        }

        private async Task<(bool,string)> GenerateFileSingleAsync(string filePath, double fileSizeMB)
        {
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
                
                while(true)
                {
                    var resultGetFileSize = GetFileSize(filePath);
                    if (resultGetFileSize.Item1)
                    {
                        if (resultGetFileSize.Item2 < fileSizeMB)
                        {
                            var blocks = (await GenerateFileBlockMultipleAsync(Consts.NUMBER_OF_PARALLEL_BLOCKS))
                                .ToList();
                            var blockSizeMB = GetBlockSizeMB(blocks);

                            if (blockSizeMB + resultGetFileSize.Item2 > fileSizeMB)
                            {
                                var subBlock = new List<string>();
                                for (int i = 0; i < blocks.Count; i += Consts.SUB_BLOCK_STEP)
                                {
                                    for (int j = i; j < i + Consts.SUB_BLOCK_STEP && j < blocks.Count; j++)
                                        subBlock.Add(blocks[j]);

                                    blockSizeMB = GetBlockSizeMB(subBlock);
                                    if (blockSizeMB + resultGetFileSize.Item2 > fileSizeMB)                                    
                                        break;                                   
                                }

                                blocks = subBlock;
                            }
                            
                            var resultWriteBlockToFile = await WriteBlockToFileAsync(filePath, blocks);
                            if (!resultWriteBlockToFile.Item1)
                                return (false, resultWriteBlockToFile.Item2);
                        }
                        else
                            return (true, string.Empty);
                    }
                    else
                        return (false, resultGetFileSize.Item3);
                }               
            }
            catch(Exception ex)
            {
                return (false, ex.ToString());
            }           
        }

        private (bool,double,string) GetFileSize(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists)
                {
                    long fileSizeInBytes = fileInfo.Length;
                    double fileSizeInKilobytes = fileSizeInBytes / Consts.KILOBYTE_SIZE;
                    double fileSizeInMegabytes = fileSizeInKilobytes / Consts.KILOBYTE_SIZE;
                    return (true, fileSizeInMegabytes, string.Empty);
                }
                else
                {
                    return (true, 0, string.Empty);
                }
            }
            catch (Exception ex)
            {
                return (false, 0, ex.ToString());
            }
        }

        private List<string> GenerateFileBlockSingle()
        {
            var stringList = new List<string>();
            var linesCounter = 0;
            while (linesCounter < Consts.WRITE_BLOCK_SIZE)
            {
                var lengthDigits = random.Next(1, Consts.DIGITS_MAX_LENGTH);
                var lengthLetters = random.Next(1, Consts.LELLERES_MAX_LENGTH);
                var numberOfRepetions = random.Next(1, Consts.REPETIONS_MAX);
                var randomString = GenerateRandomString(lengthLetters);

                for (int i = 0; i < numberOfRepetions && linesCounter < Consts.WRITE_BLOCK_SIZE; i++)
                {
                    var randomNumber = GenerateRandomNumber(lengthDigits);
                    var randomCombinedString = $"{randomNumber}. {randomString}";
                    stringList.Add(randomCombinedString);
                    linesCounter++;
                }
            }

            return stringList
            .OrderBy(line => Guid.NewGuid())
            .ToList();                      
        }

        private async Task<ConcurrentBag<string>> GenerateFileBlockMultipleAsync(int numberOfBlocks)
        {
            var tasks = new List<Task>();
            var allBlocks = new ConcurrentBag<string>();
            
            for (int i = 0; i < numberOfBlocks; i++)
            {
                var task = Task.Run(() =>
                {
                    foreach(var line in GenerateFileBlockSingle())
                        allBlocks.Add(line);
                });
                tasks.Add(task);          
            }

            await Task.WhenAll(tasks.ToArray());
            return allBlocks;
        }

        private double GetBlockSizeMB(IEnumerable<string> block)
        {
            var bytes = new List<byte>();
            foreach (var line in block)
            {
                bytes.AddRange(Encoding.UTF8.GetBytes(line));
            }

            var sizeInMB = (double)bytes.ToArray().Length / (Consts.KILOBYTE_SIZE * Consts.KILOBYTE_SIZE);
            return sizeInMB;         
        }

        private async Task<(bool,string)> WriteBlockToFileAsync(string filePath, IEnumerable<string> block)
        {
            try
            {
                await File.AppendAllLinesAsync(filePath, block);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.ToString());
            }
        }
     
        private string GenerateRandomString(int length)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                builder.Append(letters[random.Next(letters.Length)]);
            }

            return builder.ToString();
        }

        private string GenerateRandomNumber(int length)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                builder.Append(digits[random.Next(digits.Length)]);
            }

            return builder.ToString();
        }

        private Task<(bool, string)> MergeDividedFiles(string sourceFilePath)
        {
            return Task.Run(async () =>
            {
                try
                {
                    var dividedFilesDirectory = Path.Combine(Path.GetDirectoryName(sourceFilePath), Consts.FILE_SORTING_TEMP_FOLDER);
                    var tasks = new List<Task>();
                    var bufferCounter = 0;

                    while (true)
                    {
                        var files = Directory.GetFiles(dividedFilesDirectory);
                        if (files.Length == 1)
                            break;

                        for (int i = 0; i < files.Length; i += 2)
                        {
                            if (bufferCounter < Consts.NUMBER_OF_PARALLEL_READERS &&
                                i < files.Length - 1)
                            {
                                int capturedI = i;
                                int capturedIplus1 = i + 1;
                                tasks.Add(Task.Run(() =>
                                {
                                    MergeDividedFiles(files[capturedI], files[capturedIplus1], Path.Combine(dividedFilesDirectory, $"merged_{Guid.NewGuid()}.txt"));
                                    File.Delete(files[capturedI]);
                                    File.Delete(files[capturedIplus1]);
                                }));
                                bufferCounter++;
                            }

                            if (bufferCounter == Consts.NUMBER_OF_PARALLEL_READERS ||
                                i + 2 >= files.Length - 1)
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

        private void MergeDividedFiles(string file1Path, string file2Path, string outputFilePath)
        {
            using (var reader1 = new StreamReader(file1Path))
            using (var reader2 = new StreamReader(file2Path))
            using (var writer = new StreamWriter(outputFilePath))
            {
                string line1 = reader1.ReadLine();
                string line2 = reader2.ReadLine();

                while (line1 != null || line2 != null)
                {
                    if (line1 != null)                    
                        writer.WriteLine(line1);
                    
                    if (line2 != null)                   
                        writer.WriteLine(line2);

                    line1 = reader1.ReadLine();
                    line2 = reader2.ReadLine();
                }
            }
        }

        private Task<(bool, string)> MoveGeneratedFileToSourceLocation(string sourceFilePath)
        {
            return Task.Run(() =>
            {
                try
                {
                    var sourceDirectory = Path.Combine(Path.GetDirectoryName(sourceFilePath), Consts.FILE_SORTING_TEMP_FOLDER);
                    var targetDirectory = Path.GetDirectoryName(sourceFilePath);
                    var files = Directory.GetFiles(sourceDirectory);

                    var newFileName = $"{Path.GetFileNameWithoutExtension(sourceFilePath)}.txt";
                    var targetFilePath = Path.Combine(targetDirectory, newFileName);
                    File.Move(files.FirstOrDefault(), targetFilePath);
                    Directory.Delete(sourceDirectory);

                    return (true, string.Empty);
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            });
        }
    }
}
