using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileSorting.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSorting.ViewModels
{
    public class FileSortingViewModel:ObservableObject
    {
        private string? folderPath;

        public FileSortingViewModel()
        {
            SelectDirectoryCommand = new RelayCommand(SelectDirectoryCommandExecute, SelectDirectoryCommandCanExecute);
            GenerateFileCommand = new RelayCommand(GenerateFileCommandExecute, GenerateFileCommandCanExecute);
        }

        public ObservableCollection<ProgramModeModel> AvailableModes => FileSortingManager.Instance.AvailableModes;
        
        private ProgramModeModel selectedMode;
        public ProgramModeModel SelectedMode
        {
            get => selectedMode;
            set => SetProperty(ref selectedMode, value);
        }

        private string generateFileName;
        public string GenerateFileName
        {
            get => generateFileName;
            set => SetProperty(ref generateFileName, value);
        }

        private double generateFileSizeMB;
        public double GenerateFileSizeMB
        {
            get => generateFileSizeMB;
            set => SetProperty(ref generateFileSizeMB, value);
        }

        public RelayCommand SelectDirectoryCommand { get; private set; }
        private void SelectDirectoryCommandExecute()
        {
            folderPath = FileSortingManager.Instance.OpenFolderBrowserDialog();                  
        }
        private bool SelectDirectoryCommandCanExecute() => true;

        public RelayCommand GenerateFileCommand { get; private set; }
        private void GenerateFileCommandExecute()
        {
            if (string.IsNullOrEmpty(folderPath))
                return;

            var result = FileSortingManager.Instance.GenerateFileAsync(folderPath, GenerateFileName, GenerateFileSizeMB);
        }
        private bool GenerateFileCommandCanExecute() => true;
    }
}
