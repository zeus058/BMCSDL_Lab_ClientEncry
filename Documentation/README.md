# Kimi - Hệ thống Quản lý Sinh viên (Student Management System)

Kimi là một ứng dụng quản lý sinh viên hiện đại, chuyên nghiệp, được xây dựng trên nền tảng WPF (.NET 4.8) với trọng tâm là bảo mật dữ liệu và trải nghiệm người dùng tối ưu. Hệ thống cho phép quản lý hồ sơ sinh viên, lớp học, học phần và đặc biệt là cơ chế nhập điểm được mã hóa an toàn.

## 🚀 Tính năng chính

- **Quản lý Lớp học**: Quản lý danh sách các lớp học do nhân viên phụ trách.
- **Quản lý Sinh viên**: Quản lý hồ sơ chi tiết, phân lớp và cập nhật thông tin sinh viên.
- **Quản lý Học phần**: Quản lý danh mục các môn học và số tín chỉ.
- **Nhập điểm Bảo mật**: Sử dụng thuật toán **RSA-2048** để mã hóa điểm thi ngay tại máy trạm trước khi lưu vào cơ sở dữ liệu.
- **Tra cứu Điểm (Transcripts)**: Cho phép nhân viên quản lý giải mã và xem điểm của sinh viên thuộc quyền quản lý của mình.
- **Giám sát Truy vấn**: Công cụ dành cho quản trị viên để theo dõi các truy vấn SQL đang thực thi trong hệ thống.
- **Hồ sơ Cá nhân**: Xem thông tin nhân viên và giải mã lương cơ bản cá nhân bằng mật khẩu xác thực.

## 🛠 Công nghệ sử dụng

- **Frontend**: WPF (Windows Presentation Foundation), Modern UI/UX.
- **Backend**: .NET Framework 4.8, C#.
- **Database**: Microsoft SQL Server.
- **ORM/Data Access**: Dapper (Lightweight & Performance).
- **Security**: RSA-2048 (Mã hóa bất đối xứng), SHA-512 (Băm mật khẩu).
- **Libraries**: Newtonsoft.Json, ClosedXML, LiveCharts.

## 📁 Cấu trúc thư mục

- `src/StudentManager`: Mã nguồn chính của ứng dụng WPF.
- `src/DatabaseScripts`: Các script SQL để khởi tạo Schema, Stored Procedures và Seed Data.
- `Documentation`: Tài liệu hướng dẫn và các file liên quan.

## ⚙️ Hướng dẫn cài đặt

### 1. Cơ sở dữ liệu (SQL Server)
Thực thi các script trong thư mục `src/DatabaseScripts` theo thứ tự:
1. `01_Schema.sql`: Tạo cơ sở dữ liệu và các bảng.
2. `02_Procedures.sql`: Cài đặt các Stored Procedures cần thiết.
3. `03_SeedData.sql`: Nạp dữ liệu mẫu để thử nghiệm.

### 2. Cấu hình Ứng dụng
- Mở file `App.config` (hoặc cấu hình trong `DatabaseHelper.cs`) để cập nhật chuỗi kết nối (Connection String) đến SQL Server của bạn.

### 3. Build & Run
- Yêu cầu: Visual Studio 2022 hoặc .NET SDK (hỗ trợ .NET 4.8).
- Chạy lệnh:
  ```powershell
  dotnet build src/StudentManager/StudentManager.csproj
  ```
- Chạy ứng dụng từ tệp `.exe` trong thư mục `bin\Debug\net48\`.

## 🔐 Bảo mật dữ liệu

Hệ thống tuân thủ các quy tắc bảo mật nghiêm ngặt:
1. **Mật khẩu**: Được băm bằng SHA-512 với Salt trước khi lưu.
2. **Điểm số**: Điểm thi (`DIEMTHI`) được mã hóa RSA bằng khóa công khai của nhân viên phụ trách lớp. Chỉ nhân viên sở hữu khóa riêng tương ứng mới có thể giải mã để xem điểm.
3. **Lương cơ bản**: Được mã hóa bằng thuật toán đối xứng, yêu cầu mật khẩu đăng nhập để giải mã và hiển thị.
