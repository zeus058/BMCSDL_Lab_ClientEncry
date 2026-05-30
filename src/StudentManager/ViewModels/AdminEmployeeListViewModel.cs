using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows.Input;
using Dapper;
using StudentManager.Helpers;
using StudentManager.Models;

namespace StudentManager.ViewModels
{
    public class AdminEmployeeListViewModel : ViewModelBase
    {
        private ObservableCollection<NhanVien> _employees = new();
        public ObservableCollection<NhanVien> Employees
        {
            get => _employees;
            set => SetProperty(ref _employees, value);
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand LoadEmployeesCommand { get; }

        public AdminEmployeeListViewModel()
        {
            LoadEmployeesCommand = new RelayCommand(_ => LoadEmployees());
            LoadEmployees();
        }

        private async void LoadEmployees()
        {
            try
            {
                StatusMessage = "Đang tải danh sách nhân viên...";
                using var conn = DatabaseHelper.GetConnection();
                var list = (await conn.QueryAsync<NhanVien>(
                    "SP_SEL_ALL_NHANVIEN",
                    commandType: CommandType.StoredProcedure)).ToList();

                Employees = new ObservableCollection<NhanVien>(list);
                StatusMessage = $"Đã tải thành công {list.Count} nhân viên.";
                DatabaseHelper.LogQuery("EXEC SP_SEL_ALL_NHANVIEN");
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForLoad(ex);
            }
        }
    }
}
