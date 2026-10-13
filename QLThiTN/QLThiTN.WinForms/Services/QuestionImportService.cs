using System.Globalization;
using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using Microsoft.VisualBasic.FileIO;
using QLThiTN.WinForms.Models;

namespace QLThiTN.WinForms.Services;

public sealed class QuestionImportRow
{
    public int Line { get; init; }
    public CauHoi Question { get; } = new();
    public List<string> Errors { get; } = [];
    public bool Selected { get; set; }
    public int? ImportedId { get; set; }
    public string Topic { get; set; } = "";
    public string Status => ImportedId.HasValue ? $"Đã nhập #{ImportedId}" : Errors.Count > 0 ? string.Join("; ", Errors) : "Hợp lệ";
    public bool CanImport => Errors.Count == 0 && !ImportedId.HasValue;
}

/// <summary>Đọc và kiểm tra trước khi ghi DB; ảnh ZIP luôn được đọc trong bộ nhớ.</summary>
public sealed class QuestionImportService
{
    public static readonly string[] Headers = ["ChuDe", "NoiDung", "LoaiCauHoi", "MucDoKho", "Diem",
        "DapAnA", "DapAnB", "DapAnC", "DapAnD", "DapAnDung", "Y_a_Dung", "Y_b_Dung", "Y_c_Dung", "Y_d_Dung",
        "DapSo", "GiaiThich", "AnhCauHoi", "AnhA", "AnhB", "AnhC", "AnhD", "MonHoc"];
    private const int MaxRows = 500;
    private const long MaxDataBytes = 20 * 1024 * 1024;
    private readonly IReadOnlyList<ChuDe> _topics;
    private readonly IReadOnlyList<MonHoc> _subjects;

    public QuestionImportService(IReadOnlyList<ChuDe> topics, IReadOnlyList<MonHoc> subjects)
    { _topics = topics; _subjects = subjects; }

    public List<QuestionImportRow> Read(string file)
    {
        var limit = Path.GetExtension(file).Equals(".zip", StringComparison.OrdinalIgnoreCase) ? 100 * 1024 * 1024 : MaxDataBytes;
        if (new FileInfo(file).Length > limit) throw new InvalidDataException("File quá lớn (ZIP tối đa 100 MB; Excel/TXT tối đa 20 MB).");
        if (!Path.GetExtension(file).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            using var stream = File.OpenRead(file);
            return ReadData(stream, Path.GetExtension(file), _ => throw new InvalidDataException("Ảnh cần được đóng gói trong ZIP cùng file dữ liệu."));
        }
        using var zip = ZipFile.OpenRead(file);
        if (zip.Entries.Count > 2000 || zip.Entries.Sum(e => e.Length) > 200L * 1024 * 1024)
            throw new InvalidDataException("ZIP vượt giới hạn 2.000 mục hoặc 200 MB sau giải nén.");
        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in zip.Entries.Where(e => e.Name.Length > 0))
        {
            var path = SafeRelativePath(entry.FullName);
            if (!entries.TryAdd(path, entry)) throw new InvalidDataException($"ZIP có tên file trùng nhau: {path}");
        }
        var dataFiles = entries.Where(e => new[] { ".xlsx", ".txt", ".csv" }.Contains(Path.GetExtension(e.Key).ToLowerInvariant())).ToList();
        if (dataFiles.Count != 1) throw new InvalidDataException("ZIP phải có đúng một file Excel/TXT/CSV. Hãy để hướng dẫn trong file .md.");
        var dataFile = dataFiles[0];
        if (dataFile.Value.Length > MaxDataBytes) throw new InvalidDataException("File dữ liệu trong ZIP vượt 20 MB.");
        var folder = dataFile.Key.Contains('/') ? dataFile.Key[..(dataFile.Key.LastIndexOf('/') + 1)] : "";
        var cache = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        long totalImages = 0;
        byte[] LoadImage(string relative)
        {
            var path = folder + SafeRelativePath(relative);
            if (cache.TryGetValue(path, out var saved)) return saved;
            if (!entries.TryGetValue(path, out var entry)) throw new InvalidDataException($"Không tìm thấy ảnh: {relative}");
            if (!new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" }.Contains(Path.GetExtension(path).ToLowerInvariant()))
                throw new InvalidDataException($"Ảnh {relative}: chỉ hỗ trợ PNG/JPEG/BMP/GIF.");
            if (entry.Length > 15 * 1024 * 1024) throw new InvalidDataException($"Ảnh {relative} vượt 15 MB.");
            using var source = entry.Open();
            using var memory = new MemoryStream(); source.CopyTo(memory); memory.Position = 0;
            using var image = Image.FromStream(memory, false, true);
            var bytes = QuestionImageStore.ToPng(image);
            totalImages += bytes.Length;
            if (totalImages > 100L * 1024 * 1024) throw new InvalidDataException("Tổng ảnh đã xử lý vượt 100 MB. Hãy chia nhỏ gói nhập.");
            cache[path] = bytes;
            return bytes;
        }
        using var data = dataFile.Value.Open();
        return ReadData(data, Path.GetExtension(dataFile.Key), LoadImage);
    }

