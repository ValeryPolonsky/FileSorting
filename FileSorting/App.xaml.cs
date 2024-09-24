using FileSorting.ViewModels;
using System.Configuration;
using System.Data;
using System.Windows;

namespace FileSorting
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var fileSortingViewModel = new FileSortingViewModel();
            var mainWindow = new FileSortingWindow();
            mainWindow.DataContext = fileSortingViewModel;
            mainWindow.Show();
        }
    }
}
