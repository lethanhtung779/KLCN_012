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

    public static byte[] Read(QuestionImage image) => image.PendingPng ?? File.ReadAllBytes(Resolve(image.DuongDan));

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
