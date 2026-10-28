namespace QLThiTN.Shared;

public static class WorkflowSchema
{
    public static string Sql => """
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;
        DECLARE @lock int;
        EXEC @lock=sp_getapplock @Resource=N'QLThiTN.WorkflowSchema.v1',@LockMode='Exclusive',@LockOwner='Transaction';
        IF @lock<0 THROW 50001,N'Không khóa được cập nhật schema.',1;
        IF COL_LENGTH('DeThi','DaDuyet') IS NULL
            ALTER TABLE DeThi ADD DaDuyet bit NOT NULL CONSTRAINT DF_DeThi_DaDuyet DEFAULT 0;
        IF COL_LENGTH('DeThi','NguoiTaoID') IS NULL ALTER TABLE DeThi ADD NguoiTaoID int NULL REFERENCES TaiKhoan(TaiKhoanID);
        IF COL_LENGTH('DeThi','NguoiDuyetID') IS NULL ALTER TABLE DeThi ADD NguoiDuyetID int NULL REFERENCES TaiKhoan(TaiKhoanID);
        IF COL_LENGTH('DeThi','NgayDuyet') IS NULL ALTER TABLE DeThi ADD NgayDuyet datetime2 NULL;
        DECLARE @table sysname, @column sysname, @sql nvarchar(max);
        DECLARE fields CURSOR LOCAL FAST_FORWARD FOR
        SELECT t.n,c.n FROM (VALUES(N'HocVien'),(N'GiaoVien')) t(n)
        CROSS JOIN (VALUES(N'DiaChi'),(N'GioiTinh'),(N'HoTenCha'),(N'SdtCha'),(N'HoTenMe'),(N'SdtMe'),
          (N'NguoiLienHe'),(N'SdtLienHe'),(N'QuanHeLienHe'),(N'GhiChu')) c(n)
        UNION ALL SELECT N'GiaoVien',c.n FROM (VALUES(N'TrinhDoHocVan'),(N'BangCap'),(N'TruongHoc'),
          (N'ChuyenNganh'),(N'DonViCongTac')) c(n);
        OPEN fields; FETCH NEXT FROM fields INTO @table,@column;
        WHILE @@FETCH_STATUS=0
        BEGIN
            IF COL_LENGTH(@table,@column) IS NULL
            BEGIN
                SET @sql=N'ALTER TABLE '+QUOTENAME(@table)+N' ADD '+QUOTENAME(@column)+N' nvarchar(500) NULL';
                EXEC sp_executesql @sql;
            END
            FETCH NEXT FROM fields INTO @table,@column;
        END
        CLOSE fields; DEALLOCATE fields;
        IF COL_LENGTH('GiaoVien','NgaySinh') IS NULL ALTER TABLE GiaoVien ADD NgaySinh date NULL;
        IF COL_LENGTH('CauHoi','NhomTHPT') IS NULL ALTER TABLE CauHoi ADD NhomTHPT nvarchar(30) NOT NULL CONSTRAINT DF_CauHoi_NhomTHPT DEFAULT N'ChuaPhanLoai';
        IF COL_LENGTH('CauHoi','NguoiTaoID') IS NULL ALTER TABLE CauHoi ADD NguoiTaoID int NULL REFERENCES TaiKhoan(TaiKhoanID);

        -- Hợp nhất tác giả về tài khoản để dùng chung cho giáo viên và quản trị viên.
        IF COL_LENGTH('DeThi','GiaoVienID') IS NOT NULL
            EXEC sp_executesql N'UPDATE d SET NguoiTaoID=gv.TaiKhoanID FROM DeThi d JOIN GiaoVien gv ON gv.GiaoVienID=d.GiaoVienID WHERE d.NguoiTaoID IS NULL';
        IF COL_LENGTH('CauHoi','GiaoVienID') IS NOT NULL
            EXEC sp_executesql N'UPDATE c SET NguoiTaoID=gv.TaiKhoanID FROM CauHoi c JOIN GiaoVien gv ON gv.GiaoVienID=c.GiaoVienID WHERE c.NguoiTaoID IS NULL';
        EXEC sp_executesql N'IF EXISTS(SELECT 1 FROM DeThi WHERE NguoiTaoID IS NULL) THROW 50002,N''Có đề thi chưa xác định được tài khoản người tạo.'',1;';
        EXEC sp_executesql N'IF EXISTS(SELECT 1 FROM CauHoi WHERE NguoiTaoID IS NULL) THROW 50003,N''Có câu hỏi chưa xác định được tài khoản người tạo.'',1;';
        IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('DeThi') AND name='NguoiTaoID' AND is_nullable=1)
            EXEC sp_executesql N'ALTER TABLE DeThi ALTER COLUMN NguoiTaoID int NOT NULL';
        IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('CauHoi') AND name='NguoiTaoID' AND is_nullable=1)
            EXEC sp_executesql N'ALTER TABLE CauHoi ALTER COLUMN NguoiTaoID int NOT NULL';

        DECLARE @legacyTable sysname, @fk sysname, @index sysname;
        DECLARE legacy_tables CURSOR LOCAL FAST_FORWARD FOR SELECT n FROM (VALUES(N'DeThi'),(N'CauHoi')) v(n);
        OPEN legacy_tables; FETCH NEXT FROM legacy_tables INTO @legacyTable;
        WHILE @@FETCH_STATUS=0
        BEGIN
            IF COL_LENGTH(@legacyTable,'GiaoVienID') IS NOT NULL
            BEGIN
                SELECT TOP(1) @fk=fk.name FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id=fk.object_id
                  JOIN sys.columns c ON c.object_id=fkc.parent_object_id AND c.column_id=fkc.parent_column_id
                  WHERE fk.parent_object_id=OBJECT_ID(@legacyTable) AND c.name=N'GiaoVienID';
                IF @fk IS NOT NULL BEGIN SET @sql=N'ALTER TABLE '+QUOTENAME(@legacyTable)+N' DROP CONSTRAINT '+QUOTENAME(@fk); EXEC sp_executesql @sql; END
                DECLARE legacy_indexes CURSOR LOCAL FAST_FORWARD FOR
                  SELECT DISTINCT i.name FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
                  JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
                  WHERE i.object_id=OBJECT_ID(@legacyTable) AND c.name=N'GiaoVienID' AND i.is_primary_key=0 AND i.is_unique_constraint=0;
                OPEN legacy_indexes; FETCH NEXT FROM legacy_indexes INTO @index;
                WHILE @@FETCH_STATUS=0 BEGIN SET @sql=N'DROP INDEX '+QUOTENAME(@index)+N' ON '+QUOTENAME(@legacyTable); EXEC sp_executesql @sql; FETCH NEXT FROM legacy_indexes INTO @index; END
                CLOSE legacy_indexes; DEALLOCATE legacy_indexes;
                SET @sql=N'ALTER TABLE '+QUOTENAME(@legacyTable)+N' DROP COLUMN GiaoVienID'; EXEC sp_executesql @sql;
            END
            SET @fk=NULL; FETCH NEXT FROM legacy_tables INTO @legacyTable;
        END
        CLOSE legacy_tables; DEALLOCATE legacy_tables;

        DECLARE student_columns CURSOR LOCAL FAST_FORWARD FOR SELECT n FROM
          (VALUES(N'TrinhDoHocVan'),(N'BangCap'),(N'TruongHoc'),(N'ChuyenNganh'),(N'DonViCongTac')) v(n);
        OPEN student_columns; FETCH NEXT FROM student_columns INTO @column;
        WHILE @@FETCH_STATUS=0 BEGIN IF COL_LENGTH('HocVien',@column) IS NOT NULL BEGIN SET @sql=N'ALTER TABLE HocVien DROP COLUMN '+QUOTENAME(@column); EXEC sp_executesql @sql; END FETCH NEXT FROM student_columns INTO @column; END
        CLOSE student_columns; DEALLOCATE student_columns;

        IF EXISTS(SELECT 1 FROM sys.key_constraints kc JOIN sys.index_columns ic ON ic.object_id=kc.parent_object_id AND ic.index_id=kc.unique_index_id
          JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
          WHERE kc.parent_object_id=OBJECT_ID('MaDe_CauHoi') AND kc.name='UQ_MaDe_CauHoi_ThuTu' AND c.name='CauHoiID')
            ALTER TABLE MaDe_CauHoi DROP CONSTRAINT UQ_MaDe_CauHoi_ThuTu;
        IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('MaDe_CauHoi') AND name='UQ_MaDe_CauHoi_ThuTu')
            ALTER TABLE MaDe_CauHoi ADD CONSTRAINT UQ_MaDe_CauHoi_ThuTu UNIQUE(MaDeID,ThuTu);
        IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('CauHoi') AND name='IX_CauHoi_NguoiTao')
            EXEC sp_executesql N'CREATE INDEX IX_CauHoi_NguoiTao ON CauHoi(NguoiTaoID)';
        IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('DeThi') AND name='IX_DeThi_NguoiTao')
            EXEC sp_executesql N'CREATE INDEX IX_DeThi_NguoiTao ON DeThi(NguoiTaoID)';
        COMMIT;
        """ + InvalidationTriggers();

