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
    public class AdminSalaryAdjustmentViewModel : ViewModelBase
    {
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
            set
            {
                if (SetProperty(ref _selectedAdminEmployee, value))
                {
                    OnPropertyChanged(nameof(IsEmployeeSelected));
                    OnPropertyChanged(nameof(SelectedEmployeeHeader));
                    NewLuong = ""; // Clear salary field when switching employees
                }
            }
        }

        public bool IsEmployeeSelected => SelectedAdminEmployee != null;

        public string SelectedEmployeeHeader => SelectedAdminEmployee != null 
            ? $"Sửa lương cho: {SelectedAdminEmployee.HOTEN} ({SelectedAdminEmployee.MANV})" 
            : "Sửa lương (Vui lòng chọn nhân viên)";

        private string _newLuong = "";
        public string NewLuong
        {
            get => _newLuong;
            set => SetProperty(ref _newLuong, value);
        }

        private string _adminStatusMessage = "";
        public string AdminStatusMessage
        {
            get => _adminStatusMessage;
            set => SetProperty(ref _adminStatusMessage, value);
        }

        public ICommand UpdateSalaryCommand { get; }
        public ICommand LoadAdminEmployeesCommand { get; }

        public AdminSalaryAdjustmentViewModel()
        {
            UpdateSalaryCommand = new RelayCommand(_ => UpdateSalary(), _ => CanUpdateSalary());
            LoadAdminEmployeesCommand = new RelayCommand(_ => LoadAdminEmployees());
            LoadAdminEmployees();
        }

        private async void LoadAdminEmployees()
        {
            try
            {
                AdminStatusMessage = "Đang tải danh sách nhân viên...";
                using var conn = DatabaseHelper.GetConnection();
                var list = (await conn.QueryAsync<NhanVien>(
                    "SP_SEL_ALL_NHANVIEN",
                    commandType: CommandType.StoredProcedure)).ToList();

                AdminEmployees = new ObservableCollection<NhanVien>(list);
                AdminStatusMessage = "Chọn một nhân viên bên dưới để bắt đầu sửa lương.";
                DatabaseHelper.LogQuery("EXEC SP_SEL_ALL_NHANVIEN");
            }
            catch (Exception ex)
            {
                AdminStatusMessage = UserFacingMessage.ForLoad(ex);
            }
        }

        private bool CanUpdateSalary()
        {
            return SelectedAdminEmployee != null && !string.IsNullOrWhiteSpace(NewLuong);
        }

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
                byte[] encryptedLuong = await Task.Run(() => CryptoHelper.EncryptRSA(rawSalary, pubKeyXml));

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
