using System;
using System.Data;
using System.Windows;
using System.Windows.Input;
using Dapper;
using StudentManager.Helpers;
using StudentManager.Views;
using StudentManager.Models;
using System.Threading.Tasks;

namespace StudentManager.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private string _loginId = "";
        public string LoginId
        {
            get => _loginId;
            set => SetProperty(ref _loginId, value);
        }

        private string _password = "";
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand OpenChangePasswordCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(_ => ExecuteLogin(), _ => CanLogin());
            OpenChangePasswordCommand = new RelayCommand(_ => ExecuteOpenChangePassword());
        }

        private bool CanLogin() =>
            !string.IsNullOrWhiteSpace(LoginId) && !string.IsNullOrWhiteSpace(Password);

        private async void ExecuteLogin()
        {
            ErrorMessage = "";

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                // 1. Chỉ gọi 1 SP duy nhất lấy thông tin đăng nhập (nhận cả thông tin thật hoặc dummy)
                var userRow = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SP_LOGIN_NHANVIEN",
                    new { LOGIN = LoginId.Trim() },
                    commandType: CommandType.StoredProcedure);

                if (userRow == null)
                {
                    ErrorMessage = "Mã nhân viên hoặc mật khẩu không chính xác.";
                    return;
                }

                string tendn = userRow.TENDN;
                byte[] matkhauHash = userRow.MATKHAU;

                // 2. Tính toán hash mật khẩu dạng nhị phân tại Client
                byte[] clientHashedPw = CryptoHelper.Sha1(tendn + "|" + Password);

                // 3. So khớp hash tại Client
                bool isMatched = true;
                if (matkhauHash.Length != clientHashedPw.Length)
                {
                    isMatched = false;
                }
                else
                {
                    for (int i = 0; i < matkhauHash.Length; i++)
                    {
                        if (matkhauHash[i] != clientHashedPw[i])
                        {
                            isMatched = false;
                            break;
                        }
                    }
                }

                if (!isMatched || tendn == "dummy")
                {
                    ErrorMessage = "Mã nhân viên hoặc mật khẩu không chính xác.";
                    return;
                }

                string userPubKey = userRow.PUBKEY;

                // Nếu PUBKEY trống (lần đầu đăng nhập), tự động sinh khóa và cập nhật
                if (string.IsNullOrWhiteSpace(userPubKey))
                {
                    // Chỉ sinh khóa RSA tại Client khi chưa có trên DB (thường chỉ cho lần đầu đăng nhập)
                    var keys = await Task.Run(() => CryptoHelper.GenerateDeterministicKeyPair(Password, (string)userRow.MANV));
                    await conn.ExecuteAsync(
                        "UPDATE NHANVIEN SET PUBKEY = @PUB WHERE MANV = @MANV",
                        new { PUB = keys.PublicKeyXml, MANV = (string)userRow.MANV });
                    userPubKey = keys.PublicKeyXml;
                    DatabaseHelper.LogQuery("UPDATE NHANVIEN SET PUBKEY", new { MANV = (string)userRow.MANV });
                }

                CurrentUser.MANV = userRow.MANV;
                CurrentUser.HOTEN = userRow.HOTEN;
                CurrentUser.EMAIL = userRow.EMAIL ?? "";
                CurrentUser.TENDN = userRow.TENDN ?? "";
                CurrentUser.PUBKEY = userPubKey;

                DatabaseHelper.LogQuery("EXEC SP_LOGIN_NHANVIEN", new { LOGIN = LoginId.Trim() });

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var main = new MainView();
                    main.Show();
                    Application.Current.MainWindow?.Close();
                    Application.Current.MainWindow = main;
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = UserFacingMessage.ForLogin(ex);
            }
        }

        private void ExecuteOpenChangePassword()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var changePwWindow = new ChangePasswordWindow();
                changePwWindow.ShowDialog();
            });
        }
    }
}
