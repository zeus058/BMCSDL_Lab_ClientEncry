using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Dapper;
using StudentManager.Helpers;

namespace StudentManager.ViewModels
{
    public class RegisterViewModel : ViewModelBase
    {
        private string _manv = "";
        public string Manv { get => _manv; set => SetProperty(ref _manv, value); }

        private string _hoten = "";
        public string Hoten { get => _hoten; set => SetProperty(ref _hoten, value); }

        private string _email = "";
        public string Email { get => _email; set => SetProperty(ref _email, value); }

        private string _tendn = "";
        public string Tendn { get => _tendn; set => SetProperty(ref _tendn, value); }

        private string _password = "";
        public string Password { get => _password; set => SetProperty(ref _password, value); }

        private string _confirmPassword = "";
        public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }

        private string _luong = "";
        public string Luong { get => _luong; set => SetProperty(ref _luong, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        private bool _isProcessing;
        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (SetProperty(ref _isProcessing, value))
                    OnPropertyChanged(nameof(CanExecute));
            }
        }

        public bool CanExecute => !IsProcessing;

        public ICommand RegisterCommand { get; }
        public ICommand CloseCommand { get; }

        public RegisterViewModel()
        {
            RegisterCommand = new RelayCommand(async _ => await ExecuteRegister(), _ => CanRegister());
            CloseCommand = new RelayCommand(win => ExecuteClose(win));
        }

        private bool CanRegister()
        {
            return !string.IsNullOrWhiteSpace(Manv) &&
                   !string.IsNullOrWhiteSpace(Hoten) &&
                   !string.IsNullOrWhiteSpace(Tendn) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                   !string.IsNullOrWhiteSpace(Luong) &&
                   !IsProcessing;
        }

        private async Task ExecuteRegister()
        {
            StatusMessage = "";
            IsProcessing = true;

            try
            {
                // Validate
                if (Password != ConfirmPassword)
                {
                    StatusMessage = "Mật khẩu và xác nhận mật khẩu không trùng khớp.";
                    IsProcessing = false;
                    return;
                }

                if (Password.Length < 6)
                {
                    StatusMessage = "Mật khẩu phải có ít nhất 6 ký tự.";
                    IsProcessing = false;
                    return;
                }

                string manv = Manv.Trim();
                string hoten = Hoten.Trim();
                string email = Email.Trim();
                string tendn = Tendn.Trim();
                string password = Password;
                string rawSalary = Luong.Trim();

                if (!double.TryParse(rawSalary, out _))
                {
                    StatusMessage = "Lương cơ bản phải là một số hợp lệ.";
                    IsProcessing = false;
                    return;
                }

                StatusMessage = "Đang sinh cặp khóa bảo mật và mã hóa dữ liệu tại Client...";

                // Thực hiện mã hóa tại Client (chạy trên background thread)
                var (hashedPw, keys, encryptedLuong) = await Task.Run(() =>
                {
                    // 1. SHA-1 băm mật khẩu tại Client
                    byte[] pwHash = CryptoHelper.Sha1(tendn + "|" + password);

                    // 2. Sinh khóa Deterministic RSA-2048 tại Client từ (password, MANV)
                    var keyPair = CryptoHelper.GenerateDeterministicKeyPair(password, manv);

                    // 3. Mã hóa lương bằng RSA Public Key tại Client
                    byte[] encSalary = CryptoHelper.EncryptRSA(rawSalary, keyPair.PublicKeyXml);

                    return (pwHash, keyPair, encSalary);
                });

                StatusMessage = "Đang lưu thông tin vào cơ sở dữ liệu...";

                // 4. Gọi stored procedure lưu dữ liệu đã mã hóa lên DB
                using var conn = DatabaseHelper.GetConnection();
                await conn.ExecuteAsync(
                    "SP_INS_PUBLIC_ENCRYPT_NHANVIEN",
                    new
                    {
                        MANV = manv,
                        HOTEN = hoten,
                        EMAIL = string.IsNullOrEmpty(email) ? null : email,
                        LUONG = encryptedLuong,
                        TENDN = tendn,
                        MK = hashedPw,
                        PUB = keys.PublicKeyXml
                    },
                    commandType: CommandType.StoredProcedure);

                DatabaseHelper.LogQuery("EXEC SP_INS_PUBLIC_ENCRYPT_NHANVIEN", new { MANV = manv });

                MessageBox.Show(
                    $"Đăng ký tài khoản nhân viên {hoten} ({manv}) thành công!\nBạn có thể đăng nhập ngay bằng Mã Nhân Viên.",
                    "Đăng ký thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Đóng cửa sổ đăng ký
                if (Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this) is Window win)
                {
                    win.Close();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForSave(ex);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ExecuteClose(object? win)
        {
            if (win is Window window)
            {
                window.Close();
            }
        }
    }
}
