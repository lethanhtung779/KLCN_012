using System.Data;
using System.Text.Json;
using QLThiTN.Shared;
using QLThiTN.WinForms.Services;
using QLThiTN.WinForms.Models;

var config=Path.Combine(AppContext.BaseDirectory,"test-config");Directory.CreateDirectory(config);
var database="ThiTracNghiem_WorkflowTest20261028";
File.WriteAllText(Path.Combine(config,"appsettings.json"),JsonSerializer.Serialize(new{ConnectionStrings=new{ThiTracNghiem=$"Server=.\\SQLEXPRESS;Database={database};Trusted_Connection=True;TrustServerCertificate=True"}}));
Database.LoadConfig(config);
Database.EnsureGoiColumns();
Database.Execute(WorkflowSchema.Sql);
Database.Execute(WorkflowSchema.Sql); // migration must be repeatable
var checks=0;
void Check(bool condition,string name){if(!condition)throw new Exception(name);Console.WriteLine("PASS "+name);checks++;}
var admin=Database.ExecuteScalarInt("SELECT TOP 1 TaiKhoanID FROM TaiKhoan WHERE VaiTro='QuanTriVien' AND TrangThai='HoatDong'");
Session.Set(new TaiKhoan{TaiKhoanID=admin,VaiTro="QuanTriVien",TrangThai="HoatDong"});
var profileService=new PersonManagementService();
var studentPersonal=profileService.LayThongTinCaNhan("HocVien","");
Check(studentPersonal.Columns.Contains("DiaChi")&&studentPersonal.Columns.Contains("HoTenNguoiGiamHo"),"Student personal-info columns");
Check(!studentPersonal.Columns.Contains("TenDangNhap")&&!studentPersonal.Columns.Contains("Email")&&!studentPersonal.Columns.Contains("LoaiGoi"),"Student personal list excludes account and VIP columns");
var teacherPersonal=profileService.LayThongTinCaNhan("GiaoVien","");
Check(teacherPersonal.Columns.Contains("BangCap")&&teacherPersonal.Columns.Contains("DonViCongTac"),"Teacher personal-info columns");
Check(!teacherPersonal.Columns.Contains("TenDangNhap")&&!teacherPersonal.Columns.Contains("Email")&&!teacherPersonal.Columns.Contains("TrangThai"),"Teacher personal list excludes account columns");
foreach(var role in new[]{"HocVien","GiaoVien"})
{
    var person=new PersonRecord{VaiTro=role,TenDangNhap="workflow_"+Guid.NewGuid().ToString("N")[..12],Email=Guid.NewGuid()+"@example.test",HoTen="Kiểm tra hồ sơ",SoDienThoai="0912345678",NgaySinh=new DateTime(2000,1,1),MonHocIds=[1]};
    foreach(var field in ProfileFields.ForRole(role))person.HoSo[field.Key]=field.Key.Contains("NguoiGiamHo")?"":field.Key.StartsWith("TinhTrang")?"":field.Key.StartsWith("Sdt")?"0912345678":"Nội dung "+field.Caption;
    var saved=profileService.Luu(person,"TemporaryPass123!");Check(saved.Ok,role+" create: "+saved.Message);
    var read=profileService.LayChiTiet(role,saved.TaiKhoanID)!;
    if(role=="HocVien")Check(read.NguonTaiKhoan=="TrungTam","Admin-created student is a center account");
    Check(read.NgaySinh==person.NgaySinh,role+" extended fields round trip");
    if(role=="HocVien")
    {
        Check(read.HoSo["TinhTrangCha"]=="Không rõ thông tin"&&read.HoSo["TinhTrangMe"]=="Không rõ thông tin","Parent information is optional");
        read.HoSo["TinhTrangCha"]="Còn sống";read.HoSo["HoTenCha"]="Nguyễn Văn A";read.HoSo["SdtCha"]="";Check(!profileService.Luu(read,"").Ok,"Require living parent phone");
        read.HoSo["TinhTrangCha"]="Đã mất";Check(profileService.Luu(read,"").Ok,"Deceased parent does not require phone");
        read.HoSo["TinhTrangCha"]="Còn sống";read.HoSo["SdtCha"]="abc";Check(!profileService.Luu(read,"").Ok,"Reject invalid living parent phone");
        read.HoSo["TinhTrangCha"]="Không rõ thông tin";read.HoSo["HoTenCha"]="";read.HoSo["SdtCha"]="";
        read.NgaySinh=DateTime.Today.AddYears(-16);read.HoSo["QuanHeLienHe"]="Anh/Chị/Em";
        Check(!profileService.Luu(read,"").Ok,"Minor requires parent or guardian");
        read.HoSo["HoTenNguoiGiamHo"]="Nguyễn Văn Anh";read.HoSo["QuanHeNguoiGiamHo"]="Anh/Chị/Em";read.HoSo["SdtNguoiGiamHo"]="0912345678";
        Check(!profileService.Luu(read,"").Ok,"Guardian cannot replace known parent information");
        read.HoSo["TinhTrangCha"]="Đã mất";read.HoSo["TinhTrangMe"]="Đã mất";
        read.HoSo["HoTenNguoiGiamHo"]="Nguyễn Văn Anh";read.HoSo["QuanHeNguoiGiamHo"]="Anh/Chị/Em";read.HoSo["SdtNguoiGiamHo"]="0912345678";
        Check(profileService.Luu(read,"").Ok,"Guardian satisfies requirement when both parents deceased");
    }
    else Check(!read.HoSo.ContainsKey("TinhTrangCha")&&!read.HoSo.ContainsKey("TinhTrangMe"),"Teacher form excludes parent information");
}
Check(QuestionDuplicates.Normalize(" <b>HÀM</b>\n số &amp; đồ thị ")==QuestionDuplicates.Normalize("hàm số & đồ thị"),"Normalized exact duplicate");
Check(QuestionDuplicates.Normalize("x² + 1")!=QuestionDuplicates.Normalize("x² - 1"),"Do not collapse math differences");
var questions=new QuestionService();
var existing=Database.ExecuteScalarInt("SELECT TOP 1 CauHoiID FROM CauHoi WHERE LEN(NoiDung)>0 ORDER BY CauHoiID");
var duplicate=questions.LayChiTiet(existing)!;duplicate.CauHoiID=0;
try{questions.Them(duplicate);throw new Exception("Duplicate accepted");}catch(InvalidOperationException ex){Check(ex.Message.Contains("trùng"),"Duplicate save rejected");}
Check(!new QuestionImportRow{IsDuplicate=true,Selected=true}.CanImport,"Import cannot override duplicate flag");
var examService=new ExamService();
var math=Database.ExecuteScalarInt("SELECT TOP 1 MonHocID FROM MonHoc WHERE TenMon LIKE N'%Toán%'");
var created=examService.TaoDeTuDong("Workflow test "+Guid.NewGuid(),math,10,"","ThiThu");Check(created.DeThiID>0,"Create exact math format: "+created.Error);
Check(Database.ExecuteScalarInt($"SELECT COUNT(*) FROM DeThi WHERE DeThiID={created.DeThiID} AND NguoiTaoID={admin} AND DaDuyet=0")==1,"Account attribution; default pending");
Check(Database.ExecuteScalarInt($"SELECT COUNT(*) FROM MaDe_CauHoi mc JOIN MaDeThi m ON m.MaDeID=mc.MaDeID WHERE m.DeThiID={created.DeThiID} AND m.TenMaDe='101'")==22,"Default variant has complete question order");
Check(Database.ExecuteScalarInt($@"SELECT COUNT(*) FROM MaDe_DapAn ma JOIN MaDeThi m ON m.MaDeID=ma.MaDeID WHERE m.DeThiID={created.DeThiID} AND m.TenMaDe='101'")==
      Database.ExecuteScalarInt($@"SELECT COUNT(*) FROM DapAn a JOIN DeThi_CauHoi dc ON dc.CauHoiID=a.CauHoiID WHERE dc.DeThiID={created.DeThiID}"),"Default variant has complete answer order");
