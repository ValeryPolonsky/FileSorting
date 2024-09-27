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
        #region Members
        private readonly Random random;
        private readonly char[] letters;
        private readonly char[] digits;
        #endregion

        #region Constructor
        public GeneratorManager()
        {
            random = new Random();
            letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ ".ToCharArray();
            digits = "0123456789".ToCharArray();
        }
        #endregion

        #region Generators
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
            try
            {
                var folderPath = Path.GetDirectoryName(sourceFilePath);
                if (string.IsNullOrEmpty(folderPath))
                    return (false, "folderPath is null or empty");

                var dividedFilesDirectory = Path.Combine(folderPath, Consts.FILE_SORTING_TEMP_FOLDER);
                var tasks = new List<Task>();

                if (Directory.Exists(dividedFilesDirectory))
                    Directory.Delete(dividedFilesDirectory, true);

                Directory.CreateDirectory(dividedFilesDirectory);

                for (int writerIndex = 1; writerIndex <= Consts.NUMBER_OF_PARALLEL_WRITERS; writerIndex++)
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
            catch(Exception ex)
            {
                return (false, ex.Message);
            }
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
                                for (int blockStep1 = 0; blockStep1 < blocks.Count; blockStep1 += Consts.SUB_BLOCK_STEP)
                                {
                                    for (int blockStep2 = blockStep1; blockStep2 < blockStep1 + Consts.SUB_BLOCK_STEP && blockStep2 < blocks.Count; blockStep2++)
                                        subBlock.Add(blocks[blockStep2]);

                                    blockSizeMB = GetBlockSizeMB(subBlock);
                                    while(blockSizeMB + resultGetFileSize.Item2 > fileSizeMB)
                                    {
                                        if (subBlock.Any())
                                            subBlock.RemoveAt(0);
                                        else
                                            break;

                                        blockSizeMB = GetBlockSizeMB(subBlock);
                                    }                                  
                                }

                                blocks = subBlock;
                            }

                            if (blocks.Any())
                            {
                                var resultWriteBlockToFile = await WriteBlockToFileAsync(filePath, blocks);
                                if (!resultWriteBlockToFile.Item1)
                                    return (false, resultWriteBlockToFile.Item2);
                            }
                            else
                                return (true, string.Empty);
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

                for (int repetition = 0; repetition < numberOfRepetions && linesCounter < Consts.WRITE_BLOCK_SIZE; repetition++)
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

            for (int block = 0; block < numberOfBlocks; block++)
            {
                var task = Task.Run(() =>
                {
                    foreach (var line in GenerateFileBlockSingle())
                        allBlocks.Add(line);
                });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks.ToArray());
            return allBlocks;
        }

        private string GenerateRandomString(int length)
        {
            var builder = new StringBuilder();

            for (int charIndex = 0; charIndex < length; charIndex++)
                builder.Append(letters[random.Next(letters.Length)]);

            return builder.ToString();
        }

        private string GenerateRandomNumber(int length)
        {
            var builder = new StringBuilder();

            for (int charIndex = 0; charIndex < length; charIndex++)
                builder.Append(digits[random.Next(digits.Length)]);

            return builder.ToString();
        }
        #endregion

        #region Mergers
        private Task<(bool, string)> MergeDividedFiles(string sourceFilePath)
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
                                int capturedFileIndexI = fileIndex;
                                int capturedFileIndexIplus1 = fileIndex + 1;
                                tasks.Add(Task.Run(() =>
                                {
                                    MergeDividedFiles(files[capturedFileIndexI], files[capturedFileIndexIplus1], Path.Combine(dividedFilesDirectory, $"merged_{Guid.NewGuid()}.txt"));
                                    File.Delete(files[capturedFileIndexI]);
                                    File.Delete(files[capturedFileIndexIplus1]);
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

        private void MergeDividedFiles(string file1Path, string file2Path, string outputFilePath)
        {
            using (var reader1 = new StreamReader(file1Path))
            using (var reader2 = new StreamReader(file2Path))
            using (var writer = new StreamWriter(outputFilePath))
            {
                var line1 = reader1.ReadLine();
                var line2 = reader2.ReadLine();

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
        #endregion

        #region Helpers
        private Task<(bool, string)> MoveGeneratedFileToSourceLocation(string sourceFilePath)
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

                    var newFileName = $"{Path.GetFileNameWithoutExtension(sourceFilePath)}.txt";
                    var targetFilePath = Path.Combine(targetDirectory, newFileName);

                    File.Move(files.First(), targetFilePath);
                    Directory.Delete(sourceDirectory);

                    return (true, string.Empty);
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            });
        }

        private async Task<(bool, string)> WriteBlockToFileAsync(string filePath, IEnumerable<string> block)
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

        private (bool, double, string) GetFileSize(string filePath)
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

        private double GetBlockSizeMB(IEnumerable<string> block)
        {
            var bytes = new List<byte>();
            foreach (var line in block)          
                bytes.AddRange(Encoding.UTF8.GetBytes(line));
            
            var sizeInMB = (double)bytes.ToArray().Length / (Consts.KILOBYTE_SIZE * Consts.KILOBYTE_SIZE);
            return sizeInMB;
        }
        #endregion
    }
}
