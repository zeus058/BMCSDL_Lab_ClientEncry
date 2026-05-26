using System.Windows;
using StudentManager.ViewModels;

namespace StudentManager.Views
{
    public partial class ChangePasswordWindow : Window
    {
        public ChangePasswordWindow()
        {
            InitializeComponent();
            DataContext = new ChangePasswordViewModel();
        }
    }
}
