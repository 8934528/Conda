using System.Windows;

namespace Conda
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            var mainWindow = new MainWindow { WindowState = WindowState.Maximized };
            mainWindow.Show();
        }
    }
}
