USE ThiTracNghiem;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('HocVien','NguonTaiKhoan') IS NULL
BEGIN
    ALTER TABLE HocVien ADD NguonTaiKhoan NVARCHAR(20) NOT NULL
        CONSTRAINT DF_HocVien_NguonTaiKhoan DEFAULT N'TrungTam';

    -- Dữ liệu cũ: tài khoản tự do hoặc đã xác minh đăng ký Web là vãng lai.
    EXEC(N'UPDATE HocVien SET NguonTaiKhoan=N''VangLai'' WHERE LoaiHocVien=N''TuDo''');
    IF OBJECT_ID(N'dbo.DangKyXacMinhEmail',N'U') IS NOT NULL
        EXEC(N'UPDATE hv SET NguonTaiKhoan=N''VangLai'' FROM HocVien hv JOIN TaiKhoan tk ON tk.TaiKhoanID=hv.TaiKhoanID WHERE EXISTS(SELECT 1 FROM DangKyXacMinhEmail d WHERE d.Email=tk.Email AND d.DaSuDung=1)');

    -- Quan hệ lớp do logic cũ tạo cho vãng lai là không hợp lệ theo nghiệp vụ mới.
    EXEC(N'DELETE hvl FROM HocVien_Lop hvl JOIN HocVien hv ON hv.HocVienID=hvl.HocVienID WHERE hv.NguonTaiKhoan=N''VangLai''');
END;

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.HocVien') AND name=N'CK_HocVien_NguonTaiKhoan')
    EXEC(N'ALTER TABLE HocVien WITH CHECK ADD CONSTRAINT CK_HocVien_NguonTaiKhoan CHECK (NguonTaiKhoan IN (N''VangLai'',N''TrungTam''))');

EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_HocVienLop_ChiHocVienTrungTam ON dbo.HocVien_Lop AFTER INSERT, UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.HocVien hv ON hv.HocVienID=i.HocVienID WHERE hv.NguonTaiKhoan<>N''TrungTam'')
        THROW 51020,N''Tài khoản học viên vãng lai không được xếp vào lớp trung tâm. Hãy tạo tài khoản học viên trung tâm mới.'',1;
END');

COMMIT TRANSACTION;
GO
