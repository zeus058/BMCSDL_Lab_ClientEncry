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
    public class CourseViewModel : ViewModelBase
    {
        private ObservableCollection<HocPhan> _items = new();
        public ObservableCollection<HocPhan> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        private HocPhan? _selected;
        public HocPhan? Selected
        {
            get => _selected;
            set
            {
                if (!SetProperty(ref _selected, value)) return;

                if (value == null)
                {
                    EditMahp = "";
                    EditTenhp = "";
                    EditSotc = "";
                }
                else
                {
                    EditMahp = value.MAHP;
                    EditTenhp = value.TENHP;
                    EditSotc = value.SOTC?.ToString() ?? "";
                }

                OnPropertyChanged(nameof(IsMahpReadOnly));
            }
        }

        public bool IsMahpReadOnly => Selected != null;

        private string _editMahp = "";
        public string EditMahp
        {
            get => _editMahp;
            set => SetProperty(ref _editMahp, value);
        }

        private string _editTenhp = "";
        public string EditTenhp
        {
            get => _editTenhp;
            set => SetProperty(ref _editTenhp, value);
        }

        private string _editSotc = "";
        public string EditSotc
        {
            get => _editSotc;
            set => SetProperty(ref _editSotc, value);
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }

        public CourseViewModel()
        {
            RefreshCommand = new RelayCommand(_ => Load());
            NewCommand = new RelayCommand(_ => PrepareAdd());
            SaveCommand = new RelayCommand(_ => Save(), _ => !string.IsNullOrWhiteSpace(EditMahp) && !string.IsNullOrWhiteSpace(EditTenhp));
            DeleteCommand = new RelayCommand(_ => Delete(), _ => Selected != null);
            Load();
        }

        private void Load()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                var list = conn.Query<HocPhan>("SP_SEL_HOCPHAN", commandType: CommandType.StoredProcedure).ToList();
                Items = new ObservableCollection<HocPhan>(list);
                DatabaseHelper.LogQuery("EXEC SP_SEL_HOCPHAN");
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForLoad(ex);
            }
        }

        private void PrepareAdd()
        {
            Selected = null;
            StatusMessage = "Vui lòng nhập Mã học phần, Tên học phần và Số tín chỉ.";
        }

        private void Save()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                var exists = Items.Any(item => item.MAHP.Equals(EditMahp.Trim(), StringComparison.OrdinalIgnoreCase));
                var sotc = int.TryParse(EditSotc, out var credits) ? credits : (int?)null;

                if (exists)
                {
                    conn.Execute(
                        "UPDATE HOCPHAN SET TENHP = @TENHP, SOTC = @SOTC WHERE MAHP = @MAHP",
                        new
                        {
                            MAHP = EditMahp.Trim(),
                            TENHP = EditTenhp.Trim(),
                            SOTC = sotc
                        },
                        commandType: CommandType.Text);

                    DatabaseHelper.LogQuery("UPDATE HOCPHAN", new { MAHP = EditMahp.Trim() });
                    StatusMessage = "Đã cập nhật thông tin học phần.";
                }
                else
                {
                    conn.Execute(
                        "INSERT INTO HOCPHAN (MAHP, TENHP, SOTC) VALUES (@MAHP, @TENHP, @SOTC)",
                        new
                        {
                            MAHP = EditMahp.Trim(),
                            TENHP = EditTenhp.Trim(),
                            SOTC = sotc
                        },
                        commandType: CommandType.Text);

                    DatabaseHelper.LogQuery("INSERT INTO HOCPHAN", new { MAHP = EditMahp.Trim() });
                    StatusMessage = "Đã thêm học phần mới.";
                }

                Load();
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForSave(ex);
            }
        }

        private void Delete()
        {
            if (Selected == null)
                return;

            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Execute(
                    "DELETE FROM HOCPHAN WHERE MAHP = @MAHP",
                    new { MAHP = Selected.MAHP },
                    commandType: CommandType.Text);

                DatabaseHelper.LogQuery("DELETE FROM HOCPHAN", new { MAHP = Selected.MAHP });
                StatusMessage = "Đã xóa học phần khỏi hệ thống.";
                Load();
            }
            catch (Exception ex)
            {
                StatusMessage = UserFacingMessage.ForDelete(ex);
            }
        }
    }
}
