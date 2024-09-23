using FileSorting.Generator;
using FileSorting.Sorter;
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
        private SorterManager sorterManager;
        private static readonly Lazy<FileSortingManager> lazy = new Lazy<FileSortingManager>(() => new FileSortingManager());
        public static FileSortingManager Instance => lazy.Value;

        private FileSortingManager()
        {
            generatorManager = new GeneratorManager();
            sorterManager = new SorterManager();
        }

        public async Task<(bool, string)> GenerateFileAsync(string filePath, double fileSizeMB)
        {
            var result = await generatorManager.GenerateFileAsync(filePath, fileSizeMB);
            return result;
        }

        public async Task<(bool, string)> SortFileAsync(string filePath)
        {
            await sorterManager.SortFile(filePath);
            return (true, string.Empty);
        }
    }
}
