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

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source == sender && DataContext is StaffProfileViewModel vm)
            {
                if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem selectedTab && selectedTab.Header?.ToString() == "Danh sách nhân viên")
                {
                    vm.LoadEmployeesCommand.Execute(null);
                }
            }
        }
    }
}
