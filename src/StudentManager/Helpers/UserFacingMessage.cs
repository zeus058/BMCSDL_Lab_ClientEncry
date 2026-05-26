using System;
using Microsoft.Data.SqlClient;
using System.IO;

namespace StudentManager.Helpers
{
    /// <summary>Chuyển exception (đặc biệt SqlException) thành thông báo tiếng Việt, không hiển thị mã lỗi kỹ thuật.</summary>
    public static class UserFacingMessage
    {
        public static string ForLoad(Exception? ex) =>
            FromException(ex, "Không tải được dữ liệu. Kiểm tra kết nối mạng hoặc máy chủ cơ sở dữ liệu, rồi thử lại.");

        public static string ForSave(Exception? ex) =>
            FromException(ex, "Không lưu được. Kiểm tra dữ liệu nhập và quyền thao tác, rồi thử lại.");

        public static string ForDelete(Exception? ex) =>
            FromException(ex, "Không xóa được. Có thể còn dữ liệu liên quan hoặc bạn không có quyền xóa.");

        public static string ForLogin(Exception? ex) =>
            FromException(ex, "Không đăng nhập được. Kiểm tra kết nối và thử lại sau.");

        public static string ForExport(Exception? ex) =>
            FromException(ex, "Không xuất được tệp. Kiểm tra quyền ghi thư mục hoặc đóng tệp đang mở.");

        public static string ForSalaryQuery(Exception? ex) =>
            FromException(ex, "Không truy vấn được lương. Kiểm tra mật khẩu và kết nối cơ sở dữ liệu.");

        public static string ForLocalKey(Exception? ex) =>
            FromException(ex, "Không thao tác được với khóa cục bộ. Kiểm tra quyền ghi thư mục Keys.");

        public static string ForUnhandledAppError() =>
            "Đã xảy ra lỗi không mong đợi. Bạn có thể khởi động lại ứng dụng.";

        public static string FromException(Exception? ex, string genericFallback)
        {
            if (ex == null) return genericFallback;

            var sql = FindSqlException(ex);
            if (sql != null)
            {
                var mapped = MapSqlException(sql);
                if (mapped != null) return mapped;
            }

            if (ex is IOException or UnauthorizedAccessException)
                return "Không thể truy cập tệp. Kiểm tra quyền hoặc tệp đang được sử dụng.";

            return genericFallback;
        }

        private static SqlException? FindSqlException(Exception ex)
        {
            if (ex is SqlException s) return s;

            if (ex is AggregateException agg)
            {
                foreach (var inner in agg.Flatten().InnerExceptions)
                {
                    var found = FindSqlException(inner);
                    if (found != null) return found;
                }
            }

            return ex.InnerException != null ? FindSqlException(ex.InnerException) : null;
        }

        private static string? MapSqlException(SqlException sql)
        {
            foreach (SqlError err in sql.Errors)
            {
                var mapped = MapErrorNumber(err.Number);
                if (mapped != null) return mapped;
            }

            return MapErrorNumber(sql.Number);
        }

        private static string? MapErrorNumber(int number) =>
            number switch
            {
                50001 => "Mã nhân viên đã tồn tại.",
                50002 => "Tên đăng nhập đã tồn tại.",
                50003 => "Chỉ được tạo lớp do chính bạn phụ trách.",
                50004 => "Lớp không tồn tại hoặc mã nhân viên chủ nhiệm không hợp lệ.",
                50005 => "Bạn không có quyền xóa lớp này.",
                50006 => "Lớp vẫn còn sinh viên, không thể xóa.",
                50008 => "Bạn không có quyền thêm sinh viên vào lớp này.",
                50009 => "Bạn không có quyền sửa sinh viên này.",
                50010 => "Không được chuyển sinh viên sang lớp khác.",
                50011 => "Bạn không có quyền xóa sinh viên này.",
                50012 => "Học phần đang có bảng điểm, không thể xóa.",
                50013 => "Bạn không có quyền nhập điểm cho sinh viên này.",
                50014 => "Mã học phần không tồn tại trong hệ thống.",
                50015 => "Mật khẩu hiện tại không đúng.",
                547 => "Thao tác bị từ chối vì dữ liệu đang được sử dụng ở nơi khác.",
                2627 or 2601 => "Giá trị nhập bị trùng với dữ liệu đã có.",
                -2 => "Hết thời gian chờ máy chủ cơ sở dữ liệu.",
                53 or 10060 => "Không kết nối được tới máy chủ cơ sở dữ liệu.",
                4060 => "Không mở được cơ sở dữ liệu. Kiểm tra tên CSDL trong cấu hình.",
                18456 => "Không xác thực được với máy chủ cơ sở dữ liệu.",
                _ => null
            };
    }
}
