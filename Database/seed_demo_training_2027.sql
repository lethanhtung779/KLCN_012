-- Dữ liệu mẫu bổ sung cho màn hình Khóa học/Lớp và Quản lý tài khoản.
-- Chạy bằng: sqlcmd -S .\SQLEXPRESS -d ThiTracNghiem -E -b -i Database\seed_demo_training_2027.sql
-- Chạy lại an toàn: chỉ bổ sung bản ghi còn thiếu, không sửa tài khoản hoặc lớp có sẵn.
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Courses TABLE (Stt INT PRIMARY KEY, Mon NVARCHAR(100));
    INSERT INTO @Courses (Stt, Mon) VALUES
        (1, N'Toán'), (2, N'Vật lý'), (3, N'Hóa học'), (4, N'Sinh học'),
        (5, N'Lịch sử'), (6, N'Địa lý'), (7, N'Tin học'),
        (8, N'Kinh tế pháp luật'), (9, N'Công nghệ'), (10, N'Tiếng Anh');

    DECLARE @LastNames TABLE (Stt INT PRIMARY KEY, Ho NVARCHAR(30));
    INSERT INTO @LastNames (Stt, Ho) VALUES
        (1, N'Nguyễn'), (2, N'Trần'), (3, N'Lê'), (4, N'Phạm'), (5, N'Hoàng'),
        (6, N'Vũ'), (7, N'Đặng'), (8, N'Bùi'), (9, N'Đỗ'), (10, N'Phan');

    DECLARE @GivenNames TABLE (Stt INT PRIMARY KEY, Ten NVARCHAR(50));
    INSERT INTO @GivenNames (Stt, Ten) VALUES
        (1, N'Minh Anh'), (2, N'Gia Huy'), (3, N'Khánh Linh'),
        (4, N'Đức Anh'), (5, N'Thu Hà'), (6, N'Quang Minh');

    DECLARE @i INT = 1, @j INT, @mon NVARCHAR(100), @tenKhoa NVARCHAR(100),
            @monId INT, @khoaId INT, @tenLop NVARCHAR(100), @lopId INT,
            @tenDangNhap NVARCHAR(50), @email NVARCHAR(100), @hoTen NVARCHAR(100),
            @taiKhoanId INT, @hocVienId INT;
    DECLARE @passwordHash NVARCHAR(255) = CONVERT(VARCHAR(64),
        HASHBYTES('SHA2_256', CONVERT(VARBINARY(MAX), N'Demo@2027!')), 2);

    WHILE @i <= 10
    BEGIN
        SET @monId = NULL;
        SET @khoaId = NULL;
        SELECT @mon = Mon FROM @Courses WHERE Stt = @i;
        SELECT @monId = MonHocID FROM MonHoc WHERE TenMon = @mon;
        IF @monId IS NULL THROW 51001, N'Thiếu môn học trong cơ sở dữ liệu.', 1;

        SET @tenKhoa = N'Ôn thi tốt nghiệp THPT 2027 - ' + @mon;
        SELECT @khoaId = KhoaHocID FROM KhoaHoc
        WHERE TenKhoaHoc = @tenKhoa AND MonHocID = @monId;
        IF @khoaId IS NULL
        BEGIN
            INSERT INTO KhoaHoc (TenKhoaHoc, MonHocID, NgayBatDau, NgayKetThuc, TrangThai)
            VALUES (@tenKhoa, @monId, '2026-11-01', '2027-06-30', N'HoatDong');
            SET @khoaId = CONVERT(INT, SCOPE_IDENTITY());
        END;

        SET @j = 1;
        WHILE @j <= 2
        BEGIN
            SET @lopId = NULL;
            SET @tenLop = N'Lớp 12' + CASE WHEN @j = 1 THEN N'A' ELSE N'B' END
                + N' - ' + @mon + N' (THPT 2027)';
            SELECT @lopId = LopHocID FROM LopHoc
            WHERE KhoaHocID = @khoaId AND TenLop = @tenLop;
            IF @lopId IS NULL
                INSERT INTO LopHoc (TenLop, KhoaHocID) VALUES (@tenLop, @khoaId);
            SET @j += 1;
        END;
        SET @i += 1;
    END;

    SET @i = 1;
    WHILE @i <= 60
    BEGIN
        SET @taiKhoanId = NULL;
        SET @hocVienId = NULL;
        SET @lopId = NULL;
        SET @tenDangNhap = N'demo2027_' + RIGHT(N'000' + CONVERT(NVARCHAR(3), @i), 3);
        SET @email = N'demo2027.' + RIGHT(N'000' + CONVERT(NVARCHAR(3), @i), 3) + N'@example.test';
        SELECT @hoTen = h.Ho + N' ' + g.Ten
        FROM @LastNames h CROSS JOIN @GivenNames g
        WHERE h.Stt = ((@i - 1) / 6) + 1 AND g.Stt = ((@i - 1) % 6) + 1;

        SELECT @taiKhoanId = TaiKhoanID FROM TaiKhoan WHERE TenDangNhap = @tenDangNhap;
        IF @taiKhoanId IS NULL
        BEGIN
            INSERT INTO TaiKhoan (TenDangNhap, MatKhau, Email, VaiTro, TrangThai, NgayTao)
            VALUES (@tenDangNhap, @passwordHash, @email, N'HocVien', N'HoatDong', SYSDATETIME());
            SET @taiKhoanId = CONVERT(INT, SCOPE_IDENTITY());
        END;

        IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TaiKhoanID = @taiKhoanId AND VaiTro = N'HocVien')
            THROW 51002, N'Tên đăng nhập demo đã được tài khoản vai trò khác sử dụng.', 1;
        SELECT @hocVienId = HocVienID FROM HocVien WHERE TaiKhoanID = @taiKhoanId;
        IF @hocVienId IS NULL
        BEGIN
            INSERT INTO HocVien (TaiKhoanID, HoTen, LoaiHocVien, NgaySinh, LoaiGoi)
            VALUES (@taiKhoanId, @hoTen, N'HocSinh', DATEADD(DAY, @i, CONVERT(DATE, '2008-01-01')), N'MienPhi');
            SET @hocVienId = CONVERT(INT, SCOPE_IDENTITY());
        END;

        SET @j = CASE WHEN ((@i - 1) % 6) < 3 THEN 1 ELSE 2 END;
        SELECT @mon = Mon FROM @Courses WHERE Stt = ((@i - 1) / 6) + 1;
        SET @tenKhoa = N'Ôn thi tốt nghiệp THPT 2027 - ' + @mon;
        SET @tenLop = N'Lớp 12' + CASE WHEN @j = 1 THEN N'A' ELSE N'B' END
            + N' - ' + @mon + N' (THPT 2027)';
        SELECT @lopId = l.LopHocID FROM LopHoc l
        JOIN KhoaHoc k ON k.KhoaHocID = l.KhoaHocID
        WHERE k.TenKhoaHoc = @tenKhoa AND l.TenLop = @tenLop;
        IF NOT EXISTS (SELECT 1 FROM HocVien_Lop WHERE HocVienID = @hocVienId AND LopHocID = @lopId)
            INSERT INTO HocVien_Lop (HocVienID, LopHocID, NgayThamGia, TrangThai)
            VALUES (@hocVienId, @lopId, SYSDATETIME(), N'DangHoc');
        SET @i += 1;
    END;

    COMMIT TRANSACTION;
    SELECT N'Đã có dữ liệu mẫu 2027' AS KetQua,
        (SELECT COUNT(*) FROM KhoaHoc WHERE TenKhoaHoc LIKE N'Ôn thi tốt nghiệp THPT 2027 - %') AS SoKhoa,
        (SELECT COUNT(*) FROM LopHoc WHERE TenLop LIKE N'Lớp 12% (THPT 2027)') AS SoLop,
        (SELECT COUNT(*) FROM TaiKhoan WHERE TenDangNhap LIKE N'demo2027[_]%') AS SoTaiKhoan,
        (SELECT COUNT(*) FROM HocVien_Lop hl JOIN HocVien hv ON hv.HocVienID = hl.HocVienID
         JOIN TaiKhoan tk ON tk.TaiKhoanID = hv.TaiKhoanID
         WHERE tk.TenDangNhap LIKE N'demo2027[_]%') AS SoXepLop;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
