using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Dapper;
using StudentManager.Helpers;
using StudentManager.Models;

namespace StudentManager.ViewModels
{
    public class GradeEntryViewModel : ViewModelBase
    {
        private ObservableCollection<SinhVien> _students = new();
        public ObservableCollection<SinhVien> Students
        {
            get => _students;
            set => SetProperty(ref _students, value);
        }

        private ObservableCollection<HocPhan> _hocPhans = new();
        public ObservableCollection<HocPhan> HocPhans
        {
            get => _hocPhans;
            set => SetProperty(ref _hocPhans, value);
        }

        private ObservableCollection<GradeLogItem> _historyLog = new();
        public ObservableCollection<GradeLogItem> HistoryLog
        {
            get => _historyLog;
            set => SetProperty(ref _historyLog, value);
        }

        private SinhVien? _selectedStudent;
        public SinhVien? SelectedStudent
        {
            get => _selectedStudent;
            set
            {
                if (SetProperty(ref _selectedStudent, value))
                {
                    EditMasv = value?.MASV ?? "";
                }
            }
        }

        private HocPhan? _selectedHocPhan;
        public HocPhan? SelectedHocPhan
        {
            get => _selectedHocPhan;
            set
            {
                if (SetProperty(ref _selectedHocPhan, value))
                {
                    EditMahp = value?.MAHP ?? "";
                }
            }
        }

        private string _editMasv = "";
        public string EditMasv { get => _editMasv; set => SetProperty(ref _editMasv, value); }

        private string _editMahp = "";
        public string EditMahp { get => _editMahp; set => SetProperty(ref _editMahp, value); }

        private string _editDiem = "";
        public string EditDiem { get => _editDiem; set => SetProperty(ref _editDiem, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand SaveGradeCommand { get; }

        public GradeEntryViewModel()
        {
            SaveGradeCommand = new RelayCommand(_ => SaveGrade(), _ => CanSave());
            LoadInitialData();
        }

        private bool CanSave() =>
            !string.IsNullOrWhiteSpace(EditMasv) &&
            !string.IsNullOrWhiteSpace(EditMahp) &&
            !string.IsNullOrWhiteSpace(EditDiem);

        private void LoadInitialData()
        {
            LoadStudents();
            LoadHocPhans();
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

        private void LoadHocPhans()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                var list = conn.Query<HocPhan>("SP_SEL_HOCPHAN", commandType: CommandType.StoredProcedure).ToList();
                HocPhans = new ObservableCollection<HocPhan>(list);
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForLoad(ex);
            }
        }

        private void SaveGrade()
        {
            var student = Students.FirstOrDefault(s => s.MASV == EditMasv.Trim());
            if (student == null)
            {
                StatusMessage = "Không tìm thấy sinh viên với mã đã nhập.";
                return;
            }

            var course = HocPhans.FirstOrDefault(c => c.MAHP == EditMahp.Trim());
            if (course == null)
            {
                StatusMessage = "Mã học phần không hợp lệ.";
                return;
            }

            if (!TryParseGrade(EditDiem, out var score))
            {
                StatusMessage = "Điểm phải từ 0 đến 10.";
                return;
            }

            try
            {
                var publicKeyXml = CurrentUser.PUBKEY;
                if (string.IsNullOrWhiteSpace(publicKeyXml))
                {
                    StatusMessage = "Chưa có khóa công khai. Vui lòng đăng nhập lại.";
                    return;
                }

                var normalizedScore = score.ToString("0.##", CultureInfo.InvariantCulture);
                var cipher = CryptoHelper.EncryptRSA(normalizedScore, publicKeyXml);

                using var conn = DatabaseHelper.GetConnection();
                conn.Execute(
                    "SP_UPSERT_BANGDIEM",
                    new
                    {
                        CALLER_MANV = CurrentUser.MANV,
                        MASV = EditMasv.Trim(),
                        MAHP = EditMahp.Trim(),
                        DIEMTHI = cipher
                    },
                    commandType: CommandType.StoredProcedure);

                // Add to history log
                HistoryLog.Insert(0, new GradeLogItem
                {
                    Masv = student.MASV,
                    HotenSv = student.HOTEN,
                    Mahp = course.MAHP,
                    Tenhp = course.TENHP,
                    Diem = normalizedScore,
                    Timestamp = DateTime.Now.ToString("HH:mm:ss")
                });

                StatusMessage = $"Đã nhập điểm {normalizedScore} cho SV {student.HOTEN} môn {course.TENHP}.";
                EditDiem = ""; // Clear score for next input
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForSave(ex);
            }
        }

        private static bool TryParseGrade(string input, out decimal value)
        {
            var ok = decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out value) ||
                     decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
            return ok && value >= 0m && value <= 10m;
        }
    }

    public class GradeLogItem
    {
        public string Masv { get; set; } = "";
        public string HotenSv { get; set; } = "";
        public string Mahp { get; set; } = "";
        public string Tenhp { get; set; } = "";
        public string Diem { get; set; } = "";
        public string Timestamp { get; set; } = "";
    }
}
