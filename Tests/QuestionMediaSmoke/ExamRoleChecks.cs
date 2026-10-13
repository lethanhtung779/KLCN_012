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
            Session.Set(new TaiKhoan{VaiTro="QuanTriVien",ProfileID=999,HoTen="Admin"});
            using(var form=new frmMain())Assert(All(form).OfType<Button>().Any(b=>b.Text.Contains("Đề thi & Kỳ thi")),"Admin has exam/session menu");
            Assert(Session.GiaoVienID==0,"Admin profile is not a teacher ID");
            using(var form=new frmExams())Assert(form.Text.Contains("Đề thi"),"Admin can open exams form");
            Session.Set(new TaiKhoan{VaiTro="GiaoVien",ProfileID=7,HoTen="Giáo viên"});
            using(var form=new frmMain())
            {
                Assert(!All(form).OfType<Button>().Any(b=>b.Text.Contains("Đề thi & Kỳ thi")),"Teacher no longer has exam/session menu");
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
            CREATE TABLE TaiKhoan(TaiKhoanID int PRIMARY KEY,TrangThai nvarchar(20),VaiTro nvarchar(20));
            CREATE TABLE GiaoVien(GiaoVienID int PRIMARY KEY,TaiKhoanID int REFERENCES TaiKhoan);
            CREATE TABLE ChuDe(ChuDeID int PRIMARY KEY,MonHocID int,TenChuDe nvarchar(100));
            CREATE TABLE DeThi(DeThiID int IDENTITY PRIMARY KEY,MonHocID int,GiaoVienID int REFERENCES GiaoVien,TenDe nvarchar(200),LoaiDe nvarchar(30),SoLuongCauHoi int,HinhThucTao nvarchar(30));
            CREATE TABLE DeThi_CauHoi(DeThiID int REFERENCES DeThi,CauHoiID int REFERENCES CauHoi,ThuTu int);
            CREATE TABLE MaDeThi(MaDeThiID int IDENTITY PRIMARY KEY,DeThiID int REFERENCES DeThi,TenMaDe nvarchar(20));
            CREATE TABLE DotThi(DotThiID int IDENTITY PRIMARY KEY,DeThiID int REFERENCES DeThi,TenDotThi nvarchar(200),ThoiGianMoCong datetime2,ThoiGianDongCong datetime2,ThoiLuongLamBai int,GioiHanSoLuong int,CongBoDiemSom bit,TrangThai nvarchar(20),PhamVi nvarchar(20),SoLanThiToiDa int);
            INSERT INTO TaiKhoan VALUES(77,N'HoatDong',N'GiaoVien');
            INSERT INTO GiaoVien VALUES(7,77);
            INSERT INTO ChuDe VALUES(1,1,N'Chủ đề');
            INSERT INTO CauHoi(ChuDeID,GiaoVienID,NoiDung,LoaiCauHoi,MucDoKho,Diem,TrangThai,NgayTao) VALUES(1,7,N'Test',N'TracNghiem',N'NhanBiet',0.25,N'HoatDong',SYSDATETIME());
            """.Replace("CREATE TABLE DeThi_CauHoi(DeThiID int REFERENCES DeThi,CauHoiID int REFERENCES CauHoi,ThuTu int);","ALTER TABLE DeThi_CauHoi ADD DeThiID int,ThuTu int;"));
            Session.Set(new TaiKhoan{VaiTro="QuanTriVien",ProfileID=999});
            var id=Database.ExecuteScalarInt("SELECT MAX(CauHoiID) FROM CauHoi");
            var svc=new ExamService(7);
            var manual=svc.TaoDeThuCong("Thủ công",1,"ThiThu",[id]);
            var auto=svc.TaoDeTuDong("Tự động",1,1,null,"ThiThu");
            var matrix=svc.TaoDeTheoMaTranTHPTQG("Ma trận",1,"ThiThu",[new MaTranChuDeItem{ChuDeID=1,TenChuDe="Chủ đề",SoCauPhan1=1}],false);
            Assert(manual.DeThiID>0&&auto.DeThiID>0&&matrix.DeThiID>0,"Admin creates all three exam types");
            Assert(Database.ExecuteScalarInt("SELECT COUNT(*) FROM DeThi WHERE GiaoVienID=7")==3,"All exam types save explicitly selected teacher, not admin profile");
            var session=svc.TaoDotThi(new DotThi{DeThiID=manual.DeThiID,TenDotThi="Đợt thử",ThoiGianMoCong=DateTime.Now.AddDays(1),ThoiGianDongCong=DateTime.Now.AddDays(2),ThoiLuongLamBai=30,PhamVi="ToanTruong"});
            svc.CapNhatTrangThaiDot(session,"DaDong");
            Assert(Database.ExecuteScalarInt("SELECT COUNT(*) FROM DotThi WHERE TrangThai=N'DaDong'")==1,"Admin creates and closes exam session");
            var rejected=false;try{new ExamService().TaoDeThuCong("Thiếu giáo viên",1,"ThiThu",[id]);}catch(InvalidOperationException){rejected=true;}
            Assert(rejected&&Database.ExecuteScalarInt("SELECT COUNT(*) FROM DeThi")==3,"No fallback teacher when none selected");
        }
        finally{Session.Clear();}
    }
}
