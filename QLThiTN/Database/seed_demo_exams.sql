-- Seed demo DeThi + DotThi de web co du lieu hien thi (chi tao khi chua co)
USE ThiTracNghiem;
GO

IF NOT EXISTS (SELECT 1 FROM DeThi)
BEGIN
    DECLARE @NguoiTaoID INT = (SELECT TOP (1) TaiKhoanID FROM TaiKhoan WHERE VaiTro IN (N'QuanTriVien',N'GiaoVien') ORDER BY CASE WHEN VaiTro=N'QuanTriVien' THEN 0 ELSE 1 END, TaiKhoanID);
    INSERT INTO DeThi (MonHocID, NguoiTaoID, TenDe, LoaiDe, SoLuongCauHoi, HinhThucTao)
    VALUES (1, @NguoiTaoID, N'Thithu Toan - De 1', N'ThiThu', 10, N'TuDong');
    INSERT INTO DeThi (MonHocID, NguoiTaoID, TenDe, LoaiDe, SoLuongCauHoi, HinhThucTao)
    VALUES (3, @NguoiTaoID, N'Thithu Hoa Hoc - De 1', N'ThiThu', 10, N'TuDong');
    INSERT INTO DeThi (MonHocID, NguoiTaoID, TenDe, LoaiDe, SoLuongCauHoi, HinhThucTao)
    VALUES (7, @NguoiTaoID, N'On tap Tin Hoc - Tuan 1', N'OnTap', 10, N'TuDong');

    -- Gan 10 cau hoi cho tung de (lay tu mon tuong ung, chi cau trac nghiem, tron ngau nhien)
    ;WITH picked AS (
        SELECT d.DeThiID, c.CauHoiID,
               ROW_NUMBER() OVER (PARTITION BY d.DeThiID ORDER BY NEWID()) AS rn
        FROM DeThi d
        JOIN ChuDe cd ON cd.MonHocID = d.MonHocID
        JOIN CauHoi c ON c.ChuDeID = cd.ChuDeID
        WHERE c.TrangThai = N'HoatDong' AND c.LoaiCauHoi = N'TracNghiem'
    )
    INSERT INTO DeThi_CauHoi (DeThiID, CauHoiID, ThuTu)
    SELECT DeThiID, CauHoiID, rn
    FROM picked
    WHERE rn <= 10
    ORDER BY DeThiID, rn;

    UPDATE DeThi SET SoLuongCauHoi = (
        SELECT COUNT(*) FROM DeThi_CauHoi x WHERE x.DeThiID = DeThi.DeThiID);
END
GO

IF NOT EXISTS (SELECT 1 FROM DotThi)
BEGIN
    INSERT INTO DotThi (DeThiID, TenDotThi, ThoiGianMoCong, ThoiGianDongCong, ThoiLuongLamBai, TrangThai, PhamVi, SoLanThiToiDa)
    SELECT DeThiID, N'Dot thi ' + TenDe,
           DATEADD(day, -1, GETDATE()),
           DATEADD(day, 7, GETDATE()),
           45, N'DangMo', N'ToanTruong', 1
    FROM DeThi;
END
GO

SELECT 'DeThi=' + CONVERT(NVARCHAR(10), COUNT(*)) FROM DeThi;
SELECT 'DotThi=' + CONVERT(NVARCHAR(10), COUNT(*)) FROM DotThi;
GO
