using System.Windows;
using System.Windows.Controls;
using StudentManager.ViewModels;

namespace StudentManager.Views
{
    public partial class StaffProfileView : UserControl
    {
        public StaffProfileView()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is StaffProfileViewModel vm && sender is PasswordBox pb)
            {
                vm.ViewSalaryPassword = pb.Password;
            }
        }
    }
}
