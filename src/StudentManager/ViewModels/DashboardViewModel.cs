using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Dapper;
using LiveCharts;
using LiveCharts.Wpf;
using StudentManager.Helpers;

namespace StudentManager.ViewModels
{
    public class DashboardSummaryRow
    {
        public int TotalClasses { get; set; }
        public int TotalStudents { get; set; }
        public int TotalCourses { get; set; }
        public int EnteredGrades { get; set; }
        public int StudentsWithGrades { get; set; }
        public int StudentsWithoutGrades { get; set; }
    }

    public class StudentCountByClassRow
    {
        public string MALOP { get; set; } = "";
        public string TENLOP { get; set; } = "";
        public int StudentCount { get; set; }
    }

    public class DashboardViewModel : ViewModelBase
    {
        public SeriesCollection StudentCountSeries { get; set; } = new();
        public string[] Labels { get; set; } = Array.Empty<string>();
        public Func<double, string> Formatter { get; set; } = value => value.ToString("N0");

        private int _managedClasses;
        public int ManagedClasses
        {
            get => _managedClasses;
            set => SetProperty(ref _managedClasses, value);
        }

        private int _managedStudents;
        public int ManagedStudents
        {
            get => _managedStudents;
            set => SetProperty(ref _managedStudents, value);
        }

        private int _totalCourses;
        public int TotalCourses
        {
            get => _totalCourses;
            set => SetProperty(ref _totalCourses, value);
        }

        private int _enteredGrades;
        public int EnteredGrades
        {
            get => _enteredGrades;
            set => SetProperty(ref _enteredGrades, value);
        }

        public SeriesCollection CoverageSeries { get; set; } = new();

        public DashboardViewModel()
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();

                var summary = conn.QueryFirstOrDefault<DashboardSummaryRow>(
                    "SP_SEL_DASHBOARD_SUMMARY",
                    new { CALLER_MANV = CurrentUser.MANV },
                    commandType: System.Data.CommandType.StoredProcedure) ?? new DashboardSummaryRow();

                ManagedClasses = summary.TotalClasses;
                ManagedStudents = summary.TotalStudents;
                TotalCourses = summary.TotalCourses;
                EnteredGrades = summary.EnteredGrades;

                var rows = conn.Query<StudentCountByClassRow>(
                    @"SELECT l.MALOP, l.TENLOP, COUNT(s.MASV) as StudentCount 
                      FROM LOP l 
                      LEFT JOIN SINHVIEN s ON l.MALOP = s.MALOP 
                      WHERE l.MANV = @CALLER_MANV 
                      GROUP BY l.MALOP, l.TENLOP",
                    new { CALLER_MANV = CurrentUser.MANV },
                    commandType: System.Data.CommandType.Text).ToList();

                var values = new ChartValues<int>();
                var labels = new List<string>();
                foreach (var row in rows)
                {
                    labels.Add(row.MALOP);
                    values.Add(row.StudentCount);
                }

                Labels = labels.ToArray();
                StudentCountSeries = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Sinh viên",
                        Values = values,
                        Fill = new SolidColorBrush(Color.FromRgb(15, 118, 110)),
                        Stroke = new SolidColorBrush(Color.FromRgb(17, 94, 89)),
                        MaxColumnWidth = 36
                    }
                };

                CoverageSeries = new SeriesCollection
                {
                    new PieSeries
                    {
                        Title = "Đã có điểm",
                        Values = new ChartValues<int> { summary.EnteredGrades },
                        Fill = new SolidColorBrush(Color.FromRgb(14, 116, 144)),
                        DataLabels = true
                    },
                    new PieSeries
                    {
                        Title = "Tổng sinh viên",
                        Values = new ChartValues<int> { summary.TotalStudents },
                        Fill = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                        DataLabels = true
                    }
                };

                OnPropertyChanged(nameof(StudentCountSeries));
                OnPropertyChanged(nameof(Labels));
                OnPropertyChanged(nameof(CoverageSeries));

                DatabaseHelper.LogQuery("EXEC SP_SEL_DASHBOARD_SUMMARY", new { CALLER_MANV = CurrentUser.MANV });
                DatabaseHelper.LogQuery("SELECT StudentCount BY CLASS", new { CALLER_MANV = CurrentUser.MANV });
            }
            catch
            {
                StudentCountSeries = new SeriesCollection();
                CoverageSeries = new SeriesCollection();
                Labels = Array.Empty<string>();
                OnPropertyChanged(nameof(StudentCountSeries));
                OnPropertyChanged(nameof(CoverageSeries));
                OnPropertyChanged(nameof(Labels));
            }
        }
    }
}
