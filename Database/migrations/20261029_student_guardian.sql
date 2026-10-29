SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('HocVien', 'HoTenNguoiGiamHo') IS NULL ALTER TABLE HocVien ADD HoTenNguoiGiamHo NVARCHAR(500) NULL;
IF COL_LENGTH('HocVien', 'QuanHeNguoiGiamHo') IS NULL ALTER TABLE HocVien ADD QuanHeNguoiGiamHo NVARCHAR(500) NULL;
IF COL_LENGTH('HocVien', 'SdtNguoiGiamHo') IS NULL ALTER TABLE HocVien ADD SdtNguoiGiamHo NVARCHAR(500) NULL;

IF COL_LENGTH('HocVien', 'LaNguoiGiamHo') IS NOT NULL
BEGIN
    EXEC sp_executesql N'UPDATE HocVien SET HoTenNguoiGiamHo=COALESCE(HoTenNguoiGiamHo,NguoiLienHe), QuanHeNguoiGiamHo=COALESCE(QuanHeNguoiGiamHo,QuanHeLienHe), SdtNguoiGiamHo=COALESCE(SdtNguoiGiamHo,SdtLienHe) WHERE LaNguoiGiamHo=1';
    DECLARE @drop nvarchar(max);
    SELECT @drop=N'ALTER TABLE HocVien DROP CONSTRAINT '+QUOTENAME(dc.name)
    FROM sys.default_constraints dc JOIN sys.columns c ON c.default_object_id=dc.object_id
    WHERE c.object_id=OBJECT_ID('HocVien') AND c.name='LaNguoiGiamHo';
    IF @drop IS NOT NULL EXEC sp_executesql @drop;
    EXEC sp_executesql N'ALTER TABLE HocVien DROP COLUMN LaNguoiGiamHo';
END

-- Giữ dữ liệu cũ: nếu liên hệ khẩn cấp đã được khai rõ là cha hoặc mẹ
-- của học viên dưới 18 tuổi thì chuyển sang đúng nhóm thông tin phụ huynh.
UPDATE hv SET TinhTrangMe=N'Còn sống',HoTenMe=hv.NguoiLienHe,SdtMe=hv.SdtLienHe
FROM HocVien hv
WHERE hv.NgaySinh>DATEADD(YEAR,-18,CAST(GETDATE() AS DATE)) AND hv.QuanHeLienHe=N'Mẹ'
  AND NULLIF(LTRIM(RTRIM(hv.NguoiLienHe)),N'') IS NOT NULL AND NULLIF(LTRIM(RTRIM(hv.SdtLienHe)),N'') IS NOT NULL
  AND NOT (hv.TinhTrangMe=N'Còn sống' AND NULLIF(LTRIM(RTRIM(hv.HoTenMe)),N'') IS NOT NULL AND NULLIF(LTRIM(RTRIM(hv.SdtMe)),N'') IS NOT NULL);

UPDATE hv SET TinhTrangCha=N'Còn sống',HoTenCha=hv.NguoiLienHe,SdtCha=hv.SdtLienHe
FROM HocVien hv
WHERE hv.NgaySinh>DATEADD(YEAR,-18,CAST(GETDATE() AS DATE)) AND hv.QuanHeLienHe=N'Cha'
  AND NULLIF(LTRIM(RTRIM(hv.NguoiLienHe)),N'') IS NOT NULL AND NULLIF(LTRIM(RTRIM(hv.SdtLienHe)),N'') IS NOT NULL
  AND NOT (hv.TinhTrangCha=N'Còn sống' AND NULLIF(LTRIM(RTRIM(hv.HoTenCha)),N'') IS NOT NULL AND NULLIF(LTRIM(RTRIM(hv.SdtCha)),N'') IS NOT NULL);

COMMIT;
