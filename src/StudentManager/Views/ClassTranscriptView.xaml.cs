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
    }
}
