using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Dapper;
using StudentManager.Helpers;
using StudentManager.Models;

namespace StudentManager.ViewModels
{
    public class StudentViewModel : ViewModelBase
    {
        private List<SinhVien> _allStudents = new();

        private ObservableCollection<Lop> _myClasses = new();
        public ObservableCollection<Lop> MyClasses
        {
            get => _myClasses;
            set => SetProperty(ref _myClasses, value);
        }

        private Lop? _selectedClass;
        public Lop? SelectedClass
        {
            get => _selectedClass;
            set
            {
                if (!SetProperty(ref _selectedClass, value)) return;

                if (SelectedStudent == null)
                    EditMalop = value?.MALOP ?? "";

                LoadStudentsForClass();
            }
        }

        private ObservableCollection<SinhVien> _students = new();
        public ObservableCollection<SinhVien> Students
        {
            get => _students;
            set => SetProperty(ref _students, value);
        }

        private SinhVien? _selectedStudent;
        public SinhVien? SelectedStudent
        {
            get => _selectedStudent;
            set
            {
                if (!SetProperty(ref _selectedStudent, value)) return;

                if (value == null)
                {
                    ResetEditorForNewStudent();
                }
                else
                {
                    EditMasv = value.MASV ?? "";
                    EditHoten = value.HOTEN ?? "";
                    EditNgaysinh = value.NGAYSINH;
                    EditDiachi = value.DIACHI ?? "";
                    EditMalop = value.MALOP ?? "";
                    EditTendn = value.TENDN ?? "";
                    EditMatKhau = "";
                }

                OnPropertyChanged(nameof(IsMasvLocked));
                OnPropertyChanged(nameof(CanChangeStudentClass));
            }
        }

        public bool IsMasvLocked => SelectedStudent != null;

        /// <summary>Không cho đổi lớp khi sửa sinh viên đã tồn tại (chỉ thêm mới mới chọn lớp).</summary>
        public bool CanChangeStudentClass => SelectedStudent == null;

        private string _editMasv = "";
        public string EditMasv
        {
            get => _editMasv;
            set => SetProperty(ref _editMasv, value);
        }

        private string _editHoten = "";
        public string EditHoten
        {
            get => _editHoten;
            set => SetProperty(ref _editHoten, value);
        }

        private DateTime? _editNgaysinh;
        public DateTime? EditNgaysinh
        {
            get => _editNgaysinh;
            set => SetProperty(ref _editNgaysinh, value);
        }

        private string _editDiachi = "";
        public string EditDiachi
        {
            get => _editDiachi;
            set => SetProperty(ref _editDiachi, value);
        }

        private string _editMalop = "";
        public string EditMalop
        {
            get => _editMalop;
            set => SetProperty(ref _editMalop, value);
        }

        private string _editTendn = "";
        public string EditTendn
        {
            get => _editTendn;
            set => SetProperty(ref _editTendn, value);
        }

        private string _editMatKhau = "";
        public string EditMatKhau
        {
            get => _editMatKhau;
            set => SetProperty(ref _editMatKhau, value);
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (!SetProperty(ref _searchText, value)) return;
                ApplySearchFilter();
            }
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand NewStudentCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }

        public StudentViewModel()
        {
            RefreshCommand = new RelayCommand(_ => LoadClasses());
            NewStudentCommand = new RelayCommand(_ => PrepareNewStudent(), _ => SelectedClass != null);
            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
            DeleteCommand = new RelayCommand(_ => Delete(), _ => CanDelete());
            LoadClasses();
        }

        private bool CanDelete()
        {
            if (SelectedStudent == null) return false;
            var targetClass = MyClasses.FirstOrDefault(c => c.MALOP == SelectedStudent.MALOP);
            return targetClass != null && targetClass.MANV == CurrentUser.MANV;
        }
        private bool CanSave()
        {
            if (string.IsNullOrWhiteSpace(EditMasv) ||
                string.IsNullOrWhiteSpace(EditHoten) ||
                string.IsNullOrWhiteSpace(EditMalop) ||
                string.IsNullOrWhiteSpace(EditTendn))
                return false;

            // Only homeroom teachers can edit students in their class
            var targetClass = MyClasses.FirstOrDefault(c => c.MALOP == EditMalop);
            if (targetClass == null || targetClass.MANV != CurrentUser.MANV)
                return false;

            if (SelectedStudent == null && string.IsNullOrWhiteSpace(EditMatKhau))
                return false;

            return true;
        }

