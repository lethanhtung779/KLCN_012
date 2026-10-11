-- Sua ho ten bi loi font (chay bang SSMS hoac: chcp 65001 >nul & sqlcmd ... -f 65001 -i fix_unicode_hoten.sql)
USE ThiTracNghiem;
GO

UPDATE QuanTriVien
SET HoTen = N'Quản trị viên hệ thống'
WHERE TaiKhoanID IN (SELECT TaiKhoanID FROM TaiKhoan WHERE TenDangNhap = N'admin');
GO