    private static string SafeRelativePath(string value)
    {
        var path = value.Trim().Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(path) || path.StartsWith('/') || path.Contains(':') || path.Contains('\0') ||
            path.Split('/').Any(p => p is "" or "." or ".."))
            throw new InvalidDataException($"Đường dẫn ảnh/file không hợp lệ: {value}");
        return path;
    }

    private List<QuestionImportRow> ReadData(Stream stream, string extension, Func<string, byte[]> loadImage)
    {
        var rows = extension.ToLowerInvariant() switch
        {
            ".xlsx" => ReadExcel(stream),
            ".txt" or ".csv" => ReadText(stream, extension),
            _ => throw new InvalidDataException("Chỉ hỗ trợ .xlsx, .txt, .csv hoặc .zip.")
        };
        return rows.Select(row => Parse(row.Line, row.Values, row.Error, loadImage)).ToList();
    }

    private static string HeaderName(string header) => header.Split('(')[0].Trim();
    private static string[] CheckHeaders(string[] headers)
    {
        var names = headers.Select(HeaderName).ToArray();
        if (!new[] { "ChuDe", "NoiDung", "LoaiCauHoi" }.All(n => names.Contains(n, StringComparer.OrdinalIgnoreCase)))
            throw new InvalidDataException("Thiếu header ChuDe, NoiDung hoặc LoaiCauHoi. Hãy dùng file mẫu.");
        if (names.Where(n => n.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count(n => n.Length > 0))
            throw new InvalidDataException("Tên cột bị trùng trong file dữ liệu.");
        return names;
    }

    private sealed record RawRow(int Line, Dictionary<string, string> Values, string? Error = null);
    private static Dictionary<string, string> Values(string[] headers, string[] fields) => headers
        .Select((name, i) => (name, value: i < fields.Length ? fields[i].Trim() : ""))
        .Where(x => x.name.Length > 0).ToDictionary(x => x.name, x => x.value, StringComparer.OrdinalIgnoreCase);

    private static List<RawRow> ReadExcel(Stream stream)
    {
        // XLSX cũng là ZIP; giới hạn kích thước giải nén trước khi ClosedXML đọc.
        using var memory = new MemoryStream(); stream.CopyTo(memory); memory.Position = 0;
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Read, leaveOpen: true))
            if (archive.Entries.Count > 5000 || archive.Entries.Sum(e => e.Length) > 100L * 1024 * 1024)
                throw new InvalidDataException("Excel quá lớn sau giải nén.");
        memory.Position = 0;
        using var workbook = new XLWorkbook(memory);
        var sheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("CauHoi", StringComparison.OrdinalIgnoreCase)) ?? workbook.Worksheets.First();
        if (sheet.Pictures.Any()) throw new InvalidDataException("Excel chứa ảnh nhúng. Hãy xuất ảnh ra thư mục images trong ZIP và điền cột AnhCauHoi/AnhA–AnhD để tránh gắn nhầm ảnh.");
        var used = sheet.RangeUsed();
        if (used == null) throw new InvalidDataException("Excel không có dữ liệu.");
        var first = used.FirstRow().RowNumber();
        var lastCol = used.LastColumn().ColumnNumber();
        if (lastCol > 100 || used.LastRow().RowNumber() - first > MaxRows)
            throw new InvalidDataException("Mỗi file tối đa 500 dòng câu hỏi và 100 cột.");
        var headers = CheckHeaders(Enumerable.Range(1, lastCol).Select(c => sheet.Cell(first, c).GetString()).ToArray());
        var result = new List<RawRow>();
        for (int r = first + 1; r <= used.LastRow().RowNumber(); r++)
        {
            var cells = Enumerable.Range(1, lastCol).Select(c => sheet.Cell(r, c)).ToArray();
            if (cells.All(c => c.IsEmpty())) continue;
            var formula = cells.Any(c => c.HasFormula);
            var fields = cells.Select(c => c.HasFormula ? c.FormulaA1 : c.GetString()).ToArray();
            result.Add(new RawRow(r, Values(headers, fields), formula ? "Có công thức Excel (=...). Hãy nhập công thức toán bằng văn bản LaTeX $...$." : null));
        }
        return result;
    }

    private static List<RawRow> ReadText(Stream stream, string extension)
    {
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true);
        var text = reader.ReadToEnd();
        var first = text.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#'))?.TrimEnd('\r')
            ?? throw new InvalidDataException("File không có dữ liệu.");
        var separator = first.Contains('\t') ? "\t" : first.Contains('|') ? "|" : first.Contains(';') ? ";" : extension.Equals(".csv", StringComparison.OrdinalIgnoreCase) ? "," : "|";
        using var parser = new TextFieldParser(new StringReader(text)) { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = false, CommentTokens = ["#"] };
        parser.SetDelimiters(separator);
        var headers = parser.ReadFields() ?? [];
        // Header mẫu cũ chứa dấu | trong phần chú thích, chưa đặt trong ngoặc kép.
        if (separator == "|" && first.StartsWith("ChuDe (", StringComparison.OrdinalIgnoreCase) && headers.Length > 22)
            headers = Headers.Take(16).ToArray();
        headers = CheckHeaders(headers);
        var result = new List<RawRow>();
        while (!parser.EndOfData)
        {
            var line = (int)parser.LineNumber;
            try
            {
                var fields = parser.ReadFields() ?? [];
                if (fields.All(string.IsNullOrWhiteSpace)) continue;
                result.Add(new RawRow(line, Values(headers, fields), fields.Length != headers.Length
                    ? $"Có {fields.Length} trường nhưng header có {headers.Length}. Với dấu phân cách trong nội dung, hãy bao trường bằng dấu ngoặc kép." : null));
            }
            catch (MalformedLineException)
            {
                result.Add(new RawRow(line, new(StringComparer.OrdinalIgnoreCase), "Sai dấu ngoặc kép hoặc dấu phân cách."));
            }
            if (result.Count > MaxRows) throw new InvalidDataException("Mỗi file tối đa 500 dòng câu hỏi.");
        }
        return result;
    }

    private QuestionImportRow Parse(int line, Dictionary<string, string> values, string? error, Func<string, byte[]> loadImage)
    {
        string Get(string name) => values.GetValueOrDefault(name, "");
        var row = new QuestionImportRow { Line = line, Topic = Get("ChuDe") };
        if (error != null) row.Errors.Add(error);
        var q = row.Question;
        q.NoiDung = Get("NoiDung"); q.GiaiThichDapAn = Get("GiaiThich");
        q.LoaiCauHoi = Get("LoaiCauHoi") switch
        {
            "TracNghiem" or "Trắc nghiệm" or "TN" => LoaiCauHoi.TracNghiem,
            "DungSai" or "Đúng/Sai" or "Đúng sai" or "DS" => LoaiCauHoi.DungSai,
            "TraLoiNgan" or "Trả lời ngắn" or "TLN" => LoaiCauHoi.TraLoiNgan,
            _ => ""
        };
        q.MucDoKho = Get("MucDoKho") switch
        {
            "NhanBiet" or "Nhận biết" => MucDoKho.NhanBiet,
            "ThongHieu" or "Thông hiểu" => MucDoKho.ThongHieu,
            "VanDung" or "Vận dụng" => MucDoKho.VanDung,
            "VanDungCao" or "Vận dụng cao" => MucDoKho.VanDungCao,
            "" or "ChuaXacDinh" or "Chưa xác định" => MucDoKho.ChuaXacDinh,
            _ => ""
        };
        if (q.LoaiCauHoi.Length == 0) row.Errors.Add("Loại câu hỏi không hợp lệ.");
        if (q.MucDoKho.Length == 0) row.Errors.Add("Mức độ khó không hợp lệ.");
        if (Get("Diem").Length > 0)
        {
            if (!decimal.TryParse(Get("Diem").Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var score) || score <= 0 || score > 100 || decimal.Round(score, 2) != score)
                row.Errors.Add("Điểm phải từ 0,01 đến 100, tối đa hai số thập phân.");
            else q.Diem = score;
        }
        var candidates = _topics.Where(t => string.Equals(t.TenChuDe.Trim(), Get("ChuDe"), StringComparison.OrdinalIgnoreCase));
        if (Get("MonHoc").Length > 0)
        {
            var subjectIds = _subjects.Where(m => string.Equals(m.TenMon.Trim(), Get("MonHoc"), StringComparison.OrdinalIgnoreCase)).Select(m => m.MonHocID).ToHashSet();
            candidates = candidates.Where(t => subjectIds.Contains(t.MonHocID));
        }
        var topics = candidates.ToList();
        if (topics.Count != 1) row.Errors.Add(topics.Count == 0 ? "Không tìm thấy chủ đề/môn học." : "Chủ đề trùng tên: hãy điền thêm cột MonHoc.");
        else q.ChuDeID = topics[0].ChuDeID;
        List<QuestionImage> Images(string column)
        {
            var images = new List<QuestionImage>();
            foreach (var path in Get(column).Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                try { images.Add(new QuestionImage { PendingPng = loadImage(path) }); }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                { row.Errors.Add($"{column}: {ex.Message}"); }
            }
            return images;
        }
        q.HinhAnhs = Images("AnhCauHoi");
        if (q.LoaiCauHoi == LoaiCauHoi.TraLoiNgan)
        {
            q.DapAns.Add(new DapAn { NoiDung = Get("DapSo"), LaDapAnDung = true });
            if (new[] { "AnhA", "AnhB", "AnhC", "AnhD" }.Any(c => Get(c).Length > 0))
                row.Errors.Add("Trả lời ngắn chỉ dùng AnhCauHoi; đáp số cần là chữ/số.");
        }
        else if (q.LoaiCauHoi.Length > 0)
        {
            var correct = Get("DapAnDung").ToUpperInvariant();
            if (q.LoaiCauHoi == LoaiCauHoi.TracNghiem && !new[] { "A", "B", "C", "D" }.Contains(correct)) row.Errors.Add("DapAnDung phải là A, B, C hoặc D.");
            for (var i = 0; i < 4; i++)
            {
                var letter = (char)('A' + i);
                var flag = Get($"Y_{char.ToLowerInvariant(letter)}_Dung").ToUpperInvariant();
                if (q.LoaiCauHoi == LoaiCauHoi.DungSai && !new[] { "D", "S", "Đ", "ĐÚNG", "SAI" }.Contains(flag))
                    row.Errors.Add($"Ý {letter}: cần chọn D hoặc S.");
                q.DapAns.Add(new DapAn { ThuTu = i, NoiDung = Get($"DapAn{letter}"), HinhAnhs = Images($"Anh{letter}"),
                    LaDapAnDung = q.LoaiCauHoi == LoaiCauHoi.TracNghiem ? correct == letter.ToString() : flag is "D" or "Đ" or "ĐÚNG" });
            }
        }
        try { QuestionService.KiemTraHopLe(q); }
        catch (InvalidOperationException ex) { row.Errors.Add(ex.Message); }
        row.Selected = row.CanImport;
        return row;
    }

    public static void ImportSelected(IEnumerable<QuestionImportRow> rows, Func<CauHoi, int> save)
    {
        foreach (var row in rows.Where(r => r.Selected && r.CanImport))
        {
            try { row.ImportedId = save(row.Question); }
            catch (Exception ex) { row.Errors.Add("Lưu thất bại: " + ex.Message); }
            row.Selected = false;
        }
    }
}
