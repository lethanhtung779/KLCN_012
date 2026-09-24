-- Tao co so du lieu trung tam
-- An toan khi chay lai: chi tao nhung thu chua ton tai

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
        CONSTRAINT FK_HocVien_TaiKhoan FOREIGN KEY (TaiKhoanID) REFERENCES TaiKhoan(TaiKhoanID)
    );
END
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
        GiaoVienID      INT           NOT NULL,
        NoiDung         NVARCHAR(MAX) NOT NULL,
        LoaiCauHoi      NVARCHAR(50)  NOT NULL,
        MucDoKho       NVARCHAR(20)  NOT NULL,
        Diem            DECIMAL(5,2)  NOT NULL DEFAULT 0.25,
        TrangThai       NVARCHAR(20)  NOT NULL,
        NgayTao         DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        GiaiThichDapAn  NVARCHAR(MAX) NULL,
        CONSTRAINT CK_CauHoi_MucDoKho   CHECK (MucDoKho IN (N'NhanBiet', N'ThongHieu', N'VanDung', N'VanDungCao')),
        CONSTRAINT CK_CauHoi_TrangThai  CHECK (TrangThai IN (N'HoatDong', N'An')),
        CONSTRAINT CK_CauHoi_LoaiCauHoi CHECK (LoaiCauHoi IN (N'TracNghiem', N'DungSai', N'TraLoiNgan')),
        CONSTRAINT FK_CauHoi_ChuDe    FOREIGN KEY (ChuDeID)    REFERENCES ChuDe(ChuDeID),
        CONSTRAINT FK_CauHoi_GiaoVien FOREIGN KEY (GiaoVienID) REFERENCES GiaoVien(GiaoVienID)
    );
END
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
        GiaoVienID      INT           NOT NULL,
        TenDe           NVARCHAR(200) NOT NULL,
        LoaiDe          NVARCHAR(50)  NOT NULL CHECK (LoaiDe IN (N'OnTap', N'ThiThu', N'ChinhThuc')),
        SoLuongCauHoi   INT           NOT NULL,
        HinhThucTao     NVARCHAR(50)  NOT NULL CHECK (HinhThucTao IN (N'ThuCong', N'TuDong')),
        CONSTRAINT FK_DeThi_MonHoc   FOREIGN KEY (MonHocID)   REFERENCES MonHoc(MonHocID),
        CONSTRAINT FK_DeThi_GiaoVien FOREIGN KEY (GiaoVienID) REFERENCES GiaoVien(GiaoVienID)
    );
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
        CONSTRAINT UQ_MaDe_CauHoi_ThuTu UNIQUE (MaDeID, CauHoiID, ThuTu),
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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CauHoi_GiaoVien' AND object_id = OBJECT_ID(N'dbo.CauHoi'))
    CREATE INDEX IX_CauHoi_GiaoVien ON CauHoi (GiaoVienID);
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