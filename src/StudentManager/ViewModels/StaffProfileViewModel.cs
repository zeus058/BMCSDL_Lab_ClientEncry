using System;
using System.Data;
using System.Windows.Input;
using Dapper;
using StudentManager.Helpers;
using StudentManager.Models;

namespace StudentManager.ViewModels
{
    public class StaffProfileViewModel : ViewModelBase
    {
        private string _manv = "";
        public string Manv { get => _manv; set => SetProperty(ref _manv, value); }

        private string _hoten = "";
        public string Hoten { get => _hoten; set => SetProperty(ref _hoten, value); }

        private string _email = "";
        public string Email { get => _email; set => SetProperty(ref _email, value); }

        private string _tendn = "";
        public string Tendn { get => _tendn; set => SetProperty(ref _tendn, value); }

        private string _pubkey = "";
        public string Pubkey { get => _pubkey; set => SetProperty(ref _pubkey, value); }

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

        public ICommand LoadDecryptedSalaryCommand { get; }

        public StaffProfileViewModel()
        {
            LoadDecryptedSalaryCommand = new RelayCommand(_ => LoadDecryptedSalary());
            LoadProfile();
        }

        private void LoadProfile()
        {
            Manv = CurrentUser.MANV;
            Hoten = CurrentUser.HOTEN;
            Email = CurrentUser.EMAIL ?? "";
            Tendn = CurrentUser.TENDN;
            Pubkey = CurrentUser.PUBKEY;
        }

        private void LoadDecryptedSalary()
        {
            if (string.IsNullOrWhiteSpace(ViewSalaryPassword))
            {
                StatusMessage = "Vui lòng nhập mật khẩu để xem lương cơ bản.";
                return;
            }

            try
            {
                byte[] hashedPw = CryptoHelper.Sha1(CurrentUser.TENDN + "|" + ViewSalaryPassword);

                using var conn = DatabaseHelper.GetConnection();
                var row = conn.QueryFirstOrDefault<NhanVien>(
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

                var privateKeyXml = CryptoHelper.LoadPrivateKeyLocal(row.MANV);
                if (string.IsNullOrEmpty(privateKeyXml))
                {
                    DecryptedLuongcb = "(Lỗi khóa cục bộ)";
                    StatusMessage = "Không tìm thấy khóa bí mật cục bộ của nhân viên để giải mã.";
                    return;
                }

                DecryptedLuongcb = CryptoHelper.DecryptRSA(row.LUONG, privateKeyXml!);
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