    private static string InvalidationTriggers()
    {
        var sql = new System.Text.StringBuilder();
        var sources = new Dictionary<string,string>
        {
            ["DeThi_CauHoi"] = "SELECT DeThiID FROM inserted UNION SELECT DeThiID FROM deleted",
            ["MaDeThi"] = "SELECT DeThiID FROM inserted UNION SELECT DeThiID FROM deleted",
            ["MaDe_CauHoi"] = "SELECT DeThiID FROM MaDeThi WHERE MaDeID IN (SELECT MaDeID FROM inserted UNION SELECT MaDeID FROM deleted)",
            ["MaDe_DapAn"] = "SELECT DeThiID FROM MaDeThi WHERE MaDeID IN (SELECT MaDeID FROM inserted UNION SELECT MaDeID FROM deleted)",
            ["CauHoi"] = "SELECT DeThiID FROM DeThi_CauHoi WHERE CauHoiID IN (SELECT CauHoiID FROM inserted UNION SELECT CauHoiID FROM deleted)",
            ["DapAn"] = "SELECT DeThiID FROM DeThi_CauHoi WHERE CauHoiID IN (SELECT CauHoiID FROM inserted UNION SELECT CauHoiID FROM deleted)",
            ["HinhAnh"] = "SELECT DeThiID FROM DeThi_CauHoi WHERE CauHoiID IN (SELECT CauHoiID FROM inserted UNION SELECT CauHoiID FROM deleted UNION SELECT CauHoiID FROM DapAn WHERE DapAnID IN (SELECT DapAnID FROM inserted UNION SELECT DapAnID FROM deleted))"
        };
        foreach (var (table, query) in sources)
        {
            var trigger = $"CREATE OR ALTER TRIGGER dbo.TR_Review_{table} ON dbo.[{table}] AFTER INSERT,UPDATE,DELETE AS BEGIN SET NOCOUNT ON; UPDATE DeThi SET DaDuyet=0,NguoiDuyetID=NULL,NgayDuyet=NULL WHERE DeThiID IN ({query}); END";
            sql.Append("EXEC(N'").Append(trigger.Replace("'","''")).AppendLine("');");
        }
        sql.AppendLine("EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_Review_DeThi ON dbo.DeThi AFTER UPDATE AS BEGIN SET NOCOUNT ON; IF UPDATE(TenDe) OR UPDATE(MonHocID) OR UPDATE(SoLuongCauHoi) OR UPDATE(HinhThucTao) UPDATE DeThi SET DaDuyet=0,NguoiDuyetID=NULL,NgayDuyet=NULL WHERE DeThiID IN (SELECT DeThiID FROM inserted); END');");
        return sql.ToString();
    }
}
