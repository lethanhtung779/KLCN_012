-- Migration: 20261103_dotthi_access_control.sql
-- Chuyển phân quyền đối tượng dự thi (Miễn phí / VIP) từ Đề thi sang Đợt thi (DotThi)

IF COL_LENGTH('DotThi', 'LaMienPhi') IS NULL
BEGIN
    ALTER TABLE DotThi ADD LaMienPhi BIT NOT NULL CONSTRAINT DF_DotThi_LaMienPhi DEFAULT 0;
    
    -- Đồng bộ dữ liệu phân quyền ban đầu từ DeThi sang DotThi
    EXEC(N'UPDATE dt SET dt.LaMienPhi = d.LaMienPhi FROM DotThi dt JOIN DeThi d ON d.DeThiID = dt.DeThiID');
    
    PRINT N'Đã thêm cột LaMienPhi vào bảng DotThi và đồng bộ dữ liệu.';
END
ELSE
BEGIN
    PRINT N'Cột LaMienPhi đã tồn tại trong bảng DotThi.';
END
GO