var mixed=examService.TronDeThi(created.DeThiID,3,501,true);Check(mixed.Success,"Shuffle: "+mixed.Message);
var guardedQuestionId=Database.ExecuteScalarInt($"SELECT TOP 1 CauHoiID FROM DeThi_CauHoi WHERE DeThiID={created.DeThiID}");
Database.Execute($"UPDATE CauHoi SET TrangThai=N'An' WHERE CauHoiID={guardedQuestionId}");
var examsBeforeGuard=Database.ExecuteScalarInt("SELECT COUNT(*) FROM DeThi");
var invalidQuestionBlocked=false;
try{examService.TaoDeThuCong("Invalid inactive question",math,"ThiThu",[guardedQuestionId]);}
catch(InvalidOperationException){invalidQuestionBlocked=true;}
Check(invalidQuestionBlocked&&Database.ExecuteScalarInt("SELECT COUNT(*) FROM DeThi")==examsBeforeGuard,"Inactive question is rejected before exam save");
Database.Execute($"UPDATE CauHoi SET TrangThai=N'HoatDong' WHERE CauHoiID={guardedQuestionId}");
var variantsBeforeFailure=Database.ExecuteScalarInt($"SELECT COUNT(*) FROM MaDeThi WHERE DeThiID={created.DeThiID}");
Database.Execute("CREATE OR ALTER TRIGGER dbo.TR_Audit_FailShuffle ON dbo.MaDeThi AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE TenMaDe=N'601') THROW 51061,N'Injected shuffle failure',1; END");
try
{
    var failedShuffle=examService.TronDeThi(created.DeThiID,2,601,true);
    Check(!failedShuffle.Success&&Database.ExecuteScalarInt($"SELECT COUNT(*) FROM MaDeThi WHERE DeThiID={created.DeThiID}")==variantsBeforeFailure,"Failed shuffle rolls back deleted and inserted variants");
}
finally{Database.Execute("DROP TRIGGER IF EXISTS dbo.TR_Audit_FailShuffle");}
using(var conn=Database.Open())
{
    var report=ExamReview.Read(conn,created.DeThiID);
    Check(report.Variants.Count==3,"All variants visible");
    Check(report.Variants.All(v=>v.Questions.Count==22),"22 math questions each");
    Check(report.Variants.Select(v=>v.Questions.Select(q=>q.Id).Order().ToArray()).All(ids=>ids.SequenceEqual(report.Variants[0].Questions.Select(q=>q.Id).Order())),"Shuffle preserves question identities");
    Check(report.Variants.Select(v=>string.Join(',',v.Questions.Select(q=>q.Id))).Distinct().Count()==report.Variants.Count,"Every variant has a distinct question order");
    ExamReview.Approve(conn,created.DeThiID,admin,report.Fingerprint);
    Check(Database.ExecuteScalarInt($"SELECT CAST(DaDuyet AS int) FROM DeThi WHERE DeThiID={created.DeThiID}")==1,"Approve reviewed content");
    var qid=report.Variants[0].Questions[0].Id;
    Database.Execute($"UPDATE CauHoi SET NoiDung=NoiDung+N' [test]' WHERE CauHoiID={qid}");
    Check(Database.ExecuteScalarInt($"SELECT CAST(DaDuyet AS int) FROM DeThi WHERE DeThiID={created.DeThiID}")==0,"Question edit invalidates approval");
    try{ExamReview.Approve(conn,created.DeThiID,admin,report.Fingerprint);throw new Exception("Stale approval accepted");}
    catch(InvalidOperationException ex){Check(ex.Message.Contains("thay đổi"),"Stale preview cannot approve");}
    var attempt=Database.ExecuteScalarInt("SELECT TOP 1 BaiLamID FROM BaiLam ORDER BY BaiLamID");
    var details=ExamReview.Read(conn,0,attempt);
    Check(details.Student!=null&&details.Variants.Count==1,"Attempt uses actual variant and student");
    var html=ExamReview.Html(details,i=>"/image/"+i.Id);
    Check(html.Contains("Thí sinh")&&html.Contains("Đáp án"),"Answer review rendered");
}
var training=new TrainingService();
var course=training.ThemKhoaHoc("Chapter 5 course "+Guid.NewGuid(),math,DateTime.Today,DateTime.Today.AddMonths(2),"HoatDong");
Check(course.Ok,"Create course for course/schedule checks");
var classroom=training.ThemLopHoc("Chapter 5 class",course.Id);Check(classroom.Ok,"Create class for course/schedule checks");
var otherSubject=Database.ExecuteScalarInt("INSERT INTO MonHoc(TenMon) VALUES(N'Chapter 5 foreign subject'); SELECT CAST(SCOPE_IDENTITY() AS int);");
Check(!training.SuaKhoaHoc(course.Id,"Invalid subject change",otherSubject,DateTime.Today,DateTime.Today.AddMonths(2),"HoatDong").Ok,
    "Course subject cannot change after a class exists");
