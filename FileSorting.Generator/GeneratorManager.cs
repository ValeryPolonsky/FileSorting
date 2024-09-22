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
            letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ ".ToCharArray();
            digits = "0123456789".ToCharArray();
        }

        public async Task<(bool,string)> GenerateFileAsync(string filePath, double fileSizeMB)
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
                            var blocks = await GenerateFileBlockMultipleAsync(Consts.NUMBER_OF_PARALLEL_BLOCKS);
                            var blockSizeMB = GetBlockSizeMB(blocks);

                            if (blockSizeMB + resultGetFileSize.Item2 > fileSizeMB)
                            {
                                var subBlock = new ConcurrentBag<string>();
                                foreach(var line in blocks)
                                {
                                    subBlock.Add(line);
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
            while (linesCounter < Consts.BLOCK_SIZE)
            {
                var lengthDigits = random.Next(1, Consts.DIGITS_MAX_LENGTH);
                var lengthLetters = random.Next(1, Consts.LELLERES_MAX_LENGTH);
                var numberOfRepetions = random.Next(1, Consts.REPETIONS_MAX);
                var randomString = GenerateRandomString(lengthLetters);

                for (int i = 0; i < numberOfRepetions && linesCounter < Consts.BLOCK_SIZE; i++)
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
    }
}
