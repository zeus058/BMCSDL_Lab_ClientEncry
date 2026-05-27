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
    public class StaffProfileViewModel : ViewModelBase
    {
        // ============================================================
        // PROPERTIES: Hồ sơ cá nhân (Tab 1)
        // ============================================================
        private string _manv = "";
        public string Manv { get => _manv; set => SetProperty(ref _manv, value); }

        private string _hoten = "";
        public string Hoten { get => _hoten; set => SetProperty(ref _hoten, value); }

        private string _email = "";
        public string Email { get => _email; set => SetProperty(ref _email, value); }

        private string _tendn = "";
        public string Tendn { get => _tendn; set => SetProperty(ref _tendn, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        private string _viewSalaryPassword = "";
        public string ViewSalaryPassword
        {
            get => _viewSalaryPassword;
            set => SetProperty(ref _viewSalaryPassword, value);
        }

        private string _decryptedLuongcb = "";
        public string DecryptedLuongcb
        {
            get => _decryptedLuongcb;
            set => SetProperty(ref _decryptedLuongcb, value);
        }

        // ============================================================
        // PROPERTIES: Danh sách nhân viên (Tab 2)
        // ============================================================
        private ObservableCollection<NhanVien> _employees = new();
        public ObservableCollection<NhanVien> Employees
        {
            get => _employees;
            set => SetProperty(ref _employees, value);
        }

        // ============================================================
        // PROPERTIES: Admin Manager (Tab 3)
        // ============================================================
        private string _adminPassword = "";
        public string AdminPassword
        {
            get => _adminPassword;
            set => SetProperty(ref _adminPassword, value);
        }

        private bool _isAdminUnlocked;
        public bool IsAdminUnlocked
        {
            get => _isAdminUnlocked;
            set => SetProperty(ref _isAdminUnlocked, value);
        }

        private string _adminStatusMessage = "";
        public string AdminStatusMessage
        {
            get => _adminStatusMessage;
            set => SetProperty(ref _adminStatusMessage, value);
        }

        private ObservableCollection<NhanVien> _adminEmployees = new();
        public ObservableCollection<NhanVien> AdminEmployees
        {
            get => _adminEmployees;
            set => SetProperty(ref _adminEmployees, value);
        }

        private NhanVien? _selectedAdminEmployee;
        public NhanVien? SelectedAdminEmployee
        {
            get => _selectedAdminEmployee;
            set => SetProperty(ref _selectedAdminEmployee, value);
        }

        private string _newLuong = "";
        public string NewLuong
        {
            get => _newLuong;
            set => SetProperty(ref _newLuong, value);
        }

        // ============================================================
        // COMMANDS
        // ============================================================
        public ICommand LoadDecryptedSalaryCommand { get; }
        public ICommand LoadEmployeesCommand { get; }
        public ICommand UnlockAdminCommand { get; }
        public ICommand UpdateSalaryCommand { get; }

        public StaffProfileViewModel()
        {
            LoadDecryptedSalaryCommand = new RelayCommand(_ => LoadDecryptedSalary());
            LoadEmployeesCommand = new RelayCommand(_ => LoadEmployees());
            UnlockAdminCommand = new RelayCommand(_ => UnlockAdmin());
            UpdateSalaryCommand = new RelayCommand(_ => UpdateSalary(), _ => CanUpdateSalary());

            LoadProfile();
            LoadEmployees();
        }

        // ============================================================
        // TAB 1: Hồ sơ cá nhân
        // ============================================================
        private void LoadProfile()
        {
            Manv = CurrentUser.MANV;
            Hoten = CurrentUser.HOTEN;
            Email = CurrentUser.EMAIL ?? "";
            Tendn = CurrentUser.TENDN;
        }

        private async void LoadDecryptedSalary()
        {
            if (string.IsNullOrWhiteSpace(ViewSalaryPassword))
            {
                StatusMessage = "Vui lòng nhập mật khẩu để xem lương cơ bản.";
                return;
            }

            try
            {
                StatusMessage = "Đang xác thực mật khẩu và giải mã lương tại Client...";
                string password = ViewSalaryPassword;

                byte[] hashedPw = CryptoHelper.Sha1(CurrentUser.TENDN + "|" + password);

                using var conn = DatabaseHelper.GetConnection();
                var row = await conn.QueryFirstOrDefaultAsync<NhanVien>(
                    "SP_SEL_PUBLIC_ENCRYPT_NHANVIEN",
                    new { TENDN = CurrentUser.TENDN, MK = hashedPw },
                    commandType: CommandType.StoredProcedure);

                if (row == null)
                {
                    DecryptedLuongcb = "";
                    StatusMessage = "Mật khẩu xác thực không đúng.";
                    return;
                }

                if (row.LUONG == null || row.LUONG.Length == 0)
                {
                    DecryptedLuongcb = "(trống)";
                    StatusMessage = "Không có dữ liệu lương.";
                    return;
                }

                // Run deterministic key generation and RSA decryption asynchronously in background thread
                string decrypted = await Task.Run(() =>
                {
                    var keys = CryptoHelper.GenerateDeterministicKeyPair(password, row.MANV);
                    return CryptoHelper.DecryptRSA(row.LUONG, keys.PrivateKeyXml);
                });

                DecryptedLuongcb = decrypted;
                StatusMessage = "Đã giải mã lương cơ bản thành công tại Client.";
                DatabaseHelper.LogQuery("EXEC SP_SEL_PUBLIC_ENCRYPT_NHANVIEN", new { TENDN = CurrentUser.TENDN });
            }
            catch (Exception ex)
            {
                DecryptedLuongcb = "";
                StatusMessage = UserFacingMessage.ForSalaryQuery(ex);
            }
        }

        // ============================================================
        // TAB 2: Danh sách nhân viên (chỉ xem)
        // ============================================================
        private async void LoadEmployees()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                var list = (await conn.QueryAsync<NhanVien>(
                    "SP_SEL_ALL_NHANVIEN",
                    commandType: CommandType.StoredProcedure)).ToList();

                Employees = new ObservableCollection<NhanVien>(list);
                DatabaseHelper.LogQuery("EXEC SP_SEL_ALL_NHANVIEN");
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForLoad(ex);
            }
        }

        // ============================================================
        // TAB 3: Admin Manager
        // ============================================================

        /// <summary>
        /// Xác thực mật khẩu admin cố định (master password) để mở khóa tab Admin.
        /// </summary>
        private void UnlockAdmin()
        {
            if (string.IsNullOrWhiteSpace(AdminPassword))
            {
                AdminStatusMessage = "Vui lòng nhập mật khẩu admin.";
                return;
            }

            if (CryptoHelper.VerifyAdminPassword(AdminPassword))
            {
                IsAdminUnlocked = true;
                AdminStatusMessage = "Đã xác thực admin thành công.";
                LoadAdminEmployees();
            }
            else
            {
                IsAdminUnlocked = false;
                AdminStatusMessage = "Mật khẩu admin không chính xác.";
            }
        }

        /// <summary>
        /// Tải danh sách nhân viên kèm PUBKEY cho Admin (để mã hóa lương).
        /// </summary>
        private async void LoadAdminEmployees()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                var list = (await conn.QueryAsync<NhanVien>(
                    "SP_SEL_ALL_NHANVIEN",
                    commandType: CommandType.StoredProcedure)).ToList();

                AdminEmployees = new ObservableCollection<NhanVien>(list);
            }
            catch (Exception ex)
            {
                AdminStatusMessage = UserFacingMessage.ForLoad(ex);
            }
        }

        private bool CanUpdateSalary()
        {
            return IsAdminUnlocked &&
                   SelectedAdminEmployee != null &&
                   !string.IsNullOrWhiteSpace(NewLuong);
        }

        /// <summary>
        /// Admin cập nhật lương cho nhân viên: mã hóa RSA bằng PUBKEY của nhân viên đích.
        /// Admin có thể GHI lương mới nhưng KHÔNG THỂ ĐỌC lương cũ (chỉ NV sở hữu Private Key mới giải mã được).
        /// </summary>
        private async void UpdateSalary()
        {
            if (SelectedAdminEmployee == null)
            {
                AdminStatusMessage = "Vui lòng chọn nhân viên cần cập nhật lương.";
                return;
            }

            if (!double.TryParse(NewLuong.Trim(), out _))
            {
                AdminStatusMessage = "Lương cơ bản phải là một số hợp lệ.";
                return;
            }

            string targetPubKey = SelectedAdminEmployee.PUBKEY ?? "";
            if (string.IsNullOrWhiteSpace(targetPubKey))
            {
                AdminStatusMessage = $"Nhân viên {SelectedAdminEmployee.MANV} chưa có khóa công khai. Nhân viên cần đăng nhập ít nhất 1 lần để sinh khóa.";
                return;
            }

            try
            {
                AdminStatusMessage = $"Đang mã hóa lương bằng Public Key của {SelectedAdminEmployee.HOTEN} tại Client...";

                string rawSalary = NewLuong.Trim();
                string pubKeyXml = targetPubKey;

                // Mã hóa RSA lương mới bằng Public Key của nhân viên đích (Client-side encryption)
                byte[] encryptedLuong = CryptoHelper.EncryptRSA(rawSalary, pubKeyXml);

                using var conn = DatabaseHelper.GetConnection();
                await conn.ExecuteAsync(
                    "SP_UPD_LUONG_ADMIN",
                    new
                    {
                        TARGET_MANV = SelectedAdminEmployee.MANV,
                        NEW_LUONG = encryptedLuong
                    },
                    commandType: CommandType.StoredProcedure);

                DatabaseHelper.LogQuery("EXEC SP_UPD_LUONG_ADMIN", new { TARGET_MANV = SelectedAdminEmployee.MANV });

                AdminStatusMessage = $"Đã cập nhật lương thành công cho nhân viên {SelectedAdminEmployee.HOTEN} ({SelectedAdminEmployee.MANV}).";
                NewLuong = "";
            }
            catch (Exception ex)
            {
                AdminStatusMessage = UserFacingMessage.ForSave(ex);
            }
        }
    }
}
