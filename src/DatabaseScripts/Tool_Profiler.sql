/* =============================================================
   HỆ THỐNG QUẢN LÝ SINH VIÊN (STUDENT MANAGEMENT SYSTEM)
   Script: Tool_Profiler.sql (Giám sát Hệ thống)
   -------------------------------------------------------------
   Mục tiêu:
   - Sử dụng Extended Events (XE) để theo dõi các thao tác nhập điểm.
   - Kiểm chứng việc dữ liệu điểm được mã hóa từ phía Client (WPF).
   - Đảm bảo điểm số không bị lộ dưới dạng văn bản thuần trên đường truyền.
   ============================================================= */

USE master;
GO

-- 1. Xóa session cũ nếu đang tồn tại
IF EXISTS (SELECT 1 FROM sys.server_event_sessions WHERE name = N'Lab03_BangDiem_Monitor')
    DROP EVENT SESSION Lab03_BangDiem_Monitor ON SERVER;
GO

-- 2. Khởi tạo Event Session để bắt các lệnh gọi Procedure SP_UPSERT_BANGDIEM
CREATE EVENT SESSION Lab03_BangDiem_Monitor ON SERVER
ADD EVENT sqlserver.rpc_completed(
    SET collect_statement = (1)
    ACTION (sqlserver.database_name, sqlserver.client_hostname, sqlserver.username)
    WHERE (sqlserver.database_name = N'QLSVNhom' AND sqlserver.object_name = N'SP_UPSERT_BANGDIEM'))
ADD TARGET package0.ring_buffer -- Lưu dữ liệu tạm thời vào bộ nhớ đệm vòng
WITH (MAX_MEMORY = 4 MB, EVENT_RETENTION_MODE = ALLOW_SINGLE_EVENT_LOSS, MAX_DISPATCH_LATENCY = 5 SECONDS);
GO

-- 3. Bật session giám sát
ALTER EVENT SESSION Lab03_BangDiem_Monitor ON SERVER STATE = START;
GO

PRINT N'Hệ thống giám sát [Lab03_BangDiem_Monitor] đã được KÍCH HOẠT.';
PRINT N'-------------------------------------------------------------';
PRINT N'Hướng dẫn sử dụng:';
PRINT N'1. Thực hiện thao tác nhập điểm trên ứng dụng WPF.';
PRINT N'2. Chạy câu lệnh SELECT dưới đây để xem dữ liệu bắt được:';
PRINT N'';
PRINT N'SELECT 
    n.value(''(event/@name)[1]'', ''varchar(50)'') AS [EventName],
    n.value(''(event/action[@name="database_name"]/value)[1]'', ''varchar(50)'') AS [Database],
    n.value(''(event/data[@name="statement"]/value)[1]'', ''nvarchar(max)'') AS [SQL_Statement]
FROM (
    SELECT CAST(target_data AS XML) AS target_data
    FROM sys.dm_xe_session_targets AS t
    JOIN sys.dm_xe_sessions AS s ON t.event_session_address = s.address
    WHERE s.name = N''Lab03_BangDiem_Monitor''
) AS tab
CROSS APPLY target_data.nodes(''RingBufferTarget/event'') AS q(n);';
PRINT N'-------------------------------------------------------------';
PRINT N'Dừng giám sát: ALTER EVENT SESSION Lab03_BangDiem_Monitor ON SERVER STATE = STOP;';
GO
