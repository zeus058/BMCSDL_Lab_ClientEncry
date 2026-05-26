using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Dapper;
using StudentManager.Helpers;
using StudentManager.Models;

namespace StudentManager.ViewModels
{
    public class ClassViewModel : ViewModelBase
    {
        private ObservableCollection<Lop> _classes = new();
        public ObservableCollection<Lop> Classes
        {
            get => _classes;
            set => SetProperty(ref _classes, value);
        }

        private Lop? _selectedClass;
        public Lop? SelectedClass
        {
            get => _selectedClass;
            set
            {
                if (!SetProperty(ref _selectedClass, value)) return;

                if (value == null)
                {
                    EditMalop = "";
                    EditTenlop = "";
                    EditManv = "";
                }
                else
                {
                    EditMalop = value.MALOP;
                    EditTenlop = value.TENLOP;
                    EditManv = value.MANV ?? CurrentUser.MANV;
                }

                OnPropertyChanged(nameof(IsMalopReadOnly));
            }
        }

        public bool IsMalopReadOnly => SelectedClass != null;

        private string _editMalop = "";
        public string EditMalop
        {
            get => _editMalop;
            set => SetProperty(ref _editMalop, value);
        }

        private string _editTenlop = "";
        public string EditTenlop
        {
            get => _editTenlop;
            set => SetProperty(ref _editTenlop, value);
        }

        private string _editManv = "";
        public string EditManv
        {
            get => _editManv;
            set => SetProperty(ref _editManv, value);
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }

        public ClassViewModel()
        {
            RefreshCommand = new RelayCommand(_ => Load());
            AddCommand = new RelayCommand(_ => PrepareAdd());
            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
            DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedClass != null);
            Load();
        }

        private bool CanSave() =>
            !string.IsNullOrWhiteSpace(EditMalop) && !string.IsNullOrWhiteSpace(EditTenlop);

        private async void Load()
        {
            var currentId = SelectedClass?.MALOP;

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                var list = (await conn.QueryAsync<Lop>(
                    "SP_SEL_LOP",
                    commandType: CommandType.StoredProcedure)).ToList();

                Classes = new ObservableCollection<Lop>(list);
                SelectedClass = list.FirstOrDefault(item => item.MALOP == currentId);
                
                DatabaseHelper.LogQuery("EXEC SP_SEL_LOP");
                StatusMessage = list.Count == 0
                    ? "Đã tải danh sách lớp (chưa có lớp nào)."
                    : $"Đã tải danh sách lớp ({list.Count} lớp).";
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForLoad(ex);
            }
        }

        private void PrepareAdd()
        {
            SelectedClass = null;
            EditMalop = "";
            EditTenlop = "";
            EditManv = "";
            StatusMessage = "Vui lòng nhập thông tin lớp mới và mã NV phụ trách, sau đó bấm Lưu.";
        }

        private async void Save()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();

                if (SelectedClass == null)
                {
                    await conn.ExecuteAsync(
                        "SP_INS_LOP",
                        new
                        {
                            CALLER_MANV = CurrentUser.MANV,
                            MALOP = EditMalop.Trim(),
                            TENLOP = EditTenlop.Trim(),
                            MANV = EditManv.Trim()
                        },
                        commandType: CommandType.StoredProcedure);

                    DatabaseHelper.LogQuery("EXEC SP_INS_LOP", new { MALOP = EditMalop.Trim() });
                    StatusMessage = "Đã thêm lớp mới vào hệ thống.";
                }
                else
                {
                    await conn.ExecuteAsync(
                        "SP_UPD_LOP",
                        new
                        {
                            CALLER_MANV = CurrentUser.MANV,
                            MALOP = SelectedClass.MALOP,
                            TENLOP = EditTenlop.Trim(),
                            NEW_MANV = EditManv.Trim()
                        },
                        commandType: CommandType.StoredProcedure);

                    DatabaseHelper.LogQuery("EXEC SP_UPD_LOP", new { MALOP = SelectedClass.MALOP });
                    StatusMessage = "Đã cập nhật thông tin lớp học.";
                }

                Load();
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForSave(ex);
            }
        }

        private async void Delete()
        {
            if (SelectedClass == null)
                return;

            if (MessageBox.Show(
                    $"Xóa lớp {SelectedClass.MALOP}?",
                    "Xác nhận",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                await conn.ExecuteAsync(
                    "SP_DEL_LOP",
                    new
                    {
                        CALLER_MANV = CurrentUser.MANV,
                        MALOP = SelectedClass.MALOP
                    },
                    commandType: CommandType.StoredProcedure);

                DatabaseHelper.LogQuery("EXEC SP_DEL_LOP", new { MALOP = SelectedClass.MALOP });
                StatusMessage = "Đã xóa lớp khỏi hệ thống.";
                Load();
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForDelete(ex);
            }
        }
    }
}
