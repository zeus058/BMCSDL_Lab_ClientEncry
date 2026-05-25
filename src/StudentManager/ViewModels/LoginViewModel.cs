using System;
using System.Data;
using System.Windows;
using System.Windows.Input;
using Dapper;
using StudentManager.Helpers;
using StudentManager.Models;
using StudentManager.Views;

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

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(_ => ExecuteLogin(), _ => CanLogin());
        }

        private bool CanLogin() =>
            !string.IsNullOrWhiteSpace(LoginId) && !string.IsNullOrWhiteSpace(Password);

        private void ExecuteLogin()
        {
            ErrorMessage = "";

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                var tendn = conn.QueryFirstOrDefault<string>(
                    "SELECT TENDN FROM NHANVIEN WHERE MANV = @LOGIN OR TENDN = @LOGIN",
                    new { LOGIN = LoginId.Trim() });

                if (string.IsNullOrEmpty(tendn))
                {
                    ErrorMessage = "Mã nhân viên hoặc mật khẩu không chính xác.";
                    return;
                }

                byte[] hashedPw = CryptoHelper.Sha1(tendn + "|" + Password);

                var user = conn.QueryFirstOrDefault<NhanVien>(
                    "SP_LOGIN_NHANVIEN",
                    new
                    {
                        LOGIN = LoginId.Trim(),
                        MK = hashedPw
                    },
                    commandType: CommandType.StoredProcedure);

                if (user == null)
                {
                    ErrorMessage = "Mã nhân viên hoặc mật khẩu không chính xác.";
                    return;
                }

                RsaKeyProvisioning.EnsureLocalKeyPair(user.MANV);

                CurrentUser.MANV = user.MANV;
                CurrentUser.HOTEN = user.HOTEN;
                CurrentUser.EMAIL = user.EMAIL ?? "";
                CurrentUser.TENDN = user.TENDN ?? "";
                CurrentUser.PUBKEY = user.PUBKEY ?? "";
                CurrentUser.CurrentPassword = Password;

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
    }
}
