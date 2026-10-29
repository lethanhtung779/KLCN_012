-- Tao co so du lieu trung tam
-- An toan khi chay lai: chi tao nhung thu chua ton tai

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'ThiTracNghiem') IS NULL
BEGIN
    CREATE DATABASE ThiTracNghiem;
END
GO

USE ThiTracNghiem;
GO

IF OBJECT_ID(N'dbo.TaiKhoan', N'U') IS NULL
BEGIN
    CREATE TABLE TaiKhoan (
        TaiKhoanID    INT IDENTITY(1,1) PRIMARY KEY,
        TenDangNhap   NVARCHAR(50)  NOT NULL UNIQUE,
        MatKhau       NVARCHAR(255) NOT NULL,
        Email         NVARCHAR(100) NOT NULL UNIQUE,
        VaiTro        NVARCHAR(20)  NOT NULL CHECK (VaiTro IN (N'GiaoVien', N'QuanTriVien', N'HocVien')),
        TrangThai     NVARCHAR(20)  NOT NULL CHECK (TrangThai IN (N'HoatDong', N'Khoa')),
        NgayTao       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

IF OBJECT_ID(N'dbo.MonHoc', N'U') IS NULL
BEGIN
    CREATE TABLE MonHoc (
        MonHocID  INT IDENTITY(1,1) PRIMARY KEY,
        TenMon    NVARCHAR(100) NOT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.GiaoVien', N'U') IS NULL
BEGIN
    CREATE TABLE GiaoVien (
        GiaoVienID   INT IDENTITY(1,1) PRIMARY KEY,
        TaiKhoanID   INT          NOT NULL UNIQUE,
        HoTen        NVARCHAR(100) NOT NULL,
        SoDienThoai  NVARCHAR(20)  NULL,
        DiaChi       NVARCHAR(500) NULL,
        GioiTinh     NVARCHAR(500) NULL,
        TrinhDoHocVan NVARCHAR(500) NULL,
        BangCap      NVARCHAR(500) NULL,
        TruongHoc    NVARCHAR(500) NULL,
        HoTenCha     NVARCHAR(500) NULL,
        SdtCha       NVARCHAR(500) NULL,
        HoTenMe      NVARCHAR(500) NULL,
        SdtMe        NVARCHAR(500) NULL,
        NguoiLienHe  NVARCHAR(500) NULL,
        SdtLienHe    NVARCHAR(500) NULL,
        QuanHeLienHe NVARCHAR(500) NULL,
        ChuyenNganh  NVARCHAR(500) NULL,
        DonViCongTac NVARCHAR(500) NULL,
        GhiChu       NVARCHAR(500) NULL,
        NgaySinh     DATE NULL,
        TinhTrangCha NVARCHAR(30) NOT NULL CONSTRAINT DF_GiaoVien_TinhTrangCha DEFAULT N'Không rõ thông tin',
        TinhTrangMe  NVARCHAR(30) NOT NULL CONSTRAINT DF_GiaoVien_TinhTrangMe DEFAULT N'Không rõ thông tin',
        CONSTRAINT FK_GiaoVien_TaiKhoan FOREIGN KEY (TaiKhoanID) REFERENCES TaiKhoan(TaiKhoanID)
    );
END
GO

IF OBJECT_ID(N'dbo.QuanTriVien', N'U') IS NULL
BEGIN
    CREATE TABLE QuanTriVien (
        QuanTriVienID INT IDENTITY(1,1) PRIMARY KEY,
        TaiKhoanID    INT           NOT NULL UNIQUE,
        HoTen         NVARCHAR(100) NOT NULL,
        SoDienThoai   NVARCHAR(20)  NULL,
        CONSTRAINT FK_QuanTriVien_TaiKhoan FOREIGN KEY (TaiKhoanID) REFERENCES TaiKhoan(TaiKhoanID)
    );
END
GO

IF OBJECT_ID(N'dbo.HocVien', N'U') IS NULL
BEGIN
    CREATE TABLE HocVien (
        HocVienID    INT IDENTITY(1,1) PRIMARY KEY,
        TaiKhoanID   INT           NOT NULL UNIQUE,
        HoTen        NVARCHAR(100) NOT NULL,
        LoaiHocVien  NVARCHAR(50)  NOT NULL CHECK (LoaiHocVien IN (N'HocSinh', N'SinhVien', N'TuDo')),
        NgaySinh     DATE          NULL,
        NgayDangKy   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        SoDienThoai  NVARCHAR(20)  NULL,
        LoaiGoi      NVARCHAR(20)  NOT NULL CONSTRAINT DF_HocVien_LoaiGoi DEFAULT N'MienPhi',
        DiaChi       NVARCHAR(500) NULL,
        GioiTinh     NVARCHAR(500) NULL,
        HoTenCha     NVARCHAR(500) NULL,
        SdtCha       NVARCHAR(500) NULL,
        HoTenMe      NVARCHAR(500) NULL,
        SdtMe        NVARCHAR(500) NULL,
        NguoiLienHe  NVARCHAR(500) NULL,
        SdtLienHe    NVARCHAR(500) NULL,
        QuanHeLienHe NVARCHAR(500) NULL,
        GhiChu       NVARCHAR(500) NULL,
        TinhTrangCha NVARCHAR(30)  NOT NULL DEFAULT (NCHAR(75)+NCHAR(104)+NCHAR(244)+NCHAR(110)+NCHAR(103)+NCHAR(32)+NCHAR(114)+NCHAR(245)+NCHAR(32)+NCHAR(116)+NCHAR(104)+NCHAR(244)+NCHAR(110)+NCHAR(103)+NCHAR(32)+NCHAR(116)+NCHAR(105)+NCHAR(110)),
        TinhTrangMe  NVARCHAR(30)  NOT NULL DEFAULT (NCHAR(75)+NCHAR(104)+NCHAR(244)+NCHAR(110)+NCHAR(103)+NCHAR(32)+NCHAR(114)+NCHAR(245)+NCHAR(32)+NCHAR(116)+NCHAR(104)+NCHAR(244)+NCHAR(110)+NCHAR(103)+NCHAR(32)+NCHAR(116)+NCHAR(105)+NCHAR(110)),
        HoTenNguoiGiamHo NVARCHAR(500) NULL,
        QuanHeNguoiGiamHo NVARCHAR(500) NULL,
        SdtNguoiGiamHo NVARCHAR(500) NULL,
        CONSTRAINT FK_HocVien_TaiKhoan FOREIGN KEY (TaiKhoanID) REFERENCES TaiKhoan(TaiKhoanID)
    );
END
GO

IF COL_LENGTH('HocVien', 'HoTenNguoiGiamHo') IS NULL ALTER TABLE HocVien ADD HoTenNguoiGiamHo NVARCHAR(500) NULL;
IF COL_LENGTH('HocVien', 'QuanHeNguoiGiamHo') IS NULL ALTER TABLE HocVien ADD QuanHeNguoiGiamHo NVARCHAR(500) NULL;
IF COL_LENGTH('HocVien', 'SdtNguoiGiamHo') IS NULL ALTER TABLE HocVien ADD SdtNguoiGiamHo NVARCHAR(500) NULL;
GO

-- Đồng bộ các cột hồ sơ khi chạy script trên database phiên bản cũ.
DECLARE @ProfileTable SYSNAME, @ProfileColumn SYSNAME, @ProfileSql NVARCHAR(MAX);
DECLARE ProfileColumns CURSOR LOCAL FAST_FORWARD FOR
SELECT t.n,c.n FROM (VALUES(N'HocVien'),(N'GiaoVien')) t(n)
CROSS JOIN (VALUES(N'DiaChi'),(N'GioiTinh'),(N'HoTenCha'),(N'SdtCha'),(N'HoTenMe'),(N'SdtMe'),
    (N'NguoiLienHe'),(N'SdtLienHe'),(N'QuanHeLienHe'),(N'GhiChu')) c(n)
UNION ALL SELECT N'GiaoVien',c.n FROM (VALUES(N'TrinhDoHocVan'),(N'BangCap'),(N'TruongHoc'),
    (N'ChuyenNganh'),(N'DonViCongTac')) c(n);
OPEN ProfileColumns; FETCH NEXT FROM ProfileColumns INTO @ProfileTable,@ProfileColumn;
WHILE @@FETCH_STATUS=0
BEGIN
    IF COL_LENGTH(@ProfileTable,@ProfileColumn) IS NULL
    BEGIN
        SET @ProfileSql=N'ALTER TABLE '+QUOTENAME(@ProfileTable)+N' ADD '+QUOTENAME(@ProfileColumn)+N' NVARCHAR(500) NULL';
        EXEC sp_executesql @ProfileSql;
    END
    FETCH NEXT FROM ProfileColumns INTO @ProfileTable,@ProfileColumn;
END
CLOSE ProfileColumns; DEALLOCATE ProfileColumns;
IF COL_LENGTH('HocVien','LoaiGoi') IS NULL ALTER TABLE HocVien ADD LoaiGoi NVARCHAR(20) NOT NULL CONSTRAINT DF_HocVien_LoaiGoi DEFAULT N'MienPhi';
IF COL_LENGTH('GiaoVien','NgaySinh') IS NULL ALTER TABLE GiaoVien ADD NgaySinh DATE NULL;
IF COL_LENGTH('GiaoVien','TinhTrangCha') IS NULL ALTER TABLE GiaoVien ADD TinhTrangCha NVARCHAR(30) NOT NULL CONSTRAINT DF_GiaoVien_TinhTrangCha DEFAULT N'Không rõ thông tin';
IF COL_LENGTH('GiaoVien','TinhTrangMe') IS NULL ALTER TABLE GiaoVien ADD TinhTrangMe NVARCHAR(30) NOT NULL CONSTRAINT DF_GiaoVien_TinhTrangMe DEFAULT N'Không rõ thông tin';
GO

-- Chuẩn hóa hồ sơ dưới 18 tuổi đã dùng cha/mẹ làm liên hệ khẩn cấp.
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
GO

IF OBJECT_ID(N'dbo.KhoaHoc', N'U') IS NULL
BEGIN
    CREATE TABLE KhoaHoc (
        KhoaHocID   INT IDENTITY(1,1) PRIMARY KEY,
        TenKhoaHoc  NVARCHAR(100) NOT NULL,
        MonHocID    INT           NOT NULL,
        NgayBatDau  DATE          NULL,
        NgayKetThuc DATE          NULL,
        TrangThai   NVARCHAR(20)  NOT NULL CHECK (TrangThai IN (N'HoatDong', N'TamDung', N'DaKetThuc')),
        CONSTRAINT FK_KhoaHoc_MonHoc FOREIGN KEY (MonHocID) REFERENCES MonHoc(MonHocID)
    );
END
GO

IF OBJECT_ID(N'dbo.LopHoc', N'U') IS NULL
BEGIN
    CREATE TABLE LopHoc (
        LopHocID   INT IDENTITY(1,1) PRIMARY KEY,
        TenLop     NVARCHAR(100) NOT NULL,
        KhoaHocID  INT           NOT NULL,
        CONSTRAINT FK_LopHoc_KhoaHoc FOREIGN KEY (KhoaHocID) REFERENCES KhoaHoc(KhoaHocID)
    );
END
GO

IF OBJECT_ID(N'dbo.ChuDe', N'U') IS NULL
BEGIN
    CREATE TABLE ChuDe (
        ChuDeID   INT IDENTITY(1,1) PRIMARY KEY,
        MonHocID  INT           NOT NULL,
        TenChuDe  NVARCHAR(200) NOT NULL,
        CONSTRAINT FK_ChuDe_MonHoc FOREIGN KEY (MonHocID) REFERENCES MonHoc(MonHocID)
    );
END
GO

IF OBJECT_ID(N'dbo.GiaoVien_MonHoc', N'U') IS NULL
BEGIN
    CREATE TABLE GiaoVien_MonHoc (
        GiaoVienID INT NOT NULL,
        MonHocID   INT NOT NULL,
        CONSTRAINT PK_GiaoVien_MonHoc PRIMARY KEY (GiaoVienID, MonHocID),
        CONSTRAINT FK_GVMH_GiaoVien FOREIGN KEY (GiaoVienID) REFERENCES GiaoVien(GiaoVienID),
        CONSTRAINT FK_GVMH_MonHoc   FOREIGN KEY (MonHocID)   REFERENCES MonHoc(MonHocID)
    );
END
GO

IF OBJECT_ID(N'dbo.CauHoi', N'U') IS NULL
BEGIN
    CREATE TABLE CauHoi (
        CauHoiID        INT IDENTITY(1,1) PRIMARY KEY,
        ChuDeID         INT           NOT NULL,
        NguoiTaoID      INT           NOT NULL,
        NoiDung         NVARCHAR(MAX) NOT NULL,
        LoaiCauHoi      NVARCHAR(50)  NOT NULL,
        MucDoKho       NVARCHAR(20)  NOT NULL,
        Diem            DECIMAL(5,2)  NOT NULL DEFAULT 0.25,
        TrangThai       NVARCHAR(20)  NOT NULL,
        NgayTao         DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        GiaiThichDapAn  NVARCHAR(MAX) NULL,
        NhomTHPT        NVARCHAR(30)  NOT NULL CONSTRAINT DF_CauHoi_NhomTHPT DEFAULT N'ChuaPhanLoai',
        CONSTRAINT CK_CauHoi_MucDoKho   CHECK (MucDoKho IN (N'ChuaXacDinh', N'NhanBiet', N'ThongHieu', N'VanDung', N'VanDungCao')),
        CONSTRAINT CK_CauHoi_TrangThai  CHECK (TrangThai IN (N'HoatDong', N'An')),
        CONSTRAINT CK_CauHoi_LoaiCauHoi CHECK (LoaiCauHoi IN (N'TracNghiem', N'DungSai', N'TraLoiNgan')),
        CONSTRAINT FK_CauHoi_ChuDe    FOREIGN KEY (ChuDeID)    REFERENCES ChuDe(ChuDeID),
        CONSTRAINT FK_CauHoi_NguoiTao FOREIGN KEY (NguoiTaoID) REFERENCES TaiKhoan(TaiKhoanID)
    );
END
GO

IF COL_LENGTH('CauHoi', 'NhomTHPT') IS NULL
    ALTER TABLE CauHoi ADD NhomTHPT NVARCHAR(30) NOT NULL CONSTRAINT DF_CauHoi_NhomTHPT DEFAULT N'ChuaPhanLoai';
GO

IF OBJECT_ID(N'dbo.DapAn', N'U') IS NULL
BEGIN
    CREATE TABLE DapAn (
        DapAnID      INT IDENTITY(1,1) PRIMARY KEY,
        CauHoiID     INT           NOT NULL,
        ThuTu        INT           NOT NULL DEFAULT 0,
        NoiDung      NVARCHAR(MAX) NOT NULL,
        LaDapAnDung  BIT           NOT NULL DEFAULT 0,
        GiaiThich    NVARCHAR(MAX) NULL,
        CONSTRAINT FK_DapAn_CauHoi FOREIGN KEY (CauHoiID) REFERENCES CauHoi(CauHoiID)
    );
END
GO

IF OBJECT_ID(N'dbo.HinhAnh', N'U') IS NULL
BEGIN
    CREATE TABLE HinhAnh (
        HinhAnhID   INT IDENTITY(1,1) PRIMARY KEY,
        CauHoiID    INT           NULL,
        DapAnID     INT           NULL,
        DuongDan    NVARCHAR(500) NOT NULL,
        MoTa        NVARCHAR(200) NULL,
        ThuTu       INT           NOT NULL DEFAULT 0,
        CONSTRAINT CHK_HinhAnh_Nguon CHECK (CauHoiID IS NOT NULL OR DapAnID IS NOT NULL),
        CONSTRAINT FK_HinhAnh_CauHoi FOREIGN KEY (CauHoiID) REFERENCES CauHoi(CauHoiID),
        CONSTRAINT FK_HinhAnh_DapAn   FOREIGN KEY (DapAnID)  REFERENCES DapAn(DapAnID)
    );
END
GO

IF OBJECT_ID(N'dbo.HocVien_Lop', N'U') IS NULL
BEGIN
    CREATE TABLE HocVien_Lop (
        HocVienID    INT NOT NULL,
        LopHocID     INT NOT NULL,
        NgayThamGia  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        TrangThai    NVARCHAR(20) NOT NULL CHECK (TrangThai IN (N'DangHoc', N'DaKetThuc', N'ThoiHoc')),
        CONSTRAINT PK_HocVien_Lop PRIMARY KEY (HocVienID, LopHocID),
        CONSTRAINT FK_HVL_HocVien FOREIGN KEY (HocVienID) REFERENCES HocVien(HocVienID),
        CONSTRAINT FK_HVL_LopHoc  FOREIGN KEY (LopHocID)  REFERENCES LopHoc(LopHocID)
    );
END
GO

IF OBJECT_ID(N'dbo.LichHoc', N'U') IS NULL
BEGIN
    CREATE TABLE LichHoc (
        LichHocID        INT IDENTITY(1,1) PRIMARY KEY,
        LopHocID         INT           NOT NULL,
        NoiDung          NVARCHAR(200) NULL,
        ThoiGianBatDau   DATETIME2     NOT NULL,
        ThoiGianKetThuc  DATETIME2     NOT NULL,
        DiaDiem          NVARCHAR(200) NULL,
        CONSTRAINT FK_LichHoc_LopHoc FOREIGN KEY (LopHocID) REFERENCES LopHoc(LopHocID)
    );
END
GO

IF OBJECT_ID(N'dbo.PhanCongGiangDay', N'U') IS NULL
BEGIN
    CREATE TABLE PhanCongGiangDay (
        LopHocID   INT NOT NULL,
        MonHocID   INT NOT NULL,
        GiaoVienID INT NOT NULL,
        CONSTRAINT PK_PhanCongGiangDay PRIMARY KEY (LopHocID, MonHocID),
        CONSTRAINT FK_PCGD_LopHoc   FOREIGN KEY (LopHocID)   REFERENCES LopHoc(LopHocID),
        CONSTRAINT FK_PCGD_MonHoc   FOREIGN KEY (MonHocID)   REFERENCES MonHoc(MonHocID),
        CONSTRAINT FK_PCGD_GiaoVien FOREIGN KEY (GiaoVienID) REFERENCES GiaoVien(GiaoVienID)
    );
END
GO

IF OBJECT_ID(N'dbo.DeThi', N'U') IS NULL
BEGIN
    CREATE TABLE DeThi (
        DeThiID         INT IDENTITY(1,1) PRIMARY KEY,
        MonHocID        INT           NOT NULL,
        NguoiTaoID      INT           NOT NULL,
        TenDe           NVARCHAR(200) NOT NULL,
        LoaiDe          NVARCHAR(50)  NOT NULL CHECK (LoaiDe IN (N'OnTap', N'ThiThu', N'ChinhThuc')),
        SoLuongCauHoi   INT           NOT NULL,
        HinhThucTao     NVARCHAR(50)  NOT NULL CHECK (HinhThucTao IN (N'ThuCong', N'TuDong', N'MaTranTHPTQG')),
        LaMienPhi       BIT           NOT NULL CONSTRAINT DF_DeThi_LaMienPhi DEFAULT 0,
        DaDuyet         BIT           NOT NULL CONSTRAINT DF_DeThi_DaDuyet DEFAULT 0,
        NguoiDuyetID    INT           NULL,
        NgayDuyet       DATETIME2     NULL,
        CONSTRAINT FK_DeThi_MonHoc   FOREIGN KEY (MonHocID)   REFERENCES MonHoc(MonHocID),
        CONSTRAINT FK_DeThi_NguoiTao FOREIGN KEY (NguoiTaoID) REFERENCES TaiKhoan(TaiKhoanID),
        CONSTRAINT FK_DeThi_NguoiDuyet FOREIGN KEY (NguoiDuyetID) REFERENCES TaiKhoan(TaiKhoanID)
    );
END
GO

IF COL_LENGTH('DeThi', 'LaMienPhi') IS NULL ALTER TABLE DeThi ADD LaMienPhi BIT NOT NULL CONSTRAINT DF_DeThi_LaMienPhi DEFAULT 0;
IF COL_LENGTH('DeThi', 'DaDuyet') IS NULL ALTER TABLE DeThi ADD DaDuyet BIT NOT NULL CONSTRAINT DF_DeThi_DaDuyet DEFAULT 0;
IF COL_LENGTH('DeThi', 'NguoiDuyetID') IS NULL ALTER TABLE DeThi ADD NguoiDuyetID INT NULL REFERENCES TaiKhoan(TaiKhoanID);
IF COL_LENGTH('DeThi', 'NgayDuyet') IS NULL ALTER TABLE DeThi ADD NgayDuyet DATETIME2 NULL;
GO

-- Cơ sở dữ liệu cũ cần cho phép đề tạo theo ma trận.
DECLARE @oldExamCreationConstraint SYSNAME;
SELECT TOP (1) @oldExamCreationConstraint = name
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID(N'dbo.DeThi')
  AND definition LIKE N'%HinhThucTao%'
  AND definition NOT LIKE N'%MaTranTHPTQG%';
IF @oldExamCreationConstraint IS NOT NULL
BEGIN
    DECLARE @dropExamConstraintSql NVARCHAR(MAX) =
        N'ALTER TABLE dbo.DeThi DROP CONSTRAINT ' + QUOTENAME(@oldExamCreationConstraint);
    EXEC sp_executesql @dropExamConstraintSql;
    ALTER TABLE dbo.DeThi WITH CHECK ADD CONSTRAINT CK_DeThi_HinhThucTao
        CHECK (HinhThucTao IN (N'ThuCong', N'TuDong', N'MaTranTHPTQG'));
END
GO

IF OBJECT_ID(N'dbo.DeThi_CauHoi', N'U') IS NULL
BEGIN
    CREATE TABLE DeThi_CauHoi (
        DeThiID   INT NOT NULL,
        CauHoiID  INT NOT NULL,
        ThuTu     INT NOT NULL,
        CONSTRAINT PK_DeThi_CauHoi PRIMARY KEY (DeThiID, CauHoiID),
        CONSTRAINT FK_DTCH_DeThi  FOREIGN KEY (DeThiID)  REFERENCES DeThi(DeThiID),
        CONSTRAINT FK_DTCH_CauHoi FOREIGN KEY (CauHoiID) REFERENCES CauHoi(CauHoiID)
    );
END
GO

IF OBJECT_ID(N'dbo.MaDeThi', N'U') IS NULL
BEGIN
    CREATE TABLE MaDeThi (
        MaDeID   INT IDENTITY(1,1) PRIMARY KEY,
        DeThiID  INT           NOT NULL,
        TenMaDe  NVARCHAR(50)  NOT NULL,
        CONSTRAINT FK_MaDeThi_DeThi FOREIGN KEY (DeThiID) REFERENCES DeThi(DeThiID)
    );
END
GO

IF OBJECT_ID(N'dbo.MaDe_CauHoi', N'U') IS NULL
BEGIN
    CREATE TABLE MaDe_CauHoi (
        MaDeID   INT NOT NULL,
        CauHoiID INT NOT NULL,
        ThuTu    INT NOT NULL,
        CONSTRAINT PK_MaDe_CauHoi PRIMARY KEY (MaDeID, CauHoiID),
        CONSTRAINT UQ_MaDe_CauHoi_ThuTu UNIQUE (MaDeID, ThuTu),
        CONSTRAINT FK_MDCH_MaDeThi FOREIGN KEY (MaDeID)   REFERENCES MaDeThi(MaDeID),
        CONSTRAINT FK_MDCH_CauHoi  FOREIGN KEY (CauHoiID) REFERENCES CauHoi(CauHoiID)
    );
END
GO

IF OBJECT_ID(N'dbo.MaDe_DapAn', N'U') IS NULL
BEGIN
    CREATE TABLE MaDe_DapAn (
        MaDeID   INT NOT NULL,
        CauHoiID INT NOT NULL,
        DapAnID  INT NOT NULL,
        ThuTu    INT NOT NULL,
        CONSTRAINT PK_MaDe_DapAn PRIMARY KEY (MaDeID, CauHoiID, DapAnID),
        CONSTRAINT UQ_MaDe_DapAn_ThuTu UNIQUE (MaDeID, CauHoiID, ThuTu),
        CONSTRAINT FK_MDDA_MaDeThi  FOREIGN KEY (MaDeID)   REFERENCES MaDeThi(MaDeID),
        CONSTRAINT FK_MDDA_CauHoi   FOREIGN KEY (CauHoiID) REFERENCES CauHoi(CauHoiID),
        CONSTRAINT FK_MDDA_DapAn    FOREIGN KEY (DapAnID)  REFERENCES DapAn(DapAnID)
    );
END
GO

IF OBJECT_ID(N'dbo.DotThi', N'U') IS NULL
BEGIN
    CREATE TABLE DotThi (
        DotThiID           INT IDENTITY(1,1) PRIMARY KEY,
        DeThiID            INT           NOT NULL,
        TenDotThi          NVARCHAR(200) NOT NULL,
        ThoiGianMoCong     DATETIME2     NOT NULL,
        ThoiGianDongCong   DATETIME2     NOT NULL,
        ThoiLuongLamBai    INT           NOT NULL,
        GioiHanSoLuong     INT           NULL,
        CongBoDiemSom      BIT           NOT NULL DEFAULT 0,
        TrangThai          NVARCHAR(20)  NOT NULL CHECK (TrangThai IN (N'SapMo', N'DangMo', N'DaDong')),
        PhamVi             NVARCHAR(20)  NOT NULL CHECK (PhamVi IN (N'ToanTruong', N'TheoLop')),
        SoLanThiToiDa      INT           NOT NULL DEFAULT 1,
        CONSTRAINT FK_DotThi_DeThi FOREIGN KEY (DeThiID) REFERENCES DeThi(DeThiID)
    );
END
GO

IF OBJECT_ID(N'dbo.DotThi_LopHoc', N'U') IS NULL
BEGIN
    CREATE TABLE DotThi_LopHoc (
        DotThiID  INT NOT NULL,
        LopHocID  INT NOT NULL,
        CONSTRAINT PK_DotThi_LopHoc PRIMARY KEY (DotThiID, LopHocID),
        CONSTRAINT FK_DTLH_DotThi FOREIGN KEY (DotThiID) REFERENCES DotThi(DotThiID),
        CONSTRAINT FK_DTLH_LopHoc FOREIGN KEY (LopHocID) REFERENCES LopHoc(LopHocID)
    );
END
GO

IF OBJECT_ID(N'dbo.DangKyDotThi', N'U') IS NULL
BEGIN
    CREATE TABLE DangKyDotThi (
        DangKyID    INT IDENTITY(1,1) PRIMARY KEY,
        HocVienID   INT           NOT NULL,
        DotThiID    INT           NOT NULL,
        NgayDangKy  DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        TrangThai   NVARCHAR(20)  NOT NULL CHECK (TrangThai IN (N'DaDangKy', N'DaXacNhan', N'DaHuy')),
        CONSTRAINT UQ_DangKy_HocVien_DotThi UNIQUE (HocVienID, DotThiID),
        CONSTRAINT FK_DKDT_HocVien FOREIGN KEY (HocVienID) REFERENCES HocVien(HocVienID),
        CONSTRAINT FK_DKDT_DotThi  FOREIGN KEY (DotThiID)  REFERENCES DotThi(DotThiID)
    );
END
GO

IF OBJECT_ID(N'dbo.BaiLam', N'U') IS NULL
BEGIN
    CREATE TABLE BaiLam (
        BaiLamID         INT IDENTITY(1,1) PRIMARY KEY,
        HocVienID        INT           NOT NULL,
        DotThiID         INT           NOT NULL,
        MaDeID           INT           NOT NULL,
        LanThi           INT           NOT NULL,
        ThoiGianBatDau   DATETIME2     NOT NULL,
        ThoiGianNop      DATETIME2     NULL,
        TongDiem         DECIMAL(5,2)  NULL,
        TrangThai        NVARCHAR(20)  NOT NULL CHECK (TrangThai IN (N'DangLam', N'DaNop', N'HetGio')),
        CONSTRAINT UQ_BaiLam_HocVien_DotThi_Lan UNIQUE (HocVienID, DotThiID, LanThi),
        CONSTRAINT FK_BaiLam_HocVien FOREIGN KEY (HocVienID) REFERENCES HocVien(HocVienID),
        CONSTRAINT FK_BaiLam_DotThi  FOREIGN KEY (DotThiID)  REFERENCES DotThi(DotThiID),
        CONSTRAINT FK_BaiLam_MaDeThi FOREIGN KEY (MaDeID)    REFERENCES MaDeThi(MaDeID)
    );
END
GO

IF OBJECT_ID(N'dbo.ExamActivity', N'U') IS NULL
BEGIN
    CREATE TABLE ExamActivity (
        EventID    UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        BaiLamID   INT              NOT NULL,
        EventType  VARCHAR(20)      NOT NULL,
        ReceivedAt DATETIME2        NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT FK_ExamActivity_BaiLam FOREIGN KEY (BaiLamID) REFERENCES BaiLam(BaiLamID)
    );
    CREATE INDEX IX_ExamActivity_Attempt ON ExamActivity(BaiLamID, ReceivedAt);
END
GO

IF OBJECT_ID(N'dbo.ChiTietBaiLam', N'U') IS NULL
BEGIN
    CREATE TABLE ChiTietBaiLam (
        ChiTietID    INT IDENTITY(1,1) PRIMARY KEY,
        BaiLamID     INT           NOT NULL,
        CauHoiID     INT           NOT NULL,
        DiemDatDuoc  DECIMAL(5,2)  NULL,
        CONSTRAINT UQ_ChiTiet_BaiLam_CauHoi UNIQUE (BaiLamID, CauHoiID),
        CONSTRAINT FK_CTBL_BaiLam  FOREIGN KEY (BaiLamID)  REFERENCES BaiLam(BaiLamID),
        CONSTRAINT FK_CTBL_CauHoi  FOREIGN KEY (CauHoiID)  REFERENCES CauHoi(CauHoiID)
    );
END
GO

IF OBJECT_ID(N'dbo.LuaChonBaiLam', N'U') IS NULL
BEGIN
    CREATE TABLE LuaChonBaiLam (
        LuaChonID      INT IDENTITY(1,1) PRIMARY KEY,
        ChiTietID      INT           NOT NULL,
        DapAnID        INT           NULL,
        NoiDungTraLoi  NVARCHAR(MAX) NULL,
        CONSTRAINT CHK_LuaChon_DuLieu CHECK (DapAnID IS NOT NULL OR NoiDungTraLoi IS NOT NULL),
        CONSTRAINT FK_LCBL_ChiTiet FOREIGN KEY (ChiTietID) REFERENCES ChiTietBaiLam(ChiTietID),
        CONSTRAINT FK_LCBL_DapAn   FOREIGN KEY (DapAnID)   REFERENCES DapAn(DapAnID)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_LuaChon_ChiTiet_DapAn' AND object_id = OBJECT_ID(N'dbo.LuaChonBaiLam'))
    CREATE UNIQUE INDEX UQ_LuaChon_ChiTiet_DapAn ON LuaChonBaiLam (ChiTietID, DapAnID) WHERE DapAnID IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_LuaChon_TraLoiNgan' AND object_id = OBJECT_ID(N'dbo.LuaChonBaiLam'))
    CREATE UNIQUE INDEX UQ_LuaChon_TraLoiNgan ON LuaChonBaiLam (ChiTietID) WHERE DapAnID IS NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CauHoi_ChuDe' AND object_id = OBJECT_ID(N'dbo.CauHoi'))
    CREATE INDEX IX_CauHoi_ChuDe ON CauHoi (ChuDeID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CauHoi_NguoiTao' AND object_id = OBJECT_ID(N'dbo.CauHoi'))
    CREATE INDEX IX_CauHoi_NguoiTao ON CauHoi (NguoiTaoID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DeThi_NguoiTao' AND object_id = OBJECT_ID(N'dbo.DeThi'))
    CREATE INDEX IX_DeThi_NguoiTao ON DeThi (NguoiTaoID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DapAn_CauHoi' AND object_id = OBJECT_ID(N'dbo.DapAn'))
    CREATE INDEX IX_DapAn_CauHoi ON DapAn (CauHoiID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HinhAnh_CauHoi' AND object_id = OBJECT_ID(N'dbo.HinhAnh'))
    CREATE INDEX IX_HinhAnh_CauHoi ON HinhAnh (CauHoiID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HinhAnh_DapAn' AND object_id = OBJECT_ID(N'dbo.HinhAnh'))
    CREATE INDEX IX_HinhAnh_DapAn ON HinhAnh (DapAnID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HocVienLop_LopHoc' AND object_id = OBJECT_ID(N'dbo.HocVien_Lop'))
    CREATE INDEX IX_HocVienLop_LopHoc ON HocVien_Lop (LopHocID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LichHoc_LopHoc' AND object_id = OBJECT_ID(N'dbo.LichHoc'))
    CREATE INDEX IX_LichHoc_LopHoc ON LichHoc (LopHocID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PhanCong_MonHoc' AND object_id = OBJECT_ID(N'dbo.PhanCongGiangDay'))
    CREATE INDEX IX_PhanCong_MonHoc ON PhanCongGiangDay (MonHocID, GiaoVienID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DeThiCauHoi_CauHoi' AND object_id = OBJECT_ID(N'dbo.DeThi_CauHoi'))
    CREATE INDEX IX_DeThiCauHoi_CauHoi ON DeThi_CauHoi (CauHoiID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MaDeThi_DeThi' AND object_id = OBJECT_ID(N'dbo.MaDeThi'))
    CREATE INDEX IX_MaDeThi_DeThi ON MaDeThi (DeThiID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MaDeCauHoi_CauHoi' AND object_id = OBJECT_ID(N'dbo.MaDe_CauHoi'))
    CREATE INDEX IX_MaDeCauHoi_CauHoi ON MaDe_CauHoi (CauHoiID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MaDeDapAn_DapAn' AND object_id = OBJECT_ID(N'dbo.MaDe_DapAn'))
    CREATE INDEX IX_MaDeDapAn_DapAn ON MaDe_DapAn (DapAnID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DotThi_DeThi' AND object_id = OBJECT_ID(N'dbo.DotThi'))
    CREATE INDEX IX_DotThi_DeThi ON DotThi (DeThiID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DotThiLopHoc_LopHoc' AND object_id = OBJECT_ID(N'dbo.DotThi_LopHoc'))
    CREATE INDEX IX_DotThiLopHoc_LopHoc ON DotThi_LopHoc (LopHocID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DangKyDotThi_DotThi' AND object_id = OBJECT_ID(N'dbo.DangKyDotThi'))
    CREATE INDEX IX_DangKyDotThi_DotThi ON DangKyDotThi (DotThiID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BaiLam_DotThi' AND object_id = OBJECT_ID(N'dbo.BaiLam'))
    CREATE INDEX IX_BaiLam_DotThi ON BaiLam (DotThiID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BaiLam_MaDe' AND object_id = OBJECT_ID(N'dbo.BaiLam'))
    CREATE INDEX IX_BaiLam_MaDe ON BaiLam (MaDeID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ChiTietBaiLam_CauHoi' AND object_id = OBJECT_ID(N'dbo.ChiTietBaiLam'))
    CREATE INDEX IX_ChiTietBaiLam_CauHoi ON ChiTietBaiLam (CauHoiID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LuaChonBaiLam_ChiTiet' AND object_id = OBJECT_ID(N'dbo.LuaChonBaiLam'))
    CREATE INDEX IX_LuaChonBaiLam_ChiTiet ON LuaChonBaiLam (ChiTietID);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LuaChonBaiLam_DapAn' AND object_id = OBJECT_ID(N'dbo.LuaChonBaiLam'))
    CREATE INDEX IX_LuaChonBaiLam_DapAn ON LuaChonBaiLam (DapAnID);
GO

-- Mọi thay đổi nội dung đề hoặc mã đề đều hủy trạng thái duyệt cũ.
CREATE OR ALTER TRIGGER dbo.TR_Review_DeThi_CauHoi ON dbo.DeThi_CauHoi AFTER INSERT,UPDATE,DELETE AS
BEGIN SET NOCOUNT ON; UPDATE DeThi SET DaDuyet=0,NguoiDuyetID=NULL,NgayDuyet=NULL WHERE DeThiID IN (SELECT DeThiID FROM inserted UNION SELECT DeThiID FROM deleted); END
GO
CREATE OR ALTER TRIGGER dbo.TR_Review_MaDeThi ON dbo.MaDeThi AFTER INSERT,UPDATE,DELETE AS
BEGIN SET NOCOUNT ON; UPDATE DeThi SET DaDuyet=0,NguoiDuyetID=NULL,NgayDuyet=NULL WHERE DeThiID IN (SELECT DeThiID FROM inserted UNION SELECT DeThiID FROM deleted); END
GO
CREATE OR ALTER TRIGGER dbo.TR_Review_MaDe_CauHoi ON dbo.MaDe_CauHoi AFTER INSERT,UPDATE,DELETE AS
BEGIN SET NOCOUNT ON; UPDATE DeThi SET DaDuyet=0,NguoiDuyetID=NULL,NgayDuyet=NULL WHERE DeThiID IN (SELECT DeThiID FROM MaDeThi WHERE MaDeID IN (SELECT MaDeID FROM inserted UNION SELECT MaDeID FROM deleted)); END
GO
CREATE OR ALTER TRIGGER dbo.TR_Review_MaDe_DapAn ON dbo.MaDe_DapAn AFTER INSERT,UPDATE,DELETE AS
BEGIN SET NOCOUNT ON; UPDATE DeThi SET DaDuyet=0,NguoiDuyetID=NULL,NgayDuyet=NULL WHERE DeThiID IN (SELECT DeThiID FROM MaDeThi WHERE MaDeID IN (SELECT MaDeID FROM inserted UNION SELECT MaDeID FROM deleted)); END
GO
CREATE OR ALTER TRIGGER dbo.TR_Review_CauHoi ON dbo.CauHoi AFTER INSERT,UPDATE,DELETE AS
BEGIN SET NOCOUNT ON; UPDATE DeThi SET DaDuyet=0,NguoiDuyetID=NULL,NgayDuyet=NULL WHERE DeThiID IN (SELECT DeThiID FROM DeThi_CauHoi WHERE CauHoiID IN (SELECT CauHoiID FROM inserted UNION SELECT CauHoiID FROM deleted)); END
GO
CREATE OR ALTER TRIGGER dbo.TR_Review_DapAn ON dbo.DapAn AFTER INSERT,UPDATE,DELETE AS
BEGIN SET NOCOUNT ON; UPDATE DeThi SET DaDuyet=0,NguoiDuyetID=NULL,NgayDuyet=NULL WHERE DeThiID IN (SELECT DeThiID FROM DeThi_CauHoi WHERE CauHoiID IN (SELECT CauHoiID FROM inserted UNION SELECT CauHoiID FROM deleted)); END
GO
CREATE OR ALTER TRIGGER dbo.TR_Review_HinhAnh ON dbo.HinhAnh AFTER INSERT,UPDATE,DELETE AS
BEGIN SET NOCOUNT ON; UPDATE DeThi SET DaDuyet=0,NguoiDuyetID=NULL,NgayDuyet=NULL WHERE DeThiID IN (SELECT DeThiID FROM DeThi_CauHoi WHERE CauHoiID IN (SELECT CauHoiID FROM inserted UNION SELECT CauHoiID FROM deleted UNION SELECT CauHoiID FROM DapAn WHERE DapAnID IN (SELECT DapAnID FROM inserted UNION SELECT DapAnID FROM deleted))); END
GO
CREATE OR ALTER TRIGGER dbo.TR_Review_DeThi ON dbo.DeThi AFTER UPDATE AS
BEGIN SET NOCOUNT ON; IF UPDATE(TenDe) OR UPDATE(MonHocID) OR UPDATE(SoLuongCauHoi) OR UPDATE(HinhThucTao) UPDATE DeThi SET DaDuyet=0,NguoiDuyetID=NULL,NgayDuyet=NULL WHERE DeThiID IN (SELECT DeThiID FROM inserted); END
GO
