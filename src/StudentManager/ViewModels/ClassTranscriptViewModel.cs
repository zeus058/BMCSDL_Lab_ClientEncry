using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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

        private string _decryptPassword = "";
        public string DecryptPassword
        {
            get => _decryptPassword;
            set => SetProperty(ref _decryptPassword, value);
        }

        private bool _isDecrypted;
        public bool IsDecrypted
        {
            get => _isDecrypted;
            set => SetProperty(ref _isDecrypted, value);
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Cache the private key XML after first verification to make subsequent decryptions instantaneous
        private string? _cachedPrivateKeyXml;

        public ICommand RefreshCommand { get; }
        public ICommand DecryptGradesCommand { get; }

        public ClassTranscriptViewModel()
        {
            RefreshCommand = new RelayCommand(_ => LoadAll());
            DecryptGradesCommand = new RelayCommand(_ => DecryptGrades(), _ => !string.IsNullOrWhiteSpace(DecryptPassword));
            LoadAll();
        }

        private void LoadAll()
        {
            IsDecrypted = false;
            DecryptPassword = "";
            _cachedPrivateKeyXml = null;
            LoadStudents();
            Grades.Clear();
            SelectedStudent = null;
        }

        private async void LoadStudents()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                var list = (await conn.QueryAsync<SinhVien>(
                    "SP_SEL_SINHVIEN_BY_OWNER",
                    new { CALLER_MANV = CurrentUser.MANV },
                    commandType: CommandType.StoredProcedure)).ToList();

                Students = new ObservableCollection<SinhVien>(list);
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForLoad(ex);
            }
        }

        private async void LoadGrades()
        {
            if (SelectedStudent == null) return;

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                var list = (await conn.QueryAsync<GradeDetail>(
                    "SP_SEL_BANGDIEM_DETAILED_BY_STUDENT",
                    new { CALLER_MANV = CurrentUser.MANV, MASV = SelectedStudent.MASV },
                    commandType: CommandType.StoredProcedure)).ToList();

                // If password was already verified, decrypt the grades asynchronously
                if (IsDecrypted && !string.IsNullOrEmpty(_cachedPrivateKeyXml))
                {
                    string keyXml = _cachedPrivateKeyXml ?? "";
                    await Task.Run(() =>
                    {
                        foreach (var g in list)
                        {
                            if (g.DIEMTHI == null || g.DIEMTHI.Length == 0)
                            {
                                g.DiemSo = "N/A";
                            }
                            else
                            {
                                try
                                {
                                    g.DiemSo = CryptoHelper.DecryptRSA(g.DIEMTHI!, keyXml);
                                }
                                catch
                                {
                                    g.DiemSo = "(Lỗi giải mã)";
                                }
                            }
                        }
                    });
                }
                else
                {
                    foreach (var g in list)
                    {
                        if (g.DIEMTHI == null || g.DIEMTHI.Length == 0)
                            g.DiemSo = "N/A";
                        else
                            g.DiemSo = "●●●";
                    }
                }

                Grades = new ObservableCollection<GradeDetail>(list);
                StatusMessage = list.Count > 0
                    ? IsDecrypted
                        ? $"Đã tải và giải mã {list.Count} môn học cho sinh viên {SelectedStudent.HOTEN}."
                        : $"Đã tải {list.Count} môn học. Nhập mật khẩu để giải mã điểm."
                    : $"Sinh viên {SelectedStudent.HOTEN} chưa có điểm.";
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForLoad(ex);
            }
        }

        private async void DecryptGrades()
        {
            if (string.IsNullOrWhiteSpace(DecryptPassword))
            {
                StatusMessage = "Vui lòng nhập mật khẩu để giải mã.";
                return;
            }

            try
            {
                StatusMessage = "Đang sinh cặp khóa bảo mật tại Client (vui lòng đợi)...";
                string password = DecryptPassword;

                // 1. Generate keys asynchronously to avoid UI freezing
                var keys = await Task.Run(() => CryptoHelper.GenerateDeterministicKeyPair(password, CurrentUser.MANV));

                // 2. Validate the key against stored public key (indirect authentication)
                if (keys.PublicKeyXml != CurrentUser.PUBKEY)
                {
                    StatusMessage = "Mật khẩu không đúng. Khóa công khai không khớp.";
                    IsDecrypted = false;
                    _cachedPrivateKeyXml = null;
                    return;
                }

                _cachedPrivateKeyXml = keys.PrivateKeyXml;
                StatusMessage = "Mật khẩu hợp lệ. Đang giải mã bảng điểm...";

                // 3. Decrypt the loaded grade records in a background thread
                var tempGrades = Grades.ToList();
                string cachedKey = _cachedPrivateKeyXml ?? "";
                await Task.Run(() =>
                {
                    foreach (var g in tempGrades)
                    {
                        if (g.DIEMTHI == null || g.DIEMTHI.Length == 0)
                        {
                            g.DiemSo = "N/A";
                            continue;
                        }

                        try
                        {
                            g.DiemSo = CryptoHelper.DecryptRSA(g.DIEMTHI!, cachedKey);
                        }
                        catch
                        {
                            g.DiemSo = "(Lỗi giải mã)";
                        }
                    }
                });

                IsDecrypted = true;
                Grades = new ObservableCollection<GradeDetail>(tempGrades);
                StatusMessage = $"Giải mã thành công {Grades.Count} bản ghi điểm.";
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.FromException(ex, "Lỗi giải mã. Kiểm tra mật khẩu và thử lại.");
                IsDecrypted = false;
                _cachedPrivateKeyXml = null;
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
