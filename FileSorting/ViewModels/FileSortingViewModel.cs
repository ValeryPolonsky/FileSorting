using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileSorting.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileSorting.ViewModels
{
    public class FileSortingViewModel:ObservableObject
    {
        private string? folderPath;
        private string? filePath;

        public FileSortingViewModel()
        {
            SelectedMode = AvailableModes.FirstOrDefault();
            Messages = new ObservableCollection<MessageModel>();

            SelectDirectoryCommand = new RelayCommand(SelectDirectoryCommandExecute, SelectDirectoryCommandCanExecute);
            GenerateFileCommand = new RelayCommand(GenerateFileCommandExecute, GenerateFileCommandCanExecute);
            SelectFileCommand = new RelayCommand(SelectFileCommandExecute, SelectFileCommandCanExecute);
            SortFileCommand = new RelayCommand(SortFileCommandExecute, SortFileCommandCanExecute);
        }

        public ObservableCollection<ProgramModeModel> AvailableModes => FileSortingManager.Instance.AvailableModes;

        private ObservableCollection<MessageModel> messages;
        public ObservableCollection<MessageModel> Messages
        {
            get => messages;
            set => SetProperty(ref messages, value);
        }
        
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
            set
            {
                SetProperty(ref generateFileName, value);
                GenerateFileCommand.NotifyCanExecuteChanged();
            }
        }

        private double? generateFileSizeMB;
        public double? GenerateFileSizeMB
        {
            get => generateFileSizeMB;
            set
            {
                SetProperty(ref generateFileSizeMB, value);
                GenerateFileCommand.NotifyCanExecuteChanged();
            }
        }

        public RelayCommand SelectDirectoryCommand { get; private set; }
        private void SelectDirectoryCommandExecute()
        {
            folderPath = FileSortingManager.Instance.OpenFolderBrowserDialog();
            AddMessage($"Folder [{folderPath}] selected");
            GenerateFileCommand.NotifyCanExecuteChanged();
        }
        private bool SelectDirectoryCommandCanExecute() => true;

        public RelayCommand GenerateFileCommand { get; private set; }
        private async void GenerateFileCommandExecute()
        {
            if (string.IsNullOrEmpty(folderPath) || 
                string.IsNullOrEmpty(GenerateFileName) ||
                GenerateFileSizeMB == null)
                return;

            var filePath = Path.Combine(folderPath, $"{GenerateFileName}.txt");
            AddMessage($"File [{filePath}] generation started");
            var result = await FileSortingManager.Instance.GenerateFileAsync(folderPath, GenerateFileName, GenerateFileSizeMB.Value);
            if (result.Item1)
                AddMessage($"File [{filePath}] generation finished successfully");
            else
                AddMessage($"File [{filePath}] generation failed, error: {result.Item2}");
        }
        private bool GenerateFileCommandCanExecute()
        {
            if (string.IsNullOrEmpty(folderPath) ||               
                string.IsNullOrEmpty(GenerateFileName) ||
                GenerateFileSizeMB == null ||
                GenerateFileSizeMB == 0.0)
                return false;

            return true;
        }

        public RelayCommand SelectFileCommand { get; private set; }
        private void SelectFileCommandExecute()
        {
            filePath = FileSortingManager.Instance.OpenFileBrowserDialog();
            AddMessage($"File [{filePath}] selected");
            SortFileCommand.NotifyCanExecuteChanged();
        }
        private bool SelectFileCommandCanExecute() => true;

        public RelayCommand SortFileCommand { get; private set; }
        private async void SortFileCommandExecute()
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            AddMessage($"File [{filePath}] sorting started");
            var result = await FileSortingManager.Instance.SortFileAsync(filePath);
            if (result.Item1)
                AddMessage($"File [{filePath}] sorting finished successfully");
            else
                AddMessage($"File [{filePath}] sorting failed, error: {result.Item2}");
        }
        private bool SortFileCommandCanExecute()
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            return true;
        }

        private void AddMessage(string content)
        {
            Messages.Add(new MessageModel 
            { 
                UpdateTime = DateTime.Now,
                Content = content
            });
        }
    }
}
