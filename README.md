# Kimi - Hệ Thống Quản Lý Sinh Viên Bảo Mật Cao - Lab 4

<!-- Các Badges giới thiệu và công nghệ -->
[![License: MIT](https://img.shields.io/badge/License-MIT-teal.svg)](https://opensource.org/licenses/MIT)
[![.NET Version](https://img.shields.io/badge/.NET-%3E%3D%20Framework%204.8-blue.svg)](https://dotnet.microsoft.com)
[![Database](https://img.shields.io/badge/Database-SQL_Server-red.svg)](https://www.microsoft.com/sql-server)
[![Security](https://img.shields.io/badge/Security-RSA_2048%20%26%20SHA_1-brightgreen.svg)](https://en.wikipedia.org/wiki/RSA_(cryptosystem))

Một hệ thống quản lý thông tin sinh viên, lớp học và học phần chuyên nghiệp trên nền tảng **WPF (C#)**. Hệ thống tích hợp cơ chế **mã hóa và băm dữ liệu hoàn toàn tại Client (Client-Side Encryption)** trước khi truyền qua mạng và lưu trữ tại **SQL Server**, giúp bảo vệ tuyệt đối dữ liệu nhạy cảm trước các nguy cơ tấn công mạng hoặc rò rỉ thông tin từ phía quản trị viên cơ sở dữ liệu (DBA).

---

## Mục lục
1. [Giới thiệu & Tính năng](#giới-thiệu--tính-năng)
2. [Cơ chế bảo mật cốt lõi](#cơ-chế-bảo-mật-cốt-lõi)
3. [Công nghệ sử dụng](#công-nghệ-sử-dụng)
4. [Hướng dẫn khởi chạy cục bộ](#hướng-dẫn-khởi-chạy-cục-bộ)
5. [Cấu trúc thư mục](#cấu-trúc-thư-mục)
6. [Giấy phép](#giấy-phép)

---

## Giới thiệu & Tính năng

**Kimi** giải quyết triệt để bài toán rò rỉ dữ liệu điểm số và thông tin cá nhân trong các hệ thống quản lý trường học hiện nay. Nhờ áp dụng cơ chế mã hóa đầu cuối tại máy trạm, ngay cả quản trị viên cơ sở dữ liệu (DBA) hay kẻ tấn công nghe lén trên đường truyền mạng cũng không thể đọc được điểm thi hoặc lương của nhân viên nếu không có khóa bí mật cục bộ.

### Các tính năng cốt lõi và bổ sung mới:
- **Quản lý Lớp học:** Nhân viên quản lý và lập danh sách các lớp học thuộc phạm vi phụ trách. Ràng buộc nghiệp vụ và giao diện **chỉ cho phép sửa đổi Tên lớp**, khóa cứng Mã lớp và Nhân viên phụ trách sau khi tạo nhằm bảo vệ phân quyền lớp.
- **Quản lý Sinh viên:** Quản lý hồ sơ sinh viên, phân lớp và cập nhật thông tin. Ràng buộc nghiệp vụ nghiêm ngặt chỉ cho phép sửa đổi thông tin sinh viên thuộc lớp mình trực tiếp quản lý.
- **Quản lý Học phần:** Quản lý danh mục môn học, số tín chỉ tương ứng.
- **Tự Đăng Ký Tài Khoản từ Login:** Nút đăng ký tài khoản mới được tích hợp trực tiếp trên cửa sổ đăng nhập (`LoginView`), liên kết đến `RegisterWindow` an toàn. Thực hiện băm mật khẩu SHA-1, sinh cặp khóa Deterministic RSA-2048 và mã hóa lương hoàn toàn tại Client trước khi gọi `SP_INS_PUBLIC_ENCRYPT_NHANVIEN` để ghi nhận tài khoản.
- **Cập Nhật Lương Admin (Admin Manager):** Tích hợp tab quản trị đặc biệt trong Hồ sơ cá nhân. 
  - Khóa bảo vệ bằng **Mật khẩu Master của Admin** (`admin123`) xác thực một chiều ở Client.
  - Cho phép Admin chọn một nhân viên bất kỳ, Client tự động lấy khóa công khai (`PUBKEY` XML) của nhân viên đó để mã hóa RSA lương mới trước khi gọi `SP_UPD_LUONG_ADMIN`.
  - **Bảo mật tuyệt đối:** Admin có quyền ghi đè lương mới (đã mã hóa) nhưng **không thể đọc** được lương hiện tại của nhân viên vì không sở hữu khóa bí mật tương ứng (chỉ nhân viên sở hữu mới giải mã được).
- **Nhập điểm Bảo mật:** Điểm thi được mã hóa RSA-2048 ngay tại Client bằng khóa công khai của nhân viên trước khi truyền đi và lưu trữ tại bảng `BANGDIEM`.
- **Tra cứu Điểm (Transcripts):** Giải mã bảng điểm trực quan tại Client thông qua khóa riêng cục bộ của nhân viên phụ trách lớp. Tự động dọn dẹp điểm rác kiểm thử sau khi chạy công cụ Unit Tests tránh gây lỗi giải mã trên UI.
- **Giám sát Truy vấn Real-time:** Tích hợp hộp thoại giám sát SQL Profiler trực tiếp trong ứng dụng để theo dõi các câu lệnh Dapper gửi đi trong thời gian thực, phục vụ học tập và kiểm thử.

---

## Cơ chế bảo mật cốt lõi

Dự án triển khai mô hình **An toàn thông tin phía người dùng (Client-Side Security)** toàn diện:

### 1. Băm Mật khẩu (SHA-1 Salted tại Client)
- Mật khẩu nhân viên và sinh viên được băm ngay tại Client bằng thuật toán **SHA-1** kết hợp muối dạng chuỗi: `TENDN + "|" + Password`.
- Cơ sở dữ liệu lưu trữ trực tiếp chuỗi hash nhị phân (`VARBINARY`) và so khớp trực tiếp, hoàn toàn loại bỏ các hàm băm `HASHBYTES` ở phía Server để tối ưu hiệu năng và bảo mật đường truyền.

### 2. Mã hóa Lương (Client-side Deterministic RSA-2048)
- Khi khởi tạo nhân viên mới, Client sinh cặp khóa Deterministic RSA-2048 xác định từ `(Password, MANV)`. Khóa công khai dạng XML được lưu trong cột `PUBKEY` ở cơ sở dữ liệu.
- Khóa riêng (Private Key) hoàn toàn **không** lưu cục bộ trên ổ đĩa máy tính hay lưu trên DB để tránh rò rỉ. Khi cần dùng (ví dụ giải mã lương hoặc giải mã điểm số), khóa riêng sẽ được tái tạo động tức thời từ mật khẩu và mã nhân viên bằng thuật toán sinh khóa xác định.
- Mức lương được mã hóa RSA-2048 tại Client trước khi lưu trữ vào DB. Khi hiển thị, người dùng nhập lại mật khẩu để Client tái tạo khóa riêng và giải mã trực tiếp.

### 3. Mã hóa Điểm số (Client-side RSA-2048)
- Điểm thi của sinh viên được mã hóa RSA bằng khóa công khai của nhân viên phụ trách lớp. Chỉ có nhân viên sở hữu khóa riêng tương ứng mới có thể giải mã và xem điểm.

### Sơ đồ Kiến trúc Bảo mật đầu cuối:

```mermaid
graph TD
    subgraph Client ["Client-Side WPF Application"]
        P["Mật khẩu thô"] --> H["Băm SHA-1 với muối TENDN|MK"] --> HE["Mật khẩu Hash nhị phân"]
        L["Lương cơ bản thô"] --> E1["Mã hóa RSA-2048 bằng Public Key của Nhân viên"] --> LE["Lương Mã hóa nhị phân"]
        D["Điểm thi thô"] --> E2["Mã hóa RSA-2048 bằng Public Key của Giáo viên phụ trách"] --> DE["Điểm Mã hóa nhị phân"]
    end

    subgraph Network ["Đường truyền mạng"]
        HE -->|Truyền dữ liệu mã hóa/băm| DB
        LE -->|Truyền dữ liệu mã hóa/băm| DB
        DE -->|Truyền dữ liệu mã hóa/băm| DB
    end

    subgraph Server ["Database Server SQL Server"]
        DB[("Cơ sở dữ liệu QLSVNhom")]
        DB -->|Lưu trữ an toàn tuyệt đối dạng VARBINARY| C["MATKHAU, LUONG, DIEMTHI"]
    end
```

---

## Công nghệ sử dụng

Hệ thống được thiết kế theo cấu trúc hiện đại, hiệu năng cao và có tính thẩm mỹ giao diện vượt trội:

- **Frontend UI/UX:** Windows Presentation Foundation (WPF), ngôn ngữ thiết kế Modern UI với phông chữ Montserrat thời thượng, bảng màu Teal (#0F766E) và bóng đổ thẻ Card có chiều sâu.
- **Backend & Cryptography:** C# .NET Framework 4.8, thư viện bảo mật `System.Security.Cryptography`.
- **Data Access Layer:** Dapper Micro-ORM (tối ưu hóa tốc độ truy vấn, truyền tham số nhị phân an toàn tránh SQL Injection).
- **Database Engine:** Microsoft SQL Server (2019/2022/2025).
- **Libraries:** `Newtonsoft.Json` (xử lý JSON), `ClosedXML` (xuất báo cáo Excel), `LiveCharts` (biểu đồ thống kê).

---

## Hướng dẫn khởi chạy cục bộ

Để cài đặt và vận hành hệ thống Kimi trên môi trường local, vui lòng thực hiện theo các bước tuần tự dưới đây:

### 1. Yêu cầu hệ thống tiên quyết
- Máy tính chạy hệ điều hành **Windows**.
- **.NET Framework 4.8 SDK** & Runtime (hỗ trợ .NET CLI hoặc Visual Studio 2022).
- **Microsoft SQL Server** (bản 2019, 2022, 2025 hoặc mới hơn).
- **SQL Server Management Studio (SSMS)** hoặc Azure Data Studio.
- **Git** cài đặt sẵn trên máy.

### 2. Các bước cài đặt tuần tự

**Bước 1: Sao chép mã nguồn về máy trạm local**
```bash
git clone https://github.com/your-username/LAB_BMCSDL-Lab4.git
cd LAB_BMCSDL-Lab4
```

**Bước 2: Cài đặt và cấu hình Cơ sở dữ liệu**
Đảm bảo máy chủ SQL Server đang hoạt động (Ví dụ Server có tên là `ZEUS`). Thực thi các tệp tin SQL trong thư mục [DatabaseScripts](file:///d:/BMCSDL/LAB_BMCSDL - Lab4/src/DatabaseScripts) theo đúng thứ tự.

> [!IMPORTANT]
> **Chú ý đặc biệt về Mã hóa Tiếng Việt (UTF-8 Encoding):**
> Vì các tệp tin kịch bản cơ sở dữ liệu được lưu dưới dạng UTF-8, khi sử dụng công cụ `sqlcmd` để import dữ liệu trên Windows, bạn **bắt buộc** phải truyền thêm cờ `-f i:65001` (Code Page 65001 tương ứng UTF-8) để tránh tình trạng các chữ tiếng Việt có dấu bị lỗi hiển thị font (Mojibake) trong Database.

Thực hiện chạy các câu lệnh dưới đây trong cửa sổ Terminal/PowerShell:
```bash
# 1. Tạo Database và Master Key
sqlcmd -S ZEUS -E -C -f i:65001 -i "src/DatabaseScripts/01_Schema.sql"

# 2. Tạo các Stored Procedure nghiệp vụ và bảo mật
sqlcmd -S ZEUS -E -C -f i:65001 -i "src/DatabaseScripts/02_Procedures.sql"

# 3. Nạp dữ liệu mẫu cho hệ thống
sqlcmd -S ZEUS -E -C -f i:65001 -i "src/DatabaseScripts/03_SeedData.sql"

# 4. Thực thi kiểm thử đơn vị (Unit Tests) tự động
sqlcmd -S ZEUS -E -C -f i:65001 -i "src/DatabaseScripts/Tool_Tests.sql"
```

> [!NOTE]
> **Mật khẩu mặc định của các tài khoản mẫu sau khi chạy seed data:**
> - Nhân viên 1 (Admin): Tài khoản `nva` - Mật khẩu `abcd12`
> - Nhân viên 2: Tài khoản `ttb` - Mật khẩu `pass123`
> - Mật khẩu của tất cả sinh viên mẫu: `sv123` (Ví dụ SV01 tài khoản `lvc` - mật khẩu `sv123`).
> - Mật khẩu Master của quản trị viên để mở khóa tab Admin Manager: `admin123`

**Bước 3: Cấu hình chuỗi kết nối ứng dụng (Connection String)**
Mở tệp [App.config](file:///d:/BMCSDL/LAB_BMCSDL - Lab4/src/StudentManager/App.config) trong thư mục dự án và điều chỉnh thuộc tính `connectionString` trong thẻ `<connectionStrings>` cho khớp với tên máy chủ SQL Server thực tế của bạn. Đảm bảo có thiết lập `TrustServerCertificate=True` để tránh lỗi bắt tay chứng chỉ SSL:

```xml
<connectionStrings>
    <add name="QLSVNhom" 
         connectionString="Data Source=YOUR_SERVER_NAME;Initial Catalog=QLSVNhom;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;" 
         providerName="System.Data.SqlClient" />
</connectionStrings>
```

**Bước 4: Build và chạy ứng dụng**
Biên dịch dự án WPF bằng dòng lệnh hoặc mở thư mục nguồn bằng Visual Studio 2022:
```bash
dotnet build src/StudentManager/StudentManager.csproj
```
Sau khi build thành công, tệp thực thi `StudentManager.exe` sẽ được tạo ra tại thư mục `src/StudentManager/bin/Debug/net48/`. Tiến hành chạy ứng dụng và đăng nhập bằng tài khoản mẫu `nva` để trải nghiệm hệ thống!

---

## Cấu trúc thư mục thực tế

Sơ đồ hình cây trực quan thể hiện kiến trúc phân lớp thực tế của dự án sau khi dọn dẹp:

```text
LAB_BMCSDL - Lab4/
├── Documentation/               # Tài liệu thiết kế hệ thống và đề bài thực hành
│   ├── README.md                # Tài liệu hướng dẫn sử dụng gốc
│   └── lab03.pdf                # Đề bài thực hành môn học
├── src/
│   ├── DatabaseScripts/         # Các Script CSDL SQL Server (Mã hóa UTF-8)
│   │   ├── 01_Schema.sql        # Cấu trúc bảng vật lý và MASTER KEY
│   │   ├── 02_Procedures.sql    # 21 stored procedures bảo mật Client-side (Đã dọn dẹp)
│   │   ├── 03_SeedData.sql      # Nạp dữ liệu mẫu (Nhân viên, Sinh viên, Lớp...)
│   │   └── Tool_Tests.sql       # Chạy Unit Tests tự động kèm tính năng tự dọn dẹp điểm rác
│   │
│   └── StudentManager/          # Mã nguồn ứng dụng WPF C# (.NET 4.8)
│       ├── App.config           # Tệp cấu hình Connection String
│       ├── Theme.xaml           # Hệ thống Styles, Phông chữ Montserrat, Palette màu Teal
│       ├── Helpers/             # Các thư viện tiện ích mã hóa và kết nối
│       │   ├── CryptoHelper.cs       # Cung cấp facade mã hóa RSA, băm SHA-1 tại Client
│       │   ├── CurrentUser.cs        # Lưu trữ trạng thái phiên đăng nhập của nhân viên
│       │   ├── DatabaseHelper.cs     # Quản lý kết nối Dapper CSDL và Profiler Logs
│       │   ├── DeterministicRsa.cs   # Thuật toán sinh cặp khóa RSA-2048 xác định từ Password
│       │   ├── InverseBoolToVisibilityConverter.cs # Converter trạng thái khóa mở của Admin
│       │   ├── PasswordBoxAssistant.cs # Giải quyết ràng buộc PasswordBox binding trong MVVM
│       │   ├── RelayCommand.cs       # Cấu trúc lệnh ICommand phục vụ MVVM
│       │   ├── StringToVisibilityConverter.cs # Trình ẩn hiện chuỗi lỗi của giao diện
│       │   ├── ToastService.cs       # Quản lý hiển thị Toast thông báo nhanh
│       │   └── UserFacingMessage.cs  # Dịch mã lỗi SQL Server thành thông báo tiếng Việt
│       ├── Models/              # Lớp đối tượng POCO (POCO DTOs)
│       ├── ViewModels/          # Tầng ViewModels quản lý logic nghiệp vụ và mã hóa
│       └── Views/               # Tầng Giao diện XAML và Code-behind tương ứng
├── rule_README.md               # Quy chuẩn viết tài liệu README
└── README.md                    # File tài liệu hướng dẫn chính này (bản hoàn chỉnh cập nhật)
```

---

## Giấy phép

Dự án này được phân phối công khai và cấp phép hợp pháp dưới dạng **Giấy phép MIT License**. Thông tin chi tiết vui lòng xem tại tệp `LICENSE` đính kèm trong thư mục gốc.
