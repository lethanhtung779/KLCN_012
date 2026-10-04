using System.Diagnostics;

namespace QLThiTN.WinForms.Services;

/// <summary>Sao luu / phuc hoi co so du lieu ThiTracNghiem (BACKUP/RESTORE DATABASE).</summary>
public class BackupService
{
    public const string ThuMucBackupMacDinh = @"D:\KLCN\backup";

    /// <summary>Sao luu DB ra file .bak ten co moc thoi gian. Tra ve duong dan file.</summary>
    public string SaoLuu(string thuMuc)
    {
        Directory.CreateDirectory(thuMuc);
        var file = Path.Combine(thuMuc, $"ThiTracNghiem_{DateTime.Now:yyyyMMdd_HHmmss}.bak")
            .Replace("'", "''");

        var sw = Stopwatch.StartNew();
        Database.Execute(
            $"BACKUP DATABASE [ThiTracNghiem] TO DISK = N'{file}' WITH INIT, NAME = N'ThiTracNghiem-Full';");
        sw.Stop();
        return file;
    }

    /// <summary>Phuc hoi DB tu file .bak. Server phai cho phep single-user trong luc restore.</summary>
    public void PhucHoi(string fileBak)
    {
        if (!File.Exists(fileBak))
            throw new FileNotFoundException("Không tìm thấy file backup.", fileBak);

        // Ngat ket noi nguoi dung khac, restore, tra lai che do multi-user
        Database.Execute(@"
            IF DB_ID('ThiTracNghiem') IS NOT NULL
                ALTER DATABASE [ThiTracNghiem] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;");
        try
        {
            var file = fileBak.Replace("'", "''");
            // Xac dinh ten logical file tu backup de dung MOVE dung di
            var dt = Database.Query("RESTORE FILELISTONLY FROM DISK = N'" + file + "'");
            var dataFile = "";
            var logFile = "";
            foreach (System.Data.DataRow row in dt.Rows)
            {
                var type = row["Type"].ToString();
                if (type == "D") dataFile = row["LogicalName"].ToString() ?? "";
                if (type == "L") logFile = row["LogicalName"].ToString() ?? "";
            }

            var restore = $@"RESTORE DATABASE [ThiTracNghiem] FROM DISK = N'{file}' WITH REPLACE";
            if (dataFile != "" && logFile != "")
                restore += $", MOVE N'{dataFile}' TO N'D:\\KLCN\\ThiTracNghiem.mdf', MOVE N'{logFile}' TO N'D:\\KLCN\\ThiTracNghiem_log.ldf'";
            Database.Execute(restore);
        }
        finally
        {
            try { Database.Execute("ALTER DATABASE [ThiTracNghiem] SET MULTI_USER;"); }
            catch { /* ket noi bi ngat khi restore la binh thuong */ }
        }
    }
}
