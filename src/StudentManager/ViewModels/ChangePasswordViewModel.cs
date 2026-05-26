using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Dapper;
using StudentManager.Helpers;
using Microsoft.Data.SqlClient;
using StudentManager.Models;

namespace StudentManager.ViewModels
{
    public class ChangePasswordViewModel : ViewModelBase
    {
        private string _manv = "";
        public string Manv
        {
            get => _manv;
            set => SetProperty(ref _manv, value);
        }

        private string _oldPassword = "";
        public string OldPassword
        {
            get => _oldPassword;
            set => SetProperty(ref _oldPassword, value);
        }

        private string _newPassword = "";
        public string NewPassword
        {
            get => _newPassword;
            set => SetProperty(ref _newPassword, value);
        }

        private string _confirmNewPassword = "";
        public string ConfirmNewPassword
        {
            get => _confirmNewPassword;
            set => SetProperty(ref _confirmNewPassword, value);
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isProcessing;
        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (SetProperty(ref _isProcessing, value))
                {
                    OnPropertyChanged(nameof(CanExecuteChange));
                }
            }
        }

        public bool CanExecuteChange => !IsProcessing;

        public ICommand ChangePasswordCommand { get; }
        public ICommand CloseCommand { get; }

        public ChangePasswordViewModel()
        {
            ChangePasswordCommand = new RelayCommand(async _ => await ExecuteChangePassword(), _ => CanChangePassword());
            CloseCommand = new RelayCommand(win => ExecuteClose(win));
        }

        private bool CanChangePassword()
        {
            return !string.IsNullOrWhiteSpace(Manv) &&
                   !string.IsNullOrWhiteSpace(OldPassword) &&
                   !string.IsNullOrWhiteSpace(NewPassword) &&
                   !string.IsNullOrWhiteSpace(ConfirmNewPassword) &&
                   !IsProcessing;
        }

