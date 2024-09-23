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
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            //var result = await FileSortingManager.Instance.GenerateFileAsync("C:\\MyProjects\\Data\\FileSorting.txt",300);
            var result = await FileSortingManager.Instance.SortFileAsync("C:\\MyProjects\\Data\\FileSorting.txt");

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
