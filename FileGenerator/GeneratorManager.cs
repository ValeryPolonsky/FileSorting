using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileGenerator
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

        public void GenerateFile(string filePath)
        {

        }
     
        public string GenerateRandomString(int length)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                builder.Append(letters[random.Next(letters.Length)]);
            }

            return builder.ToString();
        }

        public string GenerateRandomNumber(int length)
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
