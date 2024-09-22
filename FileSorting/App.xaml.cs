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
            var result = await FileSortingManager.Instance.GenerateFileAsync("C:\\MyProjects\\Data\\FileSorting1.txt",100);
            //result = await FileSortingManager.Instance.GenerateFileAsync("C:\\MyProjects\\Data\\FileSorting2.txt", 20);
            //result = await FileSortingManager.Instance.GenerateFileAsync("C:\\MyProjects\\Data\\FileSorting3.txt", 40);
            //result = await FileSortingManager.Instance.GenerateFileAsync("C:\\MyProjects\\Data\\FileSorting4.txt", 80);
            //result = await FileSortingManager.Instance.GenerateFileAsync("C:\\MyProjects\\Data\\FileSorting5.txt", 160);

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
