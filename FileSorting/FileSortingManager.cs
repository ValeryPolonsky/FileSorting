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
        #region Members
        private GeneratorManager generatorManager;
        private SorterManager sorterManager;
        private static readonly Lazy<FileSortingManager> lazy = new Lazy<FileSortingManager>(() => new FileSortingManager());
        public static FileSortingManager Instance => lazy.Value;
        #endregion

        #region Constructor
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
        #endregion

        #region Properties
        public ObservableCollection<ProgramModeModel> AvailableModes { get; private set; }
        #endregion

        #region Dialogs
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

        public string? OpenFileBrowserDialog()
        {
            var folderDialog = new OpenFileDialog
            {
                Title = "Select Folder",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            if (folderDialog.ShowDialog() == true)
            {
                var fileName = folderDialog.FileName;
                return fileName;
            }

            return null;
        }
        #endregion

        #region Files processing
        public async Task<(bool, string)> GenerateFileAsync(string filePath, double fileSizeMB)
        {
            var result = await generatorManager.GenerateFileAsync(filePath, fileSizeMB);
            return result;
        }

        public async Task<(bool, string)> SortFileAsync(string filePath)
        {
            var result = await sorterManager.SortFile(filePath);
            return result;
        }
        #endregion
    }
}
