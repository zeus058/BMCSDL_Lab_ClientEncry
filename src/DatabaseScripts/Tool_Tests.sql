/* =============================================================
   HỆ THỐNG QUẢN LÝ SINH VIÊN (STUDENT MANAGEMENT SYSTEM)
   Script: Tool_Tests.sql (Kiểm thử đơn vị - Unit Tests)
   -------------------------------------------------------------
   Mục tiêu:
   - Tự động kiểm tra tính đúng đắn của Database và logic nghiệp vụ.
   - Kiểm tra các ràng buộc bảo mật (RSA, SHA1, Quyền hạn).
   - Đảm bảo hệ thống hoạt động đúng sau khi triển khai hoặc cập nhật.
   ============================================================= */

USE QLSVNhom;
GO
SET NOCOUNT ON;

DECLARE @TestNum INT = 0;
DECLARE @Pass BIT = 0;
DECLARE @Actual INT;

PRINT N'=============================================================';
PRINT N'   BẮT ĐẦU KIỂM THỬ HỆ THỐNG (UNIT TESTS)   ';
PRINT N'=============================================================';

-- TEST 1: Kiểm tra Compatibility Level (Mức độ tương thích)
SET @TestNum += 1;
SET @Pass = CASE WHEN (SELECT compatibility_level FROM sys.databases WHERE name = N'QLSVNhom') = 170 THEN 1 ELSE 0 END;
PRINT N'Test ' + CAST(@TestNum AS VARCHAR) + ': Mức độ tương thích Database (v170) -> ' + CASE WHEN @Pass = 1 THEN N'THÀNH CÔNG' ELSE N'THẤT BẠI' END;

-- TEST 2: Kiểm tra khóa RSA (Public Key XML) của Nhân viên lưu trong DB
SET @TestNum += 1;
SET @Pass = CASE WHEN EXISTS (SELECT 1 FROM NHANVIEN WHERE MANV = N'NV01' AND PUBKEY LIKE '<RSAKeyValue>%') THEN 1 ELSE 0 END;
PRINT N'Test ' + CAST(@TestNum AS VARCHAR) + ': Lưu trữ khóa công khai RSA dạng XML của NV01 -> ' + CASE WHEN @Pass = 1 THEN N'THÀNH CÔNG' ELSE N'THẤT BẠI' END;

-- TEST 3: Kiểm tra cơ chế Hash mật khẩu tại Client (so khớp SHA1)
SET @TestNum += 1;
SET @Pass = CASE WHEN EXISTS (SELECT 1 FROM NHANVIEN WHERE MANV = N'NV01' AND MATKHAU = HASHBYTES('SHA1', 'nva|abcd12')) THEN 1 ELSE 0 END;
PRINT N'Test ' + CAST(@TestNum AS VARCHAR) + ': Xác thực mật khẩu Hash (SHA1) cho NV01 -> ' + CASE WHEN @Pass = 1 THEN N'THÀNH CÔNG' ELSE N'THẤT BẠI' END;

-- TEST 4: Kiểm tra quyền hạn (Ràng buộc phạm vi quản lý lớp)
SET @TestNum += 1;
SET @Pass = 0;
BEGIN TRY
    -- Thử thêm sinh viên vào lớp của người khác (NV02 thử thêm vào lớp của NV01)
    DECLARE @HashedPw VARBINARY(MAX) = HASHBYTES('SHA1', 'test|test');
    EXEC SP_INS_SINHVIEN @CALLER_MANV = N'NV02', @MASV = N'SV_TEST', @HOTEN = N'Test', @NGAYSINH = '20000101', 
                         @DIACHI = N'HCM', @MALOP = N'CQ2019/1', @TENDN = N'test', @MK = @HashedPw;
END TRY
BEGIN CATCH
    SET @Pass = 1; -- Nếu ném lỗi là đúng nghiệp vụ (Thành công)
END CATCH
PRINT N'Test ' + CAST(@TestNum AS VARCHAR) + ': Ngăn chặn thêm sinh viên ngoài phạm vi quản lý -> ' + CASE WHEN @Pass = 1 THEN N'THÀNH CÔNG' ELSE N'THẤT BẠI' END;

-- TEST 5: Kiểm tra cơ chế nhập điểm mã hóa (VARBINARY)
SET @TestNum += 1;
EXEC SP_UPSERT_BANGDIEM @CALLER_MANV = N'NV01', @MASV = N'SV01', @MAHP = N'CSDL', @DIEMTHI = 0xDEADC0DE;
SET @Pass = CASE WHEN EXISTS (SELECT 1 FROM BANGDIEM WHERE MASV = N'SV01' AND MAHP = N'CSDL' AND DIEMTHI = 0xDEADC0DE) THEN 1 ELSE 0 END;
PRINT N'Test ' + CAST(@TestNum AS VARCHAR) + ': Lưu trữ điểm thi dưới dạng Ciphertext (Binary) -> ' + CASE WHEN @Pass = 1 THEN N'THÀNH CÔNG' ELSE N'THẤT BẠI' END;

PRINT N'=============================================================';
PRINT N'   HOÀN TẤT KIỂM THỬ   ';
PRINT N'=============================================================';
GO
