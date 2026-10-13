using System.Reflection;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Web.WebView2.WinForms;
using QLThiTN.WinForms.Forms.Teacher;
using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

static class Program
{
    static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(target)!;
    static void Assert(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS " + name); }
    static QuestionImage ImageData(byte[] bytes) => new() { PendingPng = bytes, MoTa = "Hình minh họa" };
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        using var host = new Form { ShowInTaskbar = false, Opacity = 0, Width = 1000, Height = 800 };
        host.Shown += async (_, _) =>
        {
            try { ExamRoleChecks.RunUi(); await CheckUi(host); await ImportChecks.RunUi(host); CheckStorage(); }
            catch(Exception ex) { Console.Error.WriteLine(ex); Environment.ExitCode = 1; }
            finally { host.Close(); }
        };
        Application.Run(host);
    }
    static async Task CheckUi(Form host)
    {
        using var editor = new frmQuestionEdit(0) { ShowInTaskbar = false, Opacity = 0 };
        editor.Show(host); editor.PerformLayout();
        var question = Field<QuestionContentEditor>(editor, "_question");
        using var bitmap = new Bitmap(240,100);
        using(var graphics=Graphics.FromImage(bitmap)) {graphics.Clear(Color.LightBlue);graphics.DrawLine(Pens.Navy,10,80,220,20);}
        question.AddImage(bitmap);
        question.Input.Text = @"Tính $\frac{1}{2}$ và $\sqrt{4}$. <script>window.injected=true</script>";
        Assert(question.Images.Count == 1 && question.Images[0].PendingPng != null, "Image remains staged until Save");
        Assert(Field<PictureBox>(question,"_picture").Image != null,"Attachment thumbnail decodes");
        Assert(Field<PictureBox>(question,"_picture").Height >= 80, "Thumbnail panel is visible");
        using (var screenshot = new Bitmap(editor.Width, editor.Height))
        {
            editor.DrawToBitmap(screenshot, new Rectangle(Point.Empty, editor.Size));
            screenshot.Save(Path.Combine(AppContext.BaseDirectory,"editor.png"));
            Field<TabControl>(editor,"_tabs").SelectedIndex=1;
            editor.PerformLayout(); Application.DoEvents();
            editor.DrawToBitmap(screenshot, new Rectangle(Point.Empty, editor.Size));
            screenshot.Save(Path.Combine(AppContext.BaseDirectory,"answers.png"));
        }
        var read = typeof(frmQuestionEdit).GetMethod("ReadQuestion", BindingFlags.NonPublic|BindingFlags.Instance)!;
        var q=(CauHoi)read.Invoke(editor,null)!;
        q.DapAns[0].NoiDung=@"$\frac{3}{4}$";q.DapAns[0].LaDapAnDung=true;q.DapAns[0].HinhAnhs=[ImageData(QuestionImageStore.ToPng(bitmap))];
        var preview=new QuestionPreview();host.Controls.Add(preview);
        await preview.RenderAsync(q);
        var browser=Field<WebView2>(preview,"_browser");
        Assert(browser.CoreWebView2!=null,"WebView2 initializes");
        var deadline=DateTime.UtcNow.AddSeconds(20); string state="";
        do
        {
            await Task.Delay(150);
            state=await browser.CoreWebView2!.ExecuteScriptAsync("JSON.stringify({math:document.querySelectorAll('.katex').length,images:[...document.images].filter(i=>i.complete&&i.naturalWidth>0).length,injected:window.injected===true})");
            if(state.Contains("math\\\":3")&&state.Contains("images\\\":2")) break;
        }while(DateTime.UtcNow<deadline);
        Console.WriteLine("Preview state: "+state);
        Assert(state.Contains("math\\\":3")&&state.Contains("images\\\":2"),"Offline preview renders formulas and question/answer images");
        Assert(state.Contains("injected\\\":false"),"Question text is HTML escaped");
        var path=Path.Combine(AppContext.BaseDirectory,"preview.png");
        using(var stream=File.Create(path)) await browser.CoreWebView2!.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png,stream);
        Console.WriteLine("Preview screenshot: "+path);
        preview.Dispose(); editor.Close();
    }
    static void CheckStorage()
    {
        var original=Database.ConnectionString;
        var name="QLThiTN_MediaSmoke_"+Guid.NewGuid().ToString("N");
        var builder=new SqlConnectionStringBuilder(original){InitialCatalog="master"};
        using var master=new SqlConnection(builder.ConnectionString);master.Open();
        using(var create=new SqlCommand($"CREATE DATABASE [{name}]",master))create.ExecuteNonQuery();
        var configField=typeof(Database).GetField("_connectionString",BindingFlags.Static|BindingFlags.NonPublic)!;
        var configPath=Path.Combine(AppContext.BaseDirectory,"appsettings.json");var originalConfig=File.ReadAllText(configPath);
        var root=Path.Combine(Path.GetTempPath(),name);Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(configPath,JsonSerializer.Serialize(new {HinhAnhRoot=root}));
            builder.InitialCatalog=name;configField.SetValue(null,builder.ConnectionString);
            Database.Execute("""
            CREATE TABLE CauHoi(CauHoiID int IDENTITY PRIMARY KEY,ChuDeID int,GiaoVienID int,NoiDung nvarchar(max),LoaiCauHoi nvarchar(30),MucDoKho nvarchar(30),Diem decimal(5,2),TrangThai nvarchar(20),NgayTao datetime2,GiaiThichDapAn nvarchar(max));
            CREATE TABLE DapAn(DapAnID int IDENTITY PRIMARY KEY,CauHoiID int REFERENCES CauHoi,ThuTu int,NoiDung nvarchar(max),LaDapAnDung bit);
            CREATE TABLE HinhAnh(HinhAnhID int IDENTITY PRIMARY KEY,CauHoiID int REFERENCES CauHoi,DapAnID int REFERENCES DapAn,DuongDan nvarchar(500),MoTa nvarchar(200),ThuTu int);
            CREATE TABLE ChiTietBaiLam(CauHoiID int REFERENCES CauHoi);
            CREATE TABLE DeThi_CauHoi(CauHoiID int REFERENCES CauHoi);
            CREATE TABLE MaDe_CauHoi(CauHoiID int REFERENCES CauHoi);
            CREATE TABLE MaDe_DapAn(CauHoiID int REFERENCES CauHoi,DapAnID int REFERENCES DapAn);
            """);
            using var bitmap=new Bitmap(16,16);var bytes=QuestionImageStore.ToPng(bitmap);var svc=new QuestionService();
            foreach(var type in new[]{LoaiCauHoi.TracNghiem,LoaiCauHoi.DungSai,LoaiCauHoi.TraLoiNgan})
            {
                var q=new CauHoi{ChuDeID=1,LoaiCauHoi=type,HinhAnhs=[ImageData(bytes)]};
                for(int i=0;i<(type==LoaiCauHoi.TraLoiNgan?1:4);i++)q.DapAns.Add(new DapAn{NoiDung=type==LoaiCauHoi.TraLoiNgan?"2":"",LaDapAnDung=i==0,HinhAnhs=type==LoaiCauHoi.TraLoiNgan?[]:[ImageData(bytes)]});
                int id=svc.Them(q);var loaded=svc.LayChiTiet(id)!;
                Assert(loaded.HinhAnhs!.Count==1 && loaded.DapAns[0].HinhAnhs!.Count==(type==LoaiCauHoi.TraLoiNgan?0:1),"Reload correct image owners "+type);
                Assert(File.Exists(QuestionImageStore.Resolve(loaded.HinhAnhs[0].DuongDan)),"Saved image available to website "+type);
                var imageId=loaded.HinhAnhs[0].HinhAnhID;loaded.HinhAnhs[0].MoTa="Đã sửa";svc.Sua(loaded);
                Assert(svc.LayChiTiet(id)!.HinhAnhs![0].HinhAnhID==imageId,"Edit retains image ID "+type);
                var before=Directory.GetFiles(root,"*",SearchOption.AllDirectories).Length;
                loaded.HinhAnhs.Add(ImageData(bytes));loaded.DapAns[0].HinhAnhs=[new QuestionImage{PendingPng=bytes,MoTa=new string('a',201)}];
                bool failed=false;try{svc.Sua(loaded);}catch(InvalidOperationException){failed=true;}
                Assert(failed && svc.LayChiTiet(id)!.HinhAnhs!.Count==1 && Directory.GetFiles(root,"*",SearchOption.AllDirectories).Length==before,"Failed save rolls back DB and new files "+type);
                loaded=svc.LayChiTiet(id)!;loaded.NoiDung=@"Đề $x^2$";loaded.HinhAnhs!.Clear();
                foreach(var answer in loaded.DapAns){answer.NoiDung="2";answer.HinhAnhs!.Clear();}
                svc.Sua(loaded);
                Assert(svc.LayChiTiet(id)!.HinhAnhs!.Count==0 && svc.LayChiTiet(id)!.DapAns.All(a=>a.HinhAnhs!.Count==0),"Remove image links "+type);
                Database.Execute("INSERT INTO ChiTietBaiLam VALUES("+id+")");
                bool blocked=false;try{svc.Sua(loaded);}catch(InvalidOperationException){blocked=true;}
                Assert(blocked&&!svc.Xoa(id).Ok,"Protect used questions "+type);
                Database.Execute("DELETE FROM ChiTietBaiLam");Assert(svc.Xoa(id).Ok,"Delete unused question "+type);
            }
            ImportChecks.RunStorage();
            ExamRoleChecks.RunStorage();
            bool escaped=false;try{QuestionImageStore.Resolve("../outside.png");}catch(InvalidOperationException){escaped=true;}
            Assert(escaped,"Reject paths outside image root");
        }
        finally
        {
            configField.SetValue(null,original);File.WriteAllText(configPath,originalConfig);SqlConnection.ClearAllPools();
            using var drop=new SqlCommand($"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}];",master);drop.ExecuteNonQuery();
            foreach(var file in Directory.GetFiles(root,"*",SearchOption.AllDirectories))File.Delete(file);
            foreach(var dir in Directory.GetDirectories(root))Directory.Delete(dir);
            Directory.Delete(root);
        }
    }
}
