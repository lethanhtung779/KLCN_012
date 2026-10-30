USE ThiTracNghiem;
GO

-- Cap nhat CHECK constraint tren cot HinhThucTao cua bang DeThi
-- de cho phep gia tri 'MaTranTHPTQG' (Tao de theo ma tran)

DECLARE @constraint sysname;
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
SELECT name FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID(N'dbo.DeThi')
  AND definition LIKE N'%HinhThucTao%'
  AND definition NOT LIKE N'%MaTranTHPTQG%';
OPEN cur;
FETCH NEXT FROM cur INTO @constraint;
WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @dropSql nvarchar(max) = N'ALTER TABLE dbo.DeThi DROP CONSTRAINT ' + QUOTENAME(@constraint);
    EXEC sp_executesql @dropSql;
    PRINT N'Da xoa constraint cu: ' + @constraint;
    FETCH NEXT FROM cur INTO @constraint;
END
CLOSE cur;
DEALLOCATE cur;

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.DeThi')
      AND definition LIKE N'%HinhThucTao%'
      AND definition LIKE N'%MaTranTHPTQG%'
)
BEGIN
    ALTER TABLE dbo.DeThi WITH CHECK ADD CONSTRAINT CK_DeThi_HinhThucTao
        CHECK (HinhThucTao IN (N'ThuCong', N'TuDong', N'MaTranTHPTQG'));
    PRINT N'Da them constraint moi: CK_DeThi_HinhThucTao (cho phep MaTranTHPTQG)';
END
ELSE
BEGIN
    PRINT N'Constraint da ton tai va ho tro MaTranTHPTQG.';
END
GO

