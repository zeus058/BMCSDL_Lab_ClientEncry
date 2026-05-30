using System;
using System.Windows;
using System.Windows.Input;
using StudentManager.Helpers;
using StudentManager.Views;

namespace StudentManager.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public static MainViewModel? Instance { get; private set; }

        private object? _currentView;
        public object? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        private string _breadcrumb = "Quản lý lớp";
        public string Breadcrumb
        {
            get => _breadcrumb;
            set => SetProperty(ref _breadcrumb, value);
        }

        private string _selectedNav = "Class";
        public string SelectedNav
        {
            get => _selectedNav;
            set => SetProperty(ref _selectedNav, value);
        }

        private bool _connectionOk;
        public bool ConnectionOk
        {
            get => _connectionOk;
            set => SetProperty(ref _connectionOk, value);
        }

        private string _toastMessage = "";
        public string ToastMessage
        {
            get => _toastMessage;
            set => SetProperty(ref _toastMessage, value);
        }

        private bool _toastVisible;
        public bool ToastVisible
        {
            get => _toastVisible;
            set => SetProperty(ref _toastVisible, value);
        }

        private bool _toastIsError;
        public bool ToastIsError
        {
            get => _toastIsError;
            set => SetProperty(ref _toastIsError, value);
        }

        public string CurrentUserName => CurrentUser.HOTEN;
        public string CurrentUserManv => CurrentUser.MANV;
        public string CurrentUserLogin => CurrentUser.TENDN;

        /// <summary>Lấy chữ cái đầu của từ cuối trong họ tên để hiển thị trên avatar.</summary>
        public string CurrentUserInitial
        {
            get
            {
                var name = CurrentUser.HOTEN?.Trim();
                if (string.IsNullOrEmpty(name)) return "?";
                var parts = name!.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                return parts[parts.Length - 1][0].ToString().ToUpper();
            }
        }

        public ICommand ShowClassCommand { get; }
        public ICommand ShowStudentCommand { get; }
        public ICommand ShowHocPhanCommand { get; }
        public ICommand ShowGradeCommand { get; }
        public ICommand ShowClassGradesCommand { get; }
        public ICommand ShowMonitorCommand { get; }
        public ICommand ShowEmployeeCommand { get; }
        public ICommand ShowAdminEmployeesCommand { get; }
        public ICommand ShowAdminSalaryCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand DismissToastCommand { get; }

        public MainViewModel()
        {
            Instance = this;

            ConnectionOk = DatabaseHelper.TestConnection();
            if (!ConnectionOk)
                PushToast("Không kết nối được tới cơ sở dữ liệu. Kiểm tra máy chủ, tên CSDL và chuỗi kết nối trong cấu hình ứng dụng.", true);

            ShowClassCommand = new RelayCommand(_ => Navigate(new ClassViewModel(), "Quản lý lớp", "Class"));
            ShowStudentCommand = new RelayCommand(_ => Navigate(new StudentViewModel(), "Quản lý sinh viên", "Student"));
            ShowHocPhanCommand = new RelayCommand(_ => Navigate(new CourseViewModel(), "Học phần", "Course"));
            ShowGradeCommand = new RelayCommand(_ => Navigate(new GradeEntryViewModel(), "Nhập bảng điểm", "GradeEntry"));
            ShowClassGradesCommand = new RelayCommand(_ => Navigate(new ClassTranscriptViewModel(), "Báo cáo điểm lớp", "ClassTranscript"));
            ShowMonitorCommand = new RelayCommand(_ => Navigate(new MonitorViewModel(), "Giám sát truy vấn", "Monitor"));
            ShowEmployeeCommand = new RelayCommand(_ => Navigate(new StaffProfileViewModel(), "Nhân viên", "StaffProfile"));
            ShowAdminEmployeesCommand = new RelayCommand(_ => Navigate(new AdminEmployeeListViewModel(), "Danh sách nhân viên", "AdminEmployees"));
            ShowAdminSalaryCommand = new RelayCommand(_ => Navigate(new AdminSalaryAdjustmentViewModel(), "Sửa lương", "AdminSalary"));
            LogoutCommand = new RelayCommand(_ => Logout());
            DismissToastCommand = new RelayCommand(_ => ToastVisible = false);

            if (IsAdmin)
            {
                Navigate(new AdminEmployeeListViewModel(), "Danh sách nhân viên", "AdminEmployees");
            }
            else
            {
                Navigate(new ClassViewModel(), "Quản lý lớp", "Class");
            }
        }

        public bool IsAdmin => CurrentUser.MANV == "ADMIN";
        public bool IsEmployee => !IsAdmin;

        public bool ClassIsActive => SelectedNav == "Class";
        public bool StudentIsActive => SelectedNav == "Student";
        public bool HocPhanIsActive => SelectedNav == "Course";
        public bool GradeIsActive => SelectedNav == "GradeEntry";
        public bool ClassGradesIsActive => SelectedNav == "ClassTranscript";
        public bool MonitorIsActive => SelectedNav == "Monitor";
        public bool EmployeeIsActive => SelectedNav == "StaffProfile";
        public bool AdminEmployeesIsActive => SelectedNav == "AdminEmployees";
        public bool AdminSalaryIsActive => SelectedNav == "AdminSalary";


        public void PushToast(string message, bool isError = false)
        {
            void Apply()
            {
                ToastMessage = message;
                ToastIsError = isError;
                ToastVisible = true;
            }

            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.Invoke(Apply);
            else
                Apply();
        }

        private void Navigate(object vm, string breadcrumb, string navId)
        {
            CurrentView = vm;
            Breadcrumb = breadcrumb;
            SelectedNav = navId;
            OnPropertyChanged(nameof(ClassIsActive));
            OnPropertyChanged(nameof(StudentIsActive));
            OnPropertyChanged(nameof(HocPhanIsActive));
            OnPropertyChanged(nameof(GradeIsActive));
            OnPropertyChanged(nameof(ClassGradesIsActive));
            OnPropertyChanged(nameof(MonitorIsActive));
            OnPropertyChanged(nameof(EmployeeIsActive));
            OnPropertyChanged(nameof(AdminEmployeesIsActive));
            OnPropertyChanged(nameof(AdminSalaryIsActive));
        }

        private void Logout()
        {
            Instance = null;
            CurrentUser.Clear();

            var login = new LoginView();
            login.Show();
            Application.Current.MainWindow?.Close();
            Application.Current.MainWindow = login;
        }
    }
}
