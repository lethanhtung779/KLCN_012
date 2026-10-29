using System.Data;
using System.Text.Json;
using QLThiTN.Shared;
using QLThiTN.WinForms.Services;
using QLThiTN.WinForms.Models;

var config=Path.Combine(AppContext.BaseDirectory,"test-config");Directory.CreateDirectory(config);
var database="ThiTracNghiem_WorkflowTest20261028";
File.WriteAllText(Path.Combine(config,"appsettings.json"),JsonSerializer.Serialize(new{ConnectionStrings=new{ThiTracNghiem=$"Server=.\\SQLEXPRESS;Database={database};Trusted_Connection=True;TrustServerCertificate=True"}}));
Database.LoadConfig(config);
Database.Execute(WorkflowSchema.Sql);
Database.Execute(WorkflowSchema.Sql); // migration must be repeatable
var checks=0;
void Check(bool condition,string name){if(!condition)throw new Exception(name);Console.WriteLine("PASS "+name);checks++;}
var admin=Database.ExecuteScalarInt("SELECT TOP 1 TaiKhoanID FROM TaiKhoan WHERE VaiTro='QuanTriVien' AND TrangThai='HoatDong'");
Session.Set(new TaiKhoan{TaiKhoanID=admin,VaiTro="QuanTriVien"});
var profileService=new PersonManagementService();
foreach(var role in new[]{"HocVien","GiaoVien"})
{
    var person=new PersonRecord{VaiTro=role,TenDangNhap="workflow_"+Guid.NewGuid().ToString("N")[..12],Email=Guid.NewGuid()+"@example.test",HoTen="Kiểm tra hồ sơ",SoDienThoai="0912345678",NgaySinh=new DateTime(2000,1,1),MonHocIds=[1]};
    foreach(var field in ProfileFields.ForRole(role))person.HoSo[field.Key]=field.Key.StartsWith("TinhTrang")?"Còn sống":field.Key.StartsWith("Sdt")?"0912345678":"Nội dung "+field.Caption;
    var saved=profileService.Luu(person,"TemporaryPass123!");Check(saved.Ok,role+" create: "+saved.Message);
    var read=profileService.LayChiTiet(role,saved.TaiKhoanID)!;
    Check(read.HoSo.All(x=>x.Value==person.HoSo[x.Key]) && read.NgaySinh==person.NgaySinh,role+" extended fields round trip");
    read.HoSo["SdtCha"]="";Check(!profileService.Luu(read,"").Ok,"Require living parent phone");
    read.HoSo["TinhTrangCha"]="Đã mất";Check(profileService.Luu(read,"").Ok,"Deceased parent does not require phone");
    read.HoSo["TinhTrangCha"]="Còn sống";read.HoSo["SdtCha"]="abc";Check(!profileService.Luu(read,"").Ok,"Reject invalid living parent phone");
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
Console.WriteLine($"{checks} checks passed on isolated database {database}.");
