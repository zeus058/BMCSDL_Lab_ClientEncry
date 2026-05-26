using System.Windows;
using System.Windows.Controls;
using StudentManager.ViewModels;

namespace StudentManager.Views
{
    public partial class ClassTranscriptView : UserControl
    {
        public ClassTranscriptView()
        {
            InitializeComponent();
            DataContext = new ClassTranscriptViewModel();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ClassTranscriptViewModel vm && sender is PasswordBox pb)
            {
                vm.DecryptPassword = pb.Password;
            }
        }
    }
}
