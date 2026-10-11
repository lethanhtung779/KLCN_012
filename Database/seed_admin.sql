-- Tai khoan QuanTriVien mac dinh + dat lai mat khau giaovien1 (neu co)
-- Mat khau: SHA-256 hex, UTF-16LE (PasswordHelper API / WinForms)
USE ThiTracNghiem;
GO

DECLARE @hashAdmin NVARCHAR(255) = N'9D39DD891B174041B3488557421FAE0F8D551E1F612725717D820BDBB111530F'; -- admin123
DECLARE @hashGv NVARCHAR(255) = N'BFF9C84C019EF2FB9C413B3194B0CCFB44E001C9DA1688622AD5C98E793F4801';   -- Abc@123456

IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'admin')
BEGIN
    INSERT INTO TaiKhoan (TenDangNhap, MatKhau, Email, VaiTro, TrangThai, NgayTao)
    VALUES (N'admin', @hashAdmin, N'admin@qlthitn.local', N'QuanTriVien', N'HoatDong', SYSDATETIME());

    INSERT INTO QuanTriVien (TaiKhoanID, HoTen)
    SELECT TaiKhoanID, N'Quản trị viên hệ thống' FROM TaiKhoan WHERE TenDangNhap = N'admin';
    PRINT N'Created: admin / admin123 (QuanTriVien)';
END
ELSE
BEGIN
    UPDATE TaiKhoan
    SET MatKhau = @hashAdmin, VaiTro = N'QuanTriVien', TrangThai = N'HoatDong'
    WHERE TenDangNhap = N'admin';

    IF NOT EXISTS (SELECT 1 FROM QuanTriVien q INNER JOIN TaiKhoan t ON t.TaiKhoanID = q.TaiKhoanID WHERE t.TenDangNhap = N'admin')
        INSERT INTO QuanTriVien (TaiKhoanID, HoTen)
        SELECT TaiKhoanID, N'Quản trị viên hệ thống' FROM TaiKhoan WHERE TenDangNhap = N'admin';
    PRINT N'Updated: admin / admin123 (QuanTriVien)';
END

IF EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'giaovien1')
BEGIN
    UPDATE TaiKhoan SET MatKhau = @hashGv WHERE TenDangNhap = N'giaovien1';
    PRINT N'Updated: giaovien1 / Abc@123456 (GiaoVien)';
END
GO
