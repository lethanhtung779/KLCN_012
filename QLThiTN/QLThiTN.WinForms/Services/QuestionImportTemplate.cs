using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;

namespace QLThiTN.WinForms.Services;

public static class QuestionImportTemplate
{
    public static void Create(string path, string topic, string subject)
    {
        var withImages = Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase);
        var rows = new List<string[]>();
        string[] Row(string type, string content)
        {
            var row = Enumerable.Repeat("", QuestionImportService.Headers.Length).ToArray();
            row[0] = topic; row[1] = content; row[2] = type; row[3] = "NhanBiet";
            row[QuestionImportService.Headers.ToList().IndexOf("MonHoc")] = subject;
            rows.Add(row); return row;
        }
        var tn = Row("TracNghiem", @"Giá trị của $\frac{1}{2}+\frac{1}{2}$ là bao nhiêu?");
        tn[4] = "$1$"; tn[5] = "$2$"; tn[6] = "$3$"; tn[7] = "$4$"; tn[8] = "A";
        tn[14] = @"Ta có $\frac{1}{2}+\frac{1}{2}=1$.";
        var ds = Row("DungSai", "Xét các khẳng định sau:");
        ds[QuestionImportService.Headers.ToList().IndexOf("NhomTHPT")] = "Chung";
        ds[4] = "$1+1=2$"; ds[5] = "$2+2=5$"; ds[6] = "$3>2$"; ds[7] = "$4<1$";
        ds[9] = "D"; ds[10] = "S"; ds[11] = "D"; ds[12] = "S";
        var shortAnswer = Row("TraLoiNgan", @"Tính $\sqrt{9}$."); shortAnswer[13] = "3";
        if (withImages)
        {
            tn[15] = "images/minh_hoa.png";
            tn[16] = "images/minh_hoa.png";
        }
        if (Path.GetExtension(path).Equals(".txt", StringComparison.OrdinalIgnoreCase))
        {
            static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
            var lines = new[] { QuestionImportService.Headers }.Concat(rows).Select(r => string.Join('\t', r.Select(Quote)));
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            return;
        }
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("CauHoi");
        for (var c = 0; c < QuestionImportService.Headers.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = QuestionImportService.Headers[c];
            for (var r = 0; r < rows.Count; r++) sheet.Cell(r + 2, c + 1).Value = rows[r][c];
        }
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1B2A4E");
        sheet.Row(1).Style.Font.FontColor = XLColor.White;
        sheet.Columns().Width = 22; sheet.Column(2).Width = 60; sheet.Column(16).Width = 50;
        sheet.RangeUsed()!.Style.Alignment.WrapText = true;
        sheet.SheetView.FreezeRows(1); sheet.RangeUsed()!.SetAutoFilter();
        var guide = workbook.Worksheets.Add("HuongDan");
        var linesGuide = new[]
        {
            "Mẫu minh họa cách nhập. Sửa chủ đề/môn học và thay nội dung trước khi dùng.",
            "ChuDe phải đúng tên có trong hệ thống. MonHoc giúp phân biệt các chủ đề trùng tên.",
            "NhomTHPT dùng cho câu Đúng/Sai môn Tin học: Chung, KhoaHocMayTinh hoặc TinHocUngDung.",
            "Câu trùng nội dung trong cùng môn không thể nhập. Hệ thống vẫn hiển thị dòng trùng và ID câu đã có.",
            "Công thức toán: nhập văn bản LaTeX $...$ hoặc $$...$$. Không dùng công thức Excel (=...) hay Equation.",
            "Ảnh: ZIP gồm đúng một file dữ liệu (.xlsx/.txt/.csv) và thư mục images. Không dán ảnh trực tiếp vào Excel.",
            "AnhCauHoi, AnhA, AnhB, AnhC, AnhD: đường dẫn tương đối so với file dữ liệu. Ví dụ images/cau1.png.",
            "Nhiều ảnh: phân cách bằng dấu chấm phẩy, ví dụ images/1.png;images/2.png.",
            "Trắc nghiệm: đủ 4 đáp án bằng chữ/ảnh; DapAnDung là A/B/C/D. Đúng-sai: đủ 4 ý và Y_a_Dung…Y_d_Dung là D/S.",
            "Trả lời ngắn: nhập DapSo bằng chữ/số; chỉ dùng ảnh ở AnhCauHoi.",
            "Điểm được tự động xác định theo môn và dạng câu hỏi trong cấu trúc đề tốt nghiệp THPT 2025. Tối đa 500 dòng mỗi lần.",
            "TXT mẫu dùng TAB và dấu ngoặc kép. Với nội dung chứa dấu phân cách hoặc xuống dòng, bao trường bằng dấu ngoặc kép; dấu ngoặc kép bên trong viết hai lần.",
            "Ảnh PNG/JPEG/BMP/GIF tối đa 15 MB, 25 triệu điểm ảnh; GIF chỉ giữ khung hiện tại. ZIP tối đa 100 MB.",
            "Dòng lỗi bị bỏ qua; chỉ nhập các dòng hợp lệ được chọn sau khi xem trước. Đóng màn hình không ghi dữ liệu."
        };
        for (var i = 0; i < linesGuide.Length; i++) guide.Cell(i + 1, 1).Value = linesGuide[i];
        guide.Column(1).Width = 120; guide.Column(1).Style.Alignment.WrapText = true; guide.RowsUsed().Height = 42;
        if (!withImages) { workbook.SaveAs(path); return; }
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        using (var data = new MemoryStream())
        {
            workbook.SaveAs(data); data.Position = 0;
            using var destination = zip.CreateEntry("cau_hoi.xlsx").Open(); data.CopyTo(destination);
        }
        using var bitmap = new Bitmap(300, 140);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
            using var pen = new Pen(Color.Navy, 3);
            graphics.DrawPolygon(pen, [new Point(35, 115), new Point(150, 20), new Point(265, 115)]);
        }
        using (var image = zip.CreateEntry("images/minh_hoa.png").Open()) image.Write(QuestionImageStore.ToPng(bitmap));
        using var readme = new StreamWriter(zip.CreateEntry("HUONG_DAN.md").Open(), new UTF8Encoding(false));
        readme.WriteLine(string.Join("\n\n", linesGuide));
    }
}
