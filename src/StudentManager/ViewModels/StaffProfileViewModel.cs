using System;
using System.Data;
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
        // PROPERTIES: Hồ sơ cá nhân
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
        // COMMANDS
        // ============================================================
        public ICommand LoadDecryptedSalaryCommand { get; }

        public StaffProfileViewModel()
        {
            LoadDecryptedSalaryCommand = new RelayCommand(_ => LoadDecryptedSalary());
            LoadProfile();
        }

        // ============================================================
        // Hồ sơ cá nhân & Xem lương
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
    }
}
