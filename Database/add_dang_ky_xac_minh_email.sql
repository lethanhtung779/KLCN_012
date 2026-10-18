-- Bang luu yeu cau dang ky cho den khi xac minh email
USE ThiTracNghiem;
GO

IF OBJECT_ID(N'dbo.DangKyXacMinhEmail', N'U') IS NULL
BEGIN
    CREATE TABLE DangKyXacMinhEmail (
        DangKyID      INT IDENTITY(1,1) PRIMARY KEY,
        Email         NVARCHAR(100) NOT NULL,
        TenDangNhap   NVARCHAR(50)  NOT NULL,
        MatKhauHash   NVARCHAR(255) NOT NULL,
        HoTen         NVARCHAR(100) NOT NULL,
        Token         NVARCHAR(128) NOT NULL,
        HetHan        DATETIME2     NOT NULL,
        DaSuDung      BIT           NOT NULL DEFAULT 0,
        NgayTao       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_DangKyXacMinhEmail_Token UNIQUE (Token)
    );

    CREATE INDEX IX_DangKyXacMinhEmail_Email ON DangKyXacMinhEmail (Email);
END
GO
