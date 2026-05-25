using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace StudentManager.ViewModels
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Query { get; set; } = "";
        public string Parameters { get; set; } = "";
    }

    public class MonitorViewModel : ViewModelBase
    {
        private static readonly ObservableCollection<LogEntry> InternalLogs = new();

        public ObservableCollection<LogEntry> Logs => InternalLogs;

        public string ProfilerNotes => @"SQL Profiler / Extended Events — Lab 03 trên SQL Server 2025

1) Mở SSMS -> Tools -> SQL Server Profiler -> tạo trace mới với sự kiện RPC:Completed.
2) Chạy script src/Database/Profiler.sql để bật session Extended Events Lab03_BangDiem_RPC.
3) Trong ứng dụng, thao tác Lưu điểm sẽ mã hóa điểm bằng public key local tại Keys/{MANV}_public.xml trước khi gọi SP_UPSERT_BANGDIEM.
4) Quan sát tham số @DIEMTHI trong trace: dữ liệu được gửi đi dưới dạng nhị phân, không phải điểm rõ.
5) Ở phiên bản SQL Server 2025 này, cột NHANVIEN.PUBKEY chỉ lưu tên asymmetric key trong SQL cho phần mã hóa lương.";

        public static void AddLog(string query, object? parameters = null)
        {
            var app = Application.Current;
            if (app?.Dispatcher == null)
                return;

            app.Dispatcher.Invoke(() =>
            {
                InternalLogs.Insert(0, new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Query = query,
                    Parameters = parameters != null
                        ? Newtonsoft.Json.JsonConvert.SerializeObject(parameters)
                        : "{}"
                });

                if (InternalLogs.Count > 200)
                    InternalLogs.RemoveAt(InternalLogs.Count - 1);
            });
        }
    }
}
