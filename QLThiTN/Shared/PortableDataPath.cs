namespace QLThiTN.Shared;

/// <summary>
/// Giải quyết đường dẫn dữ liệu tương đối khi ứng dụng chạy từ thư mục bin,
/// thư mục dự án hoặc một gói triển khai có thư mục data đặt bên cạnh.
/// </summary>
public static class PortableDataPath
{
    public static string ResolveExistingDirectory(string configuredPath, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            throw new InvalidOperationException("Chưa cấu hình đường dẫn thư mục dữ liệu.");

        if (Path.IsPathRooted(configuredPath))
            return EnsureExists(Path.GetFullPath(configuredPath));

        var checkedPaths = new List<string>();
        var current = new DirectoryInfo(Path.GetFullPath(baseDirectory));

        while (current is not null)
        {
            var candidate = Path.GetFullPath(Path.Combine(current.FullName, configuredPath));
            if (!checkedPaths.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                checkedPaths.Add(candidate);

            if (Directory.Exists(candidate))
                return candidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Không tìm thấy thư mục dữ liệu '{configuredPath}'. Đã kiểm tra: " +
            string.Join("; ", checkedPaths));
    }

    private static string EnsureExists(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Không tìm thấy thư mục dữ liệu: {path}");

        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
