using System.Windows;
using StudentManager.Helpers;
using StudentManager.Views;

namespace StudentManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            base.OnStartup(e);

            var login = new LoginView();
            MainWindow = login;
            login.Show();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                UserFacingMessage.ForUnhandledAppError(),
                "Thông báo",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            e.Handled = true;
        }
    }
}
