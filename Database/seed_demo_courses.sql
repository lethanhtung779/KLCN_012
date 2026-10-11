-- Seed du lieu mau cho KhoaHoc, LopHoc, PhanCongGiangDay, HocVien_Lop va LichHoc
USE ThiTracNghiem;
GO

IF NOT EXISTS (SELECT 1 FROM KhoaHoc WHERE TenKhoaHoc = N'Khóa Ôn luyện & Luyện thi Tốt nghiệp THPT Quốc gia 2025 - Chuyên đề Toán học')
BEGIN
    DECLARE @KhoaHocID INT, @LopHocID INT, @GiaoVienID INT, @MonHocID INT;

    -- 1. Lay MonHocID Toan
    SELECT TOP 1 @MonHocID = MonHocID FROM MonHoc WHERE TenMon LIKE N'%Toan%';
    IF @MonHocID IS NULL SET @MonHocID = 1;

    -- 2. Lay GiaoVienID
    SELECT TOP 1 @GiaoVienID = GiaoVienID FROM GiaoVien;
    IF @GiaoVienID IS NULL SET @GiaoVienID = 1;

    -- 3. Tao KhoaHoc
    INSERT INTO KhoaHoc (TenKhoaHoc, MonHocID, NgayBatDau, NgayKetThuc, TrangThai)
    VALUES (N'Khóa Ôn luyện & Luyện thi Tốt nghiệp THPT Quốc gia 2025 - Chuyên đề Toán học', @MonHocID, '2025-01-01', '2025-06-30', N'HoatDong');

    SET @KhoaHocID = SCOPE_IDENTITY();

    -- 4. Tao LopHoc
    INSERT INTO LopHoc (TenLop, KhoaHocID)
    VALUES (N'12A1 - Chuyên đề Nâng cao & Luyện đề chuẩn THPTQG', @KhoaHocID);

    SET @LopHocID = SCOPE_IDENTITY();

    -- 5. Phan cong giang day
    INSERT INTO PhanCongGiangDay (LopHocID, MonHocID, GiaoVienID)
    VALUES (@LopHocID, @MonHocID, @GiaoVienID);

    -- 6. Xep hoc vien vao lop
    INSERT INTO HocVien_Lop (HocVienID, LopHocID, NgayThamGia, TrangThai)
    SELECT TOP 5 HocVienID, @LopHocID, GETDATE(), N'DangHoc'
    FROM HocVien;

    -- 7. Lich hoc
    INSERT INTO LichHoc (LopHocID, NoiDung, ThoiGianBatDau, ThoiGianKetThuc, DiaDiem)
    VALUES 
    (@LopHocID, N'Chuyên đề 1: Khảo sát hàm số & đồ thị trong đề thi THPTQG', '2025-02-10 19:30:00', '2025-02-10 21:30:00', N'Phòng 301 - Cơ sở chính'),
    (@LopHocID, N'Chuyên đề 2: Tích phân & ứng dụng diện tích, thể tích', '2025-02-12 19:30:00', '2025-02-12 21:30:00', N'Phòng 301 - Cơ sở chính'),
    (@LopHocID, N'Chuyên đề 3: Phương pháp tọa độ trong không gian Oxyz', '2025-02-17 19:30:00', '2025-02-17 21:30:00', N'Trực tuyến (Zoom ID: 889 668 999)'),
    (@LopHocID, N'Chữa đề thi thử THPTQG số 01 & Phân tích bẫy đề thi', '2025-02-19 19:30:00', '2025-02-19 21:30:00', N'Phòng 301 - Cơ sở chính');
END
GO