        private async Task ExecuteChangePassword()
        {
            StatusMessage = "";
            IsProcessing = true;

            if (NewPassword != ConfirmNewPassword)
            {
                StatusMessage = "Mật khẩu mới và xác nhận mật khẩu không trùng khớp.";
                IsProcessing = false;
                return;
            }

            if (NewPassword.Length < 6)
            {
                StatusMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                IsProcessing = false;
                return;
            }

            try
            {
                StatusMessage = "Đang kiểm tra thông tin tài khoản...";
                
                using var conn = DatabaseHelper.GetConnection() as SqlConnection;
                if (conn == null)
                {
                    StatusMessage = "Không thể khởi tạo kết nối cơ sở dữ liệu.";
                    IsProcessing = false;
                    return;
                }
                
                await conn.OpenAsync();

                // 1. Gọi SP_LOGIN_NHANVIEN để lấy thông tin tài khoản hiện tại
                var userRow = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SP_LOGIN_NHANVIEN",
                    new { LOGIN = Manv.Trim() },
                    commandType: CommandType.StoredProcedure);

                if (userRow == null)
                {
                    StatusMessage = "Mã nhân viên hoặc mật khẩu cũ không chính xác.";
                    IsProcessing = false;
                    return;
                }

                var userDict = (IDictionary<string, object>)userRow;
                string tendn = userDict["TENDN"]?.ToString() ?? "";
                byte[] dbMatkhauHash = (byte[])userDict["MATKHAU"];
                string dbPubkey = userDict["PUBKEY"]?.ToString() ?? "";

                if (tendn == "dummy" || string.IsNullOrEmpty(tendn))
                {
                    StatusMessage = "Mã nhân viên hoặc mật khẩu cũ không chính xác.";
                    IsProcessing = false;
                    return;
                }

                // 2. Xác thực mật khẩu cũ bằng băm SHA-1 tại Client
                byte[] hashedOldPw = CryptoHelper.Sha1(tendn + "|" + OldPassword);
                bool isOldPwMatched = true;
                if (dbMatkhauHash.Length != hashedOldPw.Length)
                {
                    isOldPwMatched = false;
                }
                else
                {
                    for (int i = 0; i < dbMatkhauHash.Length; i++)
                    {
                        if (dbMatkhauHash[i] != hashedOldPw[i])
                        {
                            isOldPwMatched = false;
                            break;
                        }
                    }
                }

                if (!isOldPwMatched)
                {
                    StatusMessage = "Mã nhân viên hoặc mật khẩu cũ không chính xác.";
                    IsProcessing = false;
                    return;
                }

                StatusMessage = "Đang tạo cặp khóa RSA mới (tác vụ này có thể mất vài giây)...";

                // 3. Tái tạo cặp khóa cũ và sinh cặp khóa mới xác định bằng Task.Run (tránh đơ UI)
                var oldKeys = await Task.Run(() => CryptoHelper.GenerateDeterministicKeyPair(OldPassword, Manv.Trim()));
                var newKeys = await Task.Run(() => CryptoHelper.GenerateDeterministicKeyPair(NewPassword, Manv.Trim()));

                // Xác minh chéo khóa công khai cũ
                if (!string.IsNullOrWhiteSpace(dbPubkey) && oldKeys.PublicKeyXml != dbPubkey)
                {
                    StatusMessage = "Mật khẩu cũ không chính xác. Không thể tạo khóa giải mã.";
                    IsProcessing = false;
                    return;
                }

                StatusMessage = "Đang nạp dữ liệu cần mã hóa lại...";

                // 4. Lấy Lương cũ của Nhân viên
                var nvProfile = await conn.QueryFirstOrDefaultAsync<NhanVien>(
                    "SP_SEL_PUBLIC_ENCRYPT_NHANVIEN",
                    new { TENDN = tendn, MK = hashedOldPw },
                    commandType: CommandType.StoredProcedure);

                byte[]? newLuongBytes = null;
                if (nvProfile?.LUONG != null && nvProfile.LUONG.Length > 0)
                {
                    // Giải mã lương cũ và mã hóa lại bằng khóa công khai mới
                    string plainSalary = CryptoHelper.DecryptRSA(nvProfile.LUONG, oldKeys.PrivateKeyXml);
                    newLuongBytes = CryptoHelper.EncryptRSA(plainSalary, newKeys.PublicKeyXml);
                }

                // 5. Lấy toàn bộ điểm thi của học sinh do giáo viên này phụ trách để Re-encrypt
                var rawGrades = (await conn.QueryAsync<dynamic>(
                    "SP_SEL_BANGDIEM_BY_OWNER",
                    new { CALLER_MANV = Manv.Trim() },
                    commandType: CommandType.StoredProcedure)).ToList();

                var reEncryptedGrades = new List<(string Masv, string Mahp, byte[] NewDiem)>();

                foreach (var g in rawGrades)
                {
                    byte[]? oldDiemBytes = g.DIEMTHI;
                    if (oldDiemBytes != null && oldDiemBytes.Length > 0)
                    {
                        try
                        {
                            string plainDiem = CryptoHelper.DecryptRSA(oldDiemBytes, oldKeys.PrivateKeyXml);
                            byte[] newDiemBytes = CryptoHelper.EncryptRSA(plainDiem, newKeys.PublicKeyXml);
                            reEncryptedGrades.Add(((string)g.MASV, (string)g.MAHP, newDiemBytes));
                        }
                        catch (Exception)
                        {
                            // Nếu có lỗi giải mã một bản ghi điểm, ta bỏ qua hoặc thông báo
                        }
                    }
                }

                StatusMessage = $"Đang tiến hành mã hóa lại và cập nhật ({reEncryptedGrades.Count + (newLuongBytes != null ? 1 : 0)} bản ghi)...";

                // 6. Thực thi cập nhật toàn bộ trong một Database Transaction
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // A. Cập nhật lại toàn bộ điểm thi đã mã hóa bằng khóa mới
                        foreach (var item in reEncryptedGrades)
                        {
                            await conn.ExecuteAsync(
                                "SP_UPD_BANGDIEM_RE_ENCRYPT",
                                new
                                {
                                    CALLER_MANV = Manv.Trim(),
                                    MASV = item.Masv,
                                    MAHP = item.Mahp,
                                    DIEMTHI = item.NewDiem
                                },
                                transaction: trans,
                                commandType: CommandType.StoredProcedure);
                        }

                        // B. Gọi SP đổi mật khẩu, PubKey và Lương của nhân viên
                        byte[] hashedNewPw = CryptoHelper.Sha1(tendn + "|" + NewPassword);
                        await conn.ExecuteAsync(
                            "SP_CHANGE_PASSWORD_NHANVIEN",
                            new
                            {
                                MANV = Manv.Trim(),
                                OLD_MK = hashedOldPw,
                                NEW_MK = hashedNewPw,
                                NEW_PUB = newKeys.PublicKeyXml,
                                NEW_LUONG = newLuongBytes
                            },
                            transaction: trans,
                            commandType: CommandType.StoredProcedure);

                        trans.Commit();
                        
                        // Cập nhật lại thông tin session hiện tại nếu người đổi mật khẩu chính là người đang đăng nhập
                        if (CurrentUser.MANV == Manv.Trim())
                        {
                            CurrentUser.PUBKEY = newKeys.PublicKeyXml;
                        }

                        DatabaseHelper.LogQuery("TRANSACTION COMMIT: SP_CHANGE_PASSWORD_NHANVIEN & RE-ENCRYPT GRADES", new { MANV = Manv.Trim(), GradesCount = reEncryptedGrades.Count });
                        
                        MessageBox.Show("Đổi mật khẩu và mã hóa lại toàn bộ Lương, Điểm thi thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        if (Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this) is Window win)
                        {
                            win.Close();
                        }
                    }
                    catch (Exception)
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Lỗi trong quá trình đổi mật khẩu: " + ex.Message;
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