var lessonDay=DateTime.Today.AddDays(60+course.Id);
var lessonRoom="Room "+Guid.NewGuid().ToString("N")[..8];
var lesson=training.ThemLichHoc(classroom.Id,"Chapter 5 lesson",lessonDay.AddHours(18),lessonDay.AddHours(20),lessonRoom);
Check(lesson.Ok,"Save valid 18:00-20:00 lesson");
var lessonRow=Database.Query($"SELECT ThoiGianBatDau,ThoiGianKetThuc,DiaDiem FROM LichHoc WHERE LichHocID={lesson.Id}").Rows[0];
Check((DateTime)lessonRow["ThoiGianBatDau"]==lessonDay.AddHours(18)&&(DateTime)lessonRow["ThoiGianKetThuc"]==lessonDay.AddHours(20)&&lessonRow["DiaDiem"].ToString()==lessonRoom,
    "Read lesson time and room without drift");
var teacherScope=Database.Query("SELECT TOP 1 gv.GiaoVienID,gv.TaiKhoanID,gm.MonHocID FROM GiaoVien gv JOIN GiaoVien_MonHoc gm ON gm.GiaoVienID=gv.GiaoVienID JOIN TaiKhoan tk ON tk.TaiKhoanID=gv.TaiKhoanID WHERE tk.TrangThai=N'HoatDong'").Rows[0];
var scopeTeacherId=Convert.ToInt32(teacherScope["GiaoVienID"]);var scopeTeacherAccount=Convert.ToInt32(teacherScope["TaiKhoanID"]);var scopeSubject=Convert.ToInt32(teacherScope["MonHocID"]);
var scopeExam=Database.ExecuteScalarInt($"INSERT INTO DeThi(MonHocID,NguoiTaoID,TenDe,LoaiDe,SoLuongCauHoi,HinhThucTao,LaMienPhi,DaDuyet) VALUES({scopeSubject},{admin},N'Scope assigned',N'ThiThu',0,N'ThuCong',1,1); SELECT CAST(SCOPE_IDENTITY() AS int);");
var scopeSession=Database.ExecuteScalarInt($"INSERT INTO DotThi(DeThiID,TenDotThi,ThoiGianMoCong,ThoiGianDongCong,ThoiLuongLamBai,CongBoDiemSom,TrangThai,PhamVi,SoLanThiToiDa) VALUES({scopeExam},N'Scope assigned',DATEADD(hour,-1,GETDATE()),DATEADD(hour,1,GETDATE()),30,1,N'DangMo',N'ToanTruong',1); SELECT CAST(SCOPE_IDENTITY() AS int);");
var foreignExam=Database.ExecuteScalarInt($"INSERT INTO DeThi(MonHocID,NguoiTaoID,TenDe,LoaiDe,SoLuongCauHoi,HinhThucTao,LaMienPhi,DaDuyet) VALUES({otherSubject},{admin},N'Scope foreign',N'ThiThu',0,N'ThuCong',1,1); SELECT CAST(SCOPE_IDENTITY() AS int);");
var foreignSession=Database.ExecuteScalarInt($"INSERT INTO DotThi(DeThiID,TenDotThi,ThoiGianMoCong,ThoiGianDongCong,ThoiLuongLamBai,CongBoDiemSom,TrangThai,PhamVi,SoLanThiToiDa) VALUES({foreignExam},N'Scope foreign',DATEADD(hour,-1,GETDATE()),DATEADD(hour,1,GETDATE()),30,1,N'DangMo',N'ToanTruong',1); SELECT CAST(SCOPE_IDENTITY() AS int);");
Session.Set(new TaiKhoan{TaiKhoanID=scopeTeacherAccount,ProfileID=scopeTeacherId,VaiTro="GiaoVien",TrangThai="HoatDong"});
var teacherStats=new StatisticsService();var teacherSessions=teacherStats.LayDanhSachDotThiChoThongKe();
Check(teacherSessions.AsEnumerable().Any(r=>Convert.ToInt32(r["DotThiID"])==scopeSession)&&!teacherSessions.AsEnumerable().Any(r=>Convert.ToInt32(r["DotThiID"])==foreignSession),
    "Teacher statistics list only assigned-subject sessions");
var foreignDenied=false;try{teacherStats.LayKetQuaDotThi(foreignSession);}catch(UnauthorizedAccessException){foreignDenied=true;}
Check(foreignDenied,"Teacher cannot open statistics outside assigned subject");
Session.Set(new TaiKhoan{TaiKhoanID=admin,VaiTro="QuanTriVien",TrangThai="HoatDong"});
var adminSessions=new StatisticsService().LayDanhSachDotThiChoThongKe();
Check(adminSessions.AsEnumerable().Any(r=>Convert.ToInt32(r["DotThiID"])==scopeSession)&&adminSessions.AsEnumerable().Any(r=>Convert.ToInt32(r["DotThiID"])==foreignSession),
    "Administrator statistics includes both sessions");
Console.WriteLine($"{checks} checks passed on isolated database {database}.");
