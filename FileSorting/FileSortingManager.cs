using FileSorting.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSorting
{
    public sealed class FileSortingManager
    {
        private GeneratorManager generatorManager;
        private static readonly Lazy<FileSortingManager> lazy = new Lazy<FileSortingManager>(() => new FileSortingManager());
        public static FileSortingManager Instance => lazy.Value;

        private FileSortingManager()
        {
            generatorManager = new GeneratorManager();
        }

        public async Task<(bool, string)> GenerateFileAsync(string filePath, double fileSizeMB)
        {
            var result = await generatorManager.GenerateFileAsync(filePath, fileSizeMB);
            return result;
        }
    }
}
