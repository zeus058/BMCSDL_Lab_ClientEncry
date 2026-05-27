using System.Windows;
using StudentManager.ViewModels;

namespace StudentManager.Views
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
            DataContext = new RegisterViewModel();
        }
    }
}
