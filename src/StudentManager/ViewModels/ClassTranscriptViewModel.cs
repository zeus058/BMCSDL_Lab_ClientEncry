using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows.Input;
using Dapper;
using StudentManager.Helpers;
using StudentManager.Models;

namespace StudentManager.ViewModels
{
    public class ClassTranscriptViewModel : ViewModelBase
    {
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
                if (SetProperty(ref _selectedStudent, value))
                {
                    LoadGrades();
                }
            }
        }

        private ObservableCollection<GradeDetail> _grades = new();
        public ObservableCollection<GradeDetail> Grades
        {
            get => _grades;
            set => SetProperty(ref _grades, value);
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand RefreshCommand { get; }

        public ClassTranscriptViewModel()
        {
            RefreshCommand = new RelayCommand(_ => LoadAll());
            LoadAll();
        }

        private void LoadAll()
        {
            LoadStudents();
            Grades.Clear();
            SelectedStudent = null;
        }

        private void LoadStudents()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                var list = conn.Query<SinhVien>(
                    "SP_SEL_SINHVIEN_BY_OWNER",
                    new { CALLER_MANV = CurrentUser.MANV },
                    commandType: CommandType.StoredProcedure).ToList();

                Students = new ObservableCollection<SinhVien>(list);
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForLoad(ex);
            }
        }

        private void LoadGrades()
        {
            if (SelectedStudent == null) return;

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                var list = conn.Query<GradeDetail>(
                    "SP_SEL_BANGDIEM_DETAILED_BY_STUDENT",
                    new { CALLER_MANV = CurrentUser.MANV, MASV = SelectedStudent.MASV },
                    commandType: CommandType.StoredProcedure).ToList();

                // Decrypt grades
                var privateKey = CryptoHelper.LoadPrivateKeyLocal(CurrentUser.MANV);
                foreach (var g in list)
                {
                    if (g.DIEMTHI == null || g.DIEMTHI.Length == 0)
                    {
                        g.DiemSo = "N/A";
                        continue;
                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(privateKey))
                        {
                            g.DiemSo = CryptoHelper.DecryptRSA(g.DIEMTHI!, privateKey!);
                        }
                        else
                        {
                            g.DiemSo = "(Khóa lỗi)";
                        }
                    }
                    catch
                    {
                        g.DiemSo = "(Lỗi giải mã)";
                    }
                }

                Grades = new ObservableCollection<GradeDetail>(list);
                StatusMessage = list.Count > 0 
                    ? $"Đã tải {list.Count} môn học cho sinh viên {SelectedStudent.HOTEN}." 
                    : $"Sinh viên {SelectedStudent.HOTEN} chưa có điểm.";
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForLoad(ex);
            }
        }
    }

    public class GradeDetail
    {
        public string MASV { get; set; } = "";
        public string MAHP { get; set; } = "";
        public string TENHP { get; set; } = "";
        public int SOTC { get; set; }
        public byte[]? DIEMTHI { get; set; }
        public string DiemSo { get; set; } = "";
    }
}
