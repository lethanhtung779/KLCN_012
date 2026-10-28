USE ThiTracNghiem;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('DeThi','NguoiTaoID') IS NULL
    ALTER TABLE DeThi ADD NguoiTaoID int NULL CONSTRAINT FK_DeThi_NguoiTao REFERENCES TaiKhoan(TaiKhoanID);
IF COL_LENGTH('CauHoi','NguoiTaoID') IS NULL
    ALTER TABLE CauHoi ADD NguoiTaoID int NULL CONSTRAINT FK_CauHoi_NguoiTao REFERENCES TaiKhoan(TaiKhoanID);

IF COL_LENGTH('DeThi','GiaoVienID') IS NOT NULL
    EXEC sp_executesql N'UPDATE d SET NguoiTaoID=gv.TaiKhoanID FROM DeThi d JOIN GiaoVien gv ON gv.GiaoVienID=d.GiaoVienID WHERE d.NguoiTaoID IS NULL';
IF COL_LENGTH('CauHoi','GiaoVienID') IS NOT NULL
    EXEC sp_executesql N'UPDATE c SET NguoiTaoID=gv.TaiKhoanID FROM CauHoi c JOIN GiaoVien gv ON gv.GiaoVienID=c.GiaoVienID WHERE c.NguoiTaoID IS NULL';

IF EXISTS(SELECT 1 FROM DeThi WHERE NguoiTaoID IS NULL)
    THROW 50002,N'Có đề thi chưa xác định được tài khoản người tạo.',1;
IF EXISTS(SELECT 1 FROM CauHoi WHERE NguoiTaoID IS NULL)
    THROW 50003,N'Có câu hỏi chưa xác định được tài khoản người tạo.',1;

IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('DeThi') AND name='NguoiTaoID' AND is_nullable=1)
    ALTER TABLE DeThi ALTER COLUMN NguoiTaoID int NOT NULL;
IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('CauHoi') AND name='NguoiTaoID' AND is_nullable=1)
    ALTER TABLE CauHoi ALTER COLUMN NguoiTaoID int NOT NULL;

DECLARE @table sysname,@fk sysname,@index sysname,@sql nvarchar(max);
DECLARE legacy_tables CURSOR LOCAL FAST_FORWARD FOR SELECT n FROM (VALUES(N'DeThi'),(N'CauHoi')) v(n);
OPEN legacy_tables; FETCH NEXT FROM legacy_tables INTO @table;
WHILE @@FETCH_STATUS=0
BEGIN
    IF COL_LENGTH(@table,'GiaoVienID') IS NOT NULL
    BEGIN
        SET @fk=NULL;
        SELECT TOP(1) @fk=fk.name
        FROM sys.foreign_keys fk
        JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id=fk.object_id
        JOIN sys.columns c ON c.object_id=fkc.parent_object_id AND c.column_id=fkc.parent_column_id
        WHERE fk.parent_object_id=OBJECT_ID(@table) AND c.name=N'GiaoVienID';
        IF @fk IS NOT NULL
        BEGIN
            SET @sql=N'ALTER TABLE '+QUOTENAME(@table)+N' DROP CONSTRAINT '+QUOTENAME(@fk);
            EXEC sp_executesql @sql;
        END
        DECLARE legacy_indexes CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT i.name
        FROM sys.indexes i
        JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
        JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
        WHERE i.object_id=OBJECT_ID(@table) AND c.name=N'GiaoVienID'
          AND i.is_primary_key=0 AND i.is_unique_constraint=0;
        OPEN legacy_indexes; FETCH NEXT FROM legacy_indexes INTO @index;
        WHILE @@FETCH_STATUS=0
        BEGIN
            SET @sql=N'DROP INDEX '+QUOTENAME(@index)+N' ON '+QUOTENAME(@table);
            EXEC sp_executesql @sql;
            FETCH NEXT FROM legacy_indexes INTO @index;
        END
        CLOSE legacy_indexes; DEALLOCATE legacy_indexes;
        SET @sql=N'ALTER TABLE '+QUOTENAME(@table)+N' DROP COLUMN GiaoVienID';
        EXEC sp_executesql @sql;
    END
    FETCH NEXT FROM legacy_tables INTO @table;
END
CLOSE legacy_tables; DEALLOCATE legacy_tables;

DECLARE @column sysname;
DECLARE student_columns CURSOR LOCAL FAST_FORWARD FOR SELECT n FROM
    (VALUES(N'TrinhDoHocVan'),(N'BangCap'),(N'TruongHoc'),(N'ChuyenNganh'),(N'DonViCongTac')) v(n);
OPEN student_columns; FETCH NEXT FROM student_columns INTO @column;
WHILE @@FETCH_STATUS=0
BEGIN
    IF COL_LENGTH('HocVien',@column) IS NOT NULL
    BEGIN
        SET @sql=N'ALTER TABLE HocVien DROP COLUMN '+QUOTENAME(@column);
        EXEC sp_executesql @sql;
    END
    FETCH NEXT FROM student_columns INTO @column;
END
CLOSE student_columns; DEALLOCATE student_columns;

IF EXISTS(
    SELECT 1 FROM sys.key_constraints kc
    JOIN sys.index_columns ic ON ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id
    JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
    WHERE kc.parent_object_id=OBJECT_ID('MaDe_CauHoi')
      AND kc.name='UQ_MaDe_CauHoi_ThuTu' AND c.name='CauHoiID')
    ALTER TABLE MaDe_CauHoi DROP CONSTRAINT UQ_MaDe_CauHoi_ThuTu;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('MaDe_CauHoi') AND name='UQ_MaDe_CauHoi_ThuTu')
    ALTER TABLE MaDe_CauHoi ADD CONSTRAINT UQ_MaDe_CauHoi_ThuTu UNIQUE(MaDeID,ThuTu);

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('CauHoi') AND name='IX_CauHoi_NguoiTao')
    CREATE INDEX IX_CauHoi_NguoiTao ON CauHoi(NguoiTaoID);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('DeThi') AND name='IX_DeThi_NguoiTao')
    CREATE INDEX IX_DeThi_NguoiTao ON DeThi(NguoiTaoID);

COMMIT;
GO

-- Các đối tượng này chỉ phục vụ Database Diagrams của SSMS, không thuộc ứng dụng.
DROP PROCEDURE IF EXISTS dbo.sp_alterdiagram;
DROP PROCEDURE IF EXISTS dbo.sp_creatediagram;
DROP PROCEDURE IF EXISTS dbo.sp_dropdiagram;
DROP PROCEDURE IF EXISTS dbo.sp_helpdiagramdefinition;
DROP PROCEDURE IF EXISTS dbo.sp_helpdiagrams;
DROP PROCEDURE IF EXISTS dbo.sp_renamediagram;
DROP PROCEDURE IF EXISTS dbo.sp_upgraddiagrams;
DROP FUNCTION IF EXISTS dbo.fn_diagramobjects;
DROP TABLE IF EXISTS dbo.sysdiagrams;
GO
