using System.IO.Compression;
using System.Reflection;
using System.Text;
using Microsoft.Web.WebView2.WinForms;
using ClosedXML.Excel;
using QLThiTN.WinForms.Forms.Teacher;
using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

static class ImportChecks
{
    public static string SampleZip => Path.Combine(AppContext.BaseDirectory, "mau_import_anh.zip");
    static readonly QuestionImportService Reader = new(
        [new ChuDe { ChuDeID = 41, MonHocID = 1, TenChuDe = "Hàm số và Đạo hàm" }],
        [new MonHoc { MonHocID = 1, TenMon = "Toán" }]);
    static void Assert(bool ok, string name) { if(!ok) throw new Exception(name); Console.WriteLine("PASS "+name); }
    static string PathFor(string name)=>Path.Combine(AppContext.BaseDirectory,name);
    static List<QuestionImportRow> Read(string name)=>Reader.Read(PathFor(name));
    static void WriteZip(string name, string dataName, string data, bool image = true, string? extraName = null)
    {
        using var output = File.Create(PathFor(name));
        using var zip = new ZipArchive(output, ZipArchiveMode.Create);
        using(var writer=new StreamWriter(zip.CreateEntry(dataName).Open(),new UTF8Encoding(false)))writer.Write(data);
        if(image)
        {
            using var bitmap=new Bitmap(16,16);using var target=zip.CreateEntry("images/a.png").Open();target.Write(QuestionImageStore.ToPng(bitmap));
        }
        if(extraName!=null){using var target=zip.CreateEntry(extraName).Open();target.WriteByte(0);}
    }
    static string Text(string content=@"Tính $\frac{1}{2}$", string image="", string correct="A", string type="TracNghiem")
    {
        string Quote(string value)=>"\""+value.Replace("\"","\"\"")+"\"";
        var row=Enumerable.Repeat("",22).ToArray();row[0]="Hàm số và Đạo hàm";row[1]=content;row[2]=type;row[4]="0,25";
        row[5]="1";row[6]="2";row[7]="3";row[8]="4";row[9]=correct;row[16]=image;row[21]="Toán";
        return string.Join('\t',QuestionImportService.Headers.Select(Quote))+"\n"+string.Join('\t',row.Select(Quote))+"\n";
    }
    public static async Task RunUi(Form host)
    {
        foreach(var ext in new[]{"xlsx","txt","zip"})
        {
            var file=PathFor("mau_import_anh."+ext);if(File.Exists(file))File.Delete(file);
            QuestionImportTemplate.Create(file,"Hàm số và Đạo hàm","Toán");
            var rows=Reader.Read(file);
            Assert(rows.Count==3&&rows.All(r=>r.CanImport),"Template round-trip "+ext);
            Assert(rows[0].Question.NoiDung.Contains(@"\frac{1}{2}"),"LaTeX preserved "+ext);
        }
        var imported=Reader.Read(SampleZip);
        Assert(imported[0].Question.HinhAnhs!.Count==1&&imported[0].Question.DapAns[0].HinhAnhs!.Count==1,"ZIP associates question/answer images");
        var formula= "Giá trị $|x|$ và \\\"trích dẫn\\\"\nDòng thứ hai";
        WriteZip("quoted.zip","cauhoi.txt",Text(formula,"images/a.png;images/a.png"));
        var quoted=Read("quoted.zip");
        Assert(quoted[0].CanImport&&quoted[0].Question.NoiDung==formula&&quoted[0].Question.HinhAnhs!.Count==2,"Quoted text preserves pipes, quotes, newlines and multiple images");
        Assert(quoted[0].Line==2&&quoted[0].Question.Diem==0.25m,"Text line number and decimal comma");
        WriteZip("missing.zip","cauhoi.txt",Text(image:"images/missing.png"));
        var bad=Read("missing.zip");Assert(!bad[0].CanImport&&bad[0].Status.Contains("Không tìm thấy ảnh"),"Missing image reported per row");
        WriteZip("escape.zip","cauhoi.txt",Text(image:"../a.png"));
        Assert(!Read("escape.zip")[0].CanImport,"Image traversal rejected");
        WriteZip("entry-escape.zip","cauhoi.txt",Text(),extraName:"../outside.png");
        bool rejected=false;try{Read("entry-escape.zip");}catch(InvalidDataException){rejected=true;}
        Assert(rejected,"Archive traversal rejected without extraction");
        WriteZip("ambiguous.zip","cauhoi.txt",Text(),extraName:"other.txt");
        rejected=false;try{Read("ambiguous.zip");}catch(InvalidDataException){rejected=true;}
        Assert(rejected,"Multiple data files rejected");
        File.WriteAllText(PathFor("direct.txt"),Text(image:"images/a.png"));
        Assert(!Read("direct.txt")[0].CanImport,"Loose image references require ZIP");
        var csv=Text().Replace("\t", ",");
        File.WriteAllText(PathFor("comma.csv"),csv);
        Assert(Read("comma.csv")[0].CanImport,"Quoted comma CSV imports with decimal comma");
        var pipe=Text().Replace("\t", "|");
        File.WriteAllText(PathFor("pipe.txt"),pipe);
        Assert(Read("pipe.txt")[0].CanImport,"Pipe-delimited TXT supported");
        File.WriteAllText(PathFor("malformed.txt"),"ChuDe|NoiDung|LoaiCauHoi\n\"unfinished");
        Assert(!Read("malformed.txt")[0].CanImport,"Malformed quoted row reported");
        var duplicateReader=new QuestionImportService([new ChuDe{ChuDeID=1,MonHocID=1,TenChuDe="Hàm số và Đạo hàm"},new ChuDe{ChuDeID=2,MonHocID=2,TenChuDe="Hàm số và Đạo hàm"}], [new MonHoc{MonHocID=1,TenMon="Toán"}]);
        File.WriteAllText(PathFor("ambiguous-topic.txt"),Text().Replace("\"Toán\"","\"\""));
        Assert(!duplicateReader.Read(PathFor("ambiguous-topic.txt"))[0].CanImport,"Ambiguous topic requires subject");
        File.WriteAllText(PathFor("bad-answer.txt"),Text(correct:"Z"));
        Assert(!Read("bad-answer.txt")[0].CanImport,"Invalid answer flagged before import");
        File.WriteAllText(PathFor("bad-ds.txt"),Text(type:"DungSai"));
        Assert(!Read("bad-ds.txt")[0].CanImport,"Missing true/false flags are not silently interpreted");
        using(var workbook=new XLWorkbook(PathFor("mau_import_anh.xlsx")))
        {
            workbook.Worksheet("CauHoi").Cell(2,2).FormulaA1="1+1";workbook.SaveAs(PathFor("formula.xlsx"));
        }
        Assert(!Read("formula.xlsx")[0].CanImport&&Read("formula.xlsx")[1].CanImport,"Excel formulas flagged without rejecting other rows");
        using(var workbook=new XLWorkbook(PathFor("mau_import_anh.xlsx")))
        {
            using var bitmap=new Bitmap(10,10);using var stream=new MemoryStream(QuestionImageStore.ToPng(bitmap));
            workbook.Worksheet("CauHoi").AddPicture(stream).MoveTo(workbook.Worksheet("CauHoi").Cell(2,2));workbook.SaveAs(PathFor("embedded.xlsx"));
        }
        rejected=false;try{Read("embedded.xlsx");}catch(InvalidDataException){rejected=true;}
        Assert(rejected,"Embedded Excel images explicitly rejected");
        var calls=0;bad[0].Selected=true;QuestionImportService.ImportSelected(bad,_=>++calls);
        Assert(calls==0,"Invalid row cannot be forced into import");
        imported[1].Selected=false;
        QuestionImportService.ImportSelected(imported,_=>++calls);QuestionImportService.ImportSelected(imported,_=>++calls);
        Assert(calls==2&&imported.Count(r=>r.ImportedId.HasValue)==2,"Unchecked rows skipped and successful rows cannot import twice");
        var display=Reader.Read(SampleZip).Concat(bad).ToList();
        using var form=new frmQuestionImport(display){Opacity=0,ShowInTaskbar=false};form.Show(host);await Task.Delay(500);
        var field=typeof(frmQuestionImport).GetField("_grid",BindingFlags.NonPublic|BindingFlags.Instance)!;
        var grid=(DataGridView)field.GetValue(form)!;
        Assert(grid.Rows.Count==4&&grid.Rows[3].Cells[0].ReadOnly&&!(bool)grid.Rows[3].Cells[0].Value,"Import dialog shows row errors and blocks invalid checkbox");
        var preview=(QuestionPreview)typeof(frmQuestionImport).GetField("_preview",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(form)!;
        var browser=(WebView2)typeof(QuestionPreview).GetField("_browser",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(preview)!;
        var deadline=DateTime.UtcNow.AddSeconds(15);string previewState="";
        do
        {
            await Task.Delay(100);
            if(browser.CoreWebView2!=null)previewState=await browser.CoreWebView2.ExecuteScriptAsync("document.querySelectorAll('.katex').length>0 && [...document.images].filter(i=>i.complete&&i.naturalWidth>0).length===2");
        }while(previewState!="true"&&DateTime.UtcNow<deadline);
        Assert(previewState=="true","Import dialog renders selected ZIP row with formulas and both image owners");
        grid.CurrentCell=grid.Rows[1].Cells[1];grid.CurrentCell=grid.Rows[2].Cells[1];
        deadline=DateTime.UtcNow.AddSeconds(10);
        do
        {
            await Task.Delay(100);previewState=await browser.CoreWebView2!.ExecuteScriptAsync("document.body.innerText.includes('Đáp số') && document.querySelectorAll('.katex').length>0");
        }while(previewState!="true"&&DateTime.UtcNow<deadline);
        Assert(previewState=="true","Rapid row selection previews latest question");
        using var shot=new Bitmap(form.Width,form.Height);form.DrawToBitmap(shot,new Rectangle(Point.Empty,form.Size));shot.Save(PathFor("import-dialog.png"));
        form.Close();Assert(display.All(r=>!r.ImportedId.HasValue),"Closing preview does not import");
    }
    public static void RunStorage()
    {
        var rows=Reader.Read(SampleZip);var svc=new QuestionService();
        QuestionImportService.ImportSelected(rows,svc.Them);
        Assert(rows.All(r=>r.ImportedId.HasValue),"Import ZIP batch into isolated database");
        var question=svc.LayChiTiet(rows[0].ImportedId!.Value)!;
        Assert(question.HinhAnhs!.Count==1&&question.DapAns[0].HinhAnhs!.Count==1&&File.Exists(QuestionImageStore.Resolve(question.HinhAnhs[0].DuongDan)),"Imported image files and ownership persist");
        Assert(question.NoiDung.Contains(@"\frac{1}{2}"),"Imported LaTeX persists unchanged in database");
        foreach(var row in rows)Assert(svc.Xoa(row.ImportedId!.Value).Ok,"Remove isolated import fixture");
    }
}