        private void LoadClasses()
        {
            var currentClassId = SelectedClass?.MALOP;

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                var classes = conn.Query<Lop>(
                    "SELECT * FROM LOP",
                    commandType: CommandType.Text).ToList();

                MyClasses = new ObservableCollection<Lop>(classes);
                SelectedClass = classes.FirstOrDefault(item => item.MALOP == currentClassId) ?? classes.FirstOrDefault();
                DatabaseHelper.LogQuery("SELECT * FROM LOP");
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForLoad(ex);
            }
        }

        private void LoadStudentsForClass()
        {
            if (SelectedClass == null)
            {
                _allStudents = new List<SinhVien>();
                Students = new ObservableCollection<SinhVien>();
                ResetEditorForNewStudent();
                return;
            }

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                _allStudents = conn.Query<SinhVien>(
                    "SELECT * FROM SINHVIEN WHERE MALOP = @MALOP",
                    new
                    {
                        MALOP = SelectedClass.MALOP
                    },
                    commandType: CommandType.Text).ToList();

                DatabaseHelper.LogQuery("SELECT * FROM SINHVIEN", new { MALOP = SelectedClass.MALOP });
                ApplySearchFilter();

                if (SelectedStudent == null)
                    ResetEditorForNewStudent();
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForLoad(ex);
            }
        }

        private void ApplySearchFilter()
        {
            var filtered = _allStudents.Where(s =>
                string.IsNullOrEmpty(SearchText) ||
                (s.MASV?.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (s.HOTEN?.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (s.TENDN?.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
            ).ToList();

            Students = new ObservableCollection<SinhVien>(filtered);
        }

        private void PrepareNewStudent()
        {
            SelectedStudent = null;
            ResetEditorForNewStudent();
            StatusMessage = "Vui lòng nhập thông tin chi tiết của sinh viên.";
        }

        private void ResetEditorForNewStudent()
        {
            EditMasv = "";
            EditHoten = "";
            EditNgaysinh = null;
            EditDiachi = "";
            EditMalop = SelectedClass?.MALOP ?? MyClasses.FirstOrDefault()?.MALOP ?? "";
            EditTendn = "";
            EditMatKhau = "";
            OnPropertyChanged(nameof(IsMasvLocked));
            OnPropertyChanged(nameof(CanChangeStudentClass));
        }

        private void Save()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                if (SelectedStudent == null)
                {
                    byte[] hashedPw = CryptoHelper.Sha1(EditTendn.Trim() + "|" + EditMatKhau);

                    conn.Execute(
                        "SP_INS_SINHVIEN",
                        new
                        {
                            CALLER_MANV = CurrentUser.MANV,
                            MASV = EditMasv.Trim(),
                            HOTEN = EditHoten.Trim(),
                            NGAYSINH = EditNgaysinh,
                            DIACHI = EditDiachi.Trim(),
                            MALOP = EditMalop,
                            TENDN = EditTendn.Trim(),
                            MK = hashedPw
                        },
                        commandType: CommandType.StoredProcedure);

                    DatabaseHelper.LogQuery("EXEC SP_INS_SINHVIEN", new { MASV = EditMasv.Trim() });
                    StatusMessage = "Đã thêm sinh viên mới.";
                }
                else
                {
                    byte[]? hashedPw = null;
                    if (!string.IsNullOrWhiteSpace(EditMatKhau))
                    {
                        hashedPw = CryptoHelper.Sha1(EditTendn.Trim() + "|" + EditMatKhau);
                    }

                    var sqlUpdate = @"
                        UPDATE SINHVIEN 
                        SET HOTEN = @HOTEN, 
                            NGAYSINH = @NGAYSINH, 
                            DIACHI = @DIACHI, 
                            TENDN = @TENDN" 
                        + (string.IsNullOrWhiteSpace(EditMatKhau) ? "" : ", MATKHAU = @MK") +
                        " WHERE MASV = @MASV";

                    conn.Execute(
                        sqlUpdate,
                        new
                        {
                            MASV = EditMasv.Trim(),
                            HOTEN = EditHoten.Trim(),
                            NGAYSINH = EditNgaysinh,
                            DIACHI = EditDiachi.Trim(),
                            TENDN = EditTendn.Trim(),
                            MK = hashedPw
                        },
                        commandType: CommandType.Text);

                    DatabaseHelper.LogQuery("UPDATE SINHVIEN", new { MASV = EditMasv.Trim() });
                    StatusMessage = "Đã cập nhật thông tin sinh viên.";
                }

                LoadStudentsForClass();
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForSave(ex);
            }
        }

        private void Delete()
        {
            if (SelectedStudent == null)
                return;

            if (MessageBox.Show(
                    $"Xóa sinh viên {SelectedStudent.MASV}?",
                    "Xác nhận",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Execute(
                    "SP_DEL_SINHVIEN_BY_OWNER",
                    new
                    {
                        CALLER_MANV = CurrentUser.MANV,
                        MASV = SelectedStudent.MASV
                    },
                    commandType: CommandType.StoredProcedure);

                DatabaseHelper.LogQuery("EXEC SP_DEL_SINHVIEN_BY_OWNER", new { MASV = SelectedStudent.MASV });
                SelectedStudent = null;
                StatusMessage = "Đã xóa sinh viên khỏi hệ thống.";
                LoadStudentsForClass();
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForDelete(ex);
            }
        }
    }
}
