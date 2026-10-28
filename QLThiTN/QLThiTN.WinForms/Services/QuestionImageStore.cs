using System.Drawing.Imaging;
using System.Text.Json;
using QLThiTN.WinForms.Models;

namespace QLThiTN.WinForms.Services;

public static class QuestionImageStore
{
    public static string Root
    {
        get
        {
            var config = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(config));
            var root = doc.RootElement.TryGetProperty("HinhAnhRoot", out var value) ? value.GetString() : null;
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Chưa cấu hình thư mục HinhAnhRoot dùng chung với website trong appsettings.json.");
            return Path.GetFullPath(root);
        }
    }

    public static string Resolve(string relative)
    {
        var root = Root;
        var full = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Đường dẫn ảnh không hợp lệ.");
        return full;
    }

    public static byte[] ToPng(Image image)
    {
        if ((long)image.Width * image.Height > 25_000_000)
            throw new InvalidOperationException("Ảnh quá lớn. Hãy dùng ảnh dưới 25 triệu điểm ảnh.");
        using var data = new MemoryStream();
        using var bitmap = new Bitmap(image);
        bitmap.Save(data, ImageFormat.Png);
        if (data.Length > 15 * 1024 * 1024)
            throw new InvalidOperationException("Ảnh quá lớn. Hãy giảm kích thước xuống dưới 15 MB.");
        return data.ToArray();
    }

    private static byte[]? _placeholderPng;

    public static byte[] GetPlaceholderPng()
    {
        if (_placeholderPng != null) return _placeholderPng;
        using var bmp = new Bitmap(240, 80);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(243, 244, 246));
        using var pen = new Pen(Color.FromArgb(209, 213, 219), 2);
        g.DrawRectangle(pen, 1, 1, 237, 77);
        using var font = new Font("Segoe UI", 9, FontStyle.Regular);
        using var brush = new SolidBrush(Color.FromArgb(107, 114, 128));
        var text = "[Ảnh minh họa]";
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, brush, (240 - size.Width) / 2, (80 - size.Height) / 2);
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return _placeholderPng = ms.ToArray();
    }

    public static byte[] Read(QuestionImage image)
    {
        if (image.PendingPng != null) return image.PendingPng;
        try
        {
            var path = Resolve(image.DuongDan);
            if (File.Exists(path))
                return File.ReadAllBytes(path);
        }
        catch
        {
            // Bỏ qua lỗi giải quyết đường dẫn để dùng ảnh giữ chỗ
        }
        return GetPlaceholderPng();
    }

    // Chỉ xóa những file mới do chính lần lưu thất bại này tạo ra.
    public sealed class Batch : IDisposable
    {
        private readonly List<string> _created = [];
        public bool Committed { get; set; }

        public string Save(QuestionImage image)
        {
            if (image.PendingPng == null) return image.DuongDan;
            var relative = $"winforms/{Guid.NewGuid():N}.png";
            var full = Resolve(relative);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            using var stream = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            _created.Add(full);
            stream.Write(image.PendingPng);
            return relative;
        }

        public void Dispose()
        {
            if (Committed) return;
            foreach (var file in _created)
            {
                try { File.Delete(file); }
                catch (IOException) { /* File còn lại không được liên kết trong DB. */ }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
