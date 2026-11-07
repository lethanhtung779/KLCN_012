using QLThiTN.WinForms.Forms.Common;
using QLThiTN.WinForms.Forms.Teacher;
using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

static class ExamRoleChecks
{
    static void Assert(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
    static IEnumerable<Control> All(Control c)=>c.Controls.Cast<Control>().SelectMany(x=>new[]{x}.Concat(All(x)));
    static void Blocked(Action action,string name){bool denied=false;try{action();}catch(UnauthorizedAccessException){denied=true;}Assert(denied,name);}
    public static void RunUi()
    {
        try
        {
            var adminId = Database.ExecuteScalarInt("SELECT TOP 1 TaiKhoanID FROM TaiKhoan WHERE VaiTro='QuanTriVien' AND TrangThai='HoatDong'");
            Session.Set(new TaiKhoan{TaiKhoanID=adminId > 0 ? adminId : 1, VaiTro="QuanTriVien",TrangThai="HoatDong",ProfileID=999,HoTen="Admin"});
            using(var form=new frmMain())Assert(All(form).OfType<Button>().Any(b=>b.Text.Contains("Đề thi & Lịch thi") || b.Text.Contains("Đề thi & Kỳ thi")),"Admin has exam/session menu");
            Assert(Session.GiaoVienID==0,"Admin profile is not a teacher ID");
            using(var form=new frmExams())Assert(form.Text.Contains("Đề thi"),"Admin can open exams form");
            Session.Set(new TaiKhoan{VaiTro="GiaoVien",TrangThai="HoatDong",ProfileID=7,HoTen="Giáo viên"});
            using(var form=new frmMain())
            {
                Assert(!All(form).OfType<Button>().Any(b=>b.Text.Contains("Đề thi & Lịch thi") || b.Text.Contains("Đề thi & Kỳ thi")),"Teacher no longer has exam/session menu");
                Assert(All(form).OfType<Button>().Any(b=>b.Text.Contains("Ngân hàng câu hỏi")),"Teacher retains question bank");
            }
            Blocked(()=>{using var form=new frmExams();},"Teacher cannot open exam management directly");
            var svc=new ExamService(7);
            Blocked(()=>svc.TaoDeThuCong("test",1,"ThiThu",[1]),"Teacher cannot create exam through service");
            Blocked(()=>svc.TaoDotThi(new DotThi()),"Teacher cannot create exam session");
            Blocked(()=>svc.CapNhatTrangThaiDot(1,"DaDong"),"Teacher cannot close exam session");
            Blocked(()=>svc.XoaDe(1),"Teacher cannot delete exam");
        }
        finally{Session.Clear();}
    }
    public static void RunStorage()
    {
        try
        {
            Database.Execute("""
            IF OBJECT_ID('TaiKhoan') IS NULL CREATE TABLE TaiKhoan(TaiKhoanID int PRIMARY KEY,TrangThai nvarchar(20),VaiTro nvarchar(20));
            IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TaiKhoanID=1) INSERT INTO TaiKhoan VALUES(1,N'HoatDong',N'QuanTriVien');
            IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TaiKhoanID=77) INSERT INTO TaiKhoan VALUES(77,N'HoatDong',N'GiaoVien');
            IF OBJECT_ID('GiaoVien') IS NULL CREATE TABLE GiaoVien(GiaoVienID int PRIMARY KEY,TaiKhoanID int REFERENCES TaiKhoan);
            IF NOT EXISTS (SELECT 1 FROM GiaoVien WHERE GiaoVienID=7) INSERT INTO GiaoVien VALUES(7,77);
            IF OBJECT_ID('ChuDe') IS NULL CREATE TABLE ChuDe(ChuDeID int PRIMARY KEY,MonHocID int,TenChuDe nvarchar(100));
            IF NOT EXISTS (SELECT 1 FROM ChuDe WHERE ChuDeID=1) INSERT INTO ChuDe VALUES(1,1,N'Chủ đề');
            IF OBJECT_ID('DeThi') IS NULL CREATE TABLE DeThi(DeThiID int IDENTITY PRIMARY KEY,MonHocID int,NguoiTaoID int,TenDe nvarchar(200),LoaiDe nvarchar(30),SoLuongCauHoi int,HinhThucTao nvarchar(30),LaMienPhi bit);
            IF COL_LENGTH('DeThi_CauHoi', 'DeThiID') IS NULL ALTER TABLE DeThi_CauHoi ADD DeThiID int,ThuTu int;
            IF OBJECT_ID('MaDeThi') IS NULL CREATE TABLE MaDeThi(MaDeThiID int IDENTITY PRIMARY KEY,DeThiID int REFERENCES DeThi,TenMaDe nvarchar(20));
            IF OBJECT_ID('DotThi') IS NULL CREATE TABLE DotThi(DotThiID int IDENTITY PRIMARY KEY,DeThiID int REFERENCES DeThi,TenDotThi nvarchar(200),ThoiGianMoCong datetime2,ThoiGianDongCong datetime2,ThoiLuongLamBai int,GioiHanSoLuong int,CongBoDiemSom bit,TrangThai nvarchar(20),PhamVi nvarchar(20),SoLanThiToiDa int);
            INSERT INTO CauHoi(ChuDeID,GiaoVienID,NoiDung,LoaiCauHoi,MucDoKho,Diem,TrangThai,NgayTao) VALUES(1,7,N'Test',N'TracNghiem',N'NhanBiet',0.25,N'HoatDong',SYSDATETIME());
            """);
            Session.Set(new TaiKhoan{TaiKhoanID=1,VaiTro="QuanTriVien",TrangThai="HoatDong",ProfileID=999,HoTen="Admin"});
            var id=Database.ExecuteScalarInt("SELECT MAX(CauHoiID) FROM CauHoi");
            var svc=new ExamService();
            var manual=svc.TaoDeThuCong("Thủ công",1,"ThiThu",[id]);
            Assert(manual.DeThiID>0,"Admin creates manual exam");
            Assert(Database.ExecuteScalarInt("SELECT COUNT(*) FROM DeThi WHERE NguoiTaoID=1")==1,"Manual exam saves admin as creator");
            var session=svc.TaoDotThi(new DotThi{DeThiID=manual.DeThiID,TenDotThi="Đợt thử",ThoiGianMoCong=DateTime.Now.AddDays(1),ThoiGianDongCong=DateTime.Now.AddDays(2),ThoiLuongLamBai=30,PhamVi="ToanTruong"});
            svc.CapNhatTrangThaiDot(session,"DaDong");
            Assert(Database.ExecuteScalarInt("SELECT COUNT(*) FROM DotThi WHERE TrangThai=N'DaDong'")==1,"Admin creates and closes exam session");
            Blocked(() => { Session.Set(new TaiKhoan { TaiKhoanID = 77, VaiTro = "GiaoVien", TrangThai = "HoatDong", ProfileID = 7, HoTen = "GV" }); svc.TaoDeThuCong("Chặn GV", 1, "ThiThu", [id]); }, "Teacher cannot create exam");
            Assert(Database.ExecuteScalarInt("SELECT COUNT(*) FROM DeThi")==1,"Teacher creation attempt blocked");
        }
        finally{Session.Clear();}
    }
}
