using FileSorting.Common.Enums;
using FileSorting.Generator;
using FileSorting.Models;
using FileSorting.Sorter;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
            AvailableModes = new ObservableCollection<ProgramModeModel>();
            AvailableModes.Add(new ProgramModeModel
            {
                Description = "File Generator",
                Mode = ProgramMode.FileGenerator
            });
            AvailableModes.Add(new ProgramModeModel
            {
                Description = "File Sorter",
                Mode = ProgramMode.FileSorter
            });
        }

        public ObservableCollection<ProgramModeModel> AvailableModes { get; private set; }

        public string? OpenFolderBrowserDialog()
        {
            var folderDialog = new OpenFolderDialog
            {
                Title = "Select Folder",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            if (folderDialog.ShowDialog() == true)
            {
                var folderName = folderDialog.FolderName;
                return folderName;
            }

            return null;
        }

        public async Task<(bool, string)> GenerateFileAsync(string folderName, string fileName, double fileSizeMB)
        {
            var filePath = Path.Combine(folderName, $"{fileName}.txt");  
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
