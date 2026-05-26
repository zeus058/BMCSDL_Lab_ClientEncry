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
        // Properties for personal profile
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

        // Properties for staff list
        private ObservableCollection<NhanVien> _employees = new();
        public ObservableCollection<NhanVien> Employees
        {
            get => _employees;
            set => SetProperty(ref _employees, value);
        }

        // Properties for new employee input form
        private string _editManv = "";
        public string EditManv { get => _editManv; set => SetProperty(ref _editManv, value); }

        private string _editHoten = "";
        public string EditHoten { get => _editHoten; set => SetProperty(ref _editHoten, value); }

        private string _editEmail = "";
        public string EditEmail { get => _editEmail; set => SetProperty(ref _editEmail, value); }

        private string _editTendn = "";
        public string EditTendn { get => _editTendn; set => SetProperty(ref _editTendn, value); }

        private string _editMatKhau = "";
        public string EditMatKhau { get => _editMatKhau; set => SetProperty(ref _editMatKhau, value); }

        private string _editLuong = "";
        public string EditLuong { get => _editLuong; set => SetProperty(ref _editLuong, value); }

        // Commands
        public ICommand LoadDecryptedSalaryCommand { get; }
        public ICommand LoadEmployeesCommand { get; }
        public ICommand AddEmployeeCommand { get; }
        public ICommand PrepareNewEmployeeCommand { get; }

        public StaffProfileViewModel()
        {
            LoadDecryptedSalaryCommand = new RelayCommand(_ => LoadDecryptedSalary());
            LoadEmployeesCommand = new RelayCommand(_ => LoadEmployees());
            AddEmployeeCommand = new RelayCommand(_ => AddEmployee(), _ => CanAddEmployee());
            PrepareNewEmployeeCommand = new RelayCommand(_ => PrepareNewEmployee());

            LoadProfile();
            LoadEmployees();
        }

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

        private bool CanAddEmployee()
        {
            return !string.IsNullOrWhiteSpace(EditManv) &&
                   !string.IsNullOrWhiteSpace(EditHoten) &&
                   !string.IsNullOrWhiteSpace(EditTendn) &&
                   !string.IsNullOrWhiteSpace(EditMatKhau) &&
                   !string.IsNullOrWhiteSpace(EditLuong);
        }

        private void PrepareNewEmployee()
        {
            EditManv = "";
            EditHoten = "";
            EditEmail = "";
            EditTendn = "";
            EditMatKhau = "";
            EditLuong = "";
            StatusMessage = "Vui lòng nhập thông tin chi tiết của nhân viên mới.";
        }

        private async void AddEmployee()
        {
            if (!CanAddEmployee())
            {
                StatusMessage = "Vui lòng điền đầy đủ các thông tin bắt buộc.";
                return;
            }

            try
            {
                StatusMessage = "Đang sinh cặp khóa bảo mật và mã hóa dữ liệu tại Client...";
                
                string manv = EditManv.Trim();
                string hoten = EditHoten.Trim();
                string email = EditEmail.Trim();
                string tendn = EditTendn.Trim();
                string password = EditMatKhau;
                string rawSalary = EditLuong.Trim();

                if (!double.TryParse(rawSalary, out _))
                {
                    StatusMessage = "Lương cơ bản phải là một số hợp lệ.";
                    return;
                }

                // Run CPU-intensive cryptographic operations asynchronously to avoid UI thread blocking
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

                StatusMessage = $"Đã thêm nhân viên {hoten} ({manv}) thành công.";
                PrepareNewEmployee();
                LoadEmployees();
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForSave(ex);
            }
        }
    }
}

