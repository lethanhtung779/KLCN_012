using System.Diagnostics;

namespace QLThiTN.WinForms.Services;

/// <summary>Sao luu / phuc hoi co so du lieu ThiTracNghiem (BACKUP/RESTORE DATABASE).</summary>
public class BackupService
{
    public const string ThuMucBackupMacDinh = @"D:\KLCN\backup";

    private static string GetCurrentDatabaseName()
    {
        try
        {
            var db = Database.ExecuteScalarString("SELECT DB_NAME()");
            if (!string.IsNullOrWhiteSpace(db)) return db;
        }
        catch { }
        try
        {
            var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Database.ConnectionString);
            if (!string.IsNullOrWhiteSpace(csb.InitialCatalog)) return csb.InitialCatalog;
        }
        catch { }
        return "ThiTracNghiem";
    }

    /// <summary>Sao luu DB ra file .bak ten co moc thoi gian. Tra ve duong dan file.</summary>
    public string SaoLuu(string thuMuc)
    {
        Session.YeuCauQuanTriVien();
        var dbName = GetCurrentDatabaseName();
        Directory.CreateDirectory(thuMuc);
        var file = Path.Combine(thuMuc, $"{dbName}_{DateTime.Now:yyyyMMdd_HHmmss}.bak")
            .Replace("'", "''");

        var sw = Stopwatch.StartNew();
        Database.Execute(
            $"BACKUP DATABASE [{dbName}] TO DISK = N'{file}' WITH INIT, NAME = N'{dbName}-Full';");
        sw.Stop();
        return file;
    }

    /// <summary>Phuc hoi DB tu file .bak. Server phai cho phep single-user trong luc restore.</summary>
    public void PhucHoi(string fileBak)
    {
        Session.YeuCauQuanTriVien();
        if (!File.Exists(fileBak))
            throw new FileNotFoundException("Không tìm thấy file backup.", fileBak);

        var dbName = GetCurrentDatabaseName();

        // Ngat ket noi nguoi dung khac, restore, tra lai che do multi-user
        Database.Execute($@"
            IF DB_ID('{dbName}') IS NOT NULL
                ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;");
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

            var restore = $@"RESTORE DATABASE [{dbName}] FROM DISK = N'{file}' WITH REPLACE";
            if (dataFile != "" && logFile != "")
                restore += $", MOVE N'{dataFile}' TO N'D:\\KLCN\\{dbName}.mdf', MOVE N'{logFile}' TO N'D:\\KLCN\\{dbName}_log.ldf'";
            Database.Execute(restore);
        }
        finally
        {
            try { Database.Execute($"ALTER DATABASE [{dbName}] SET MULTI_USER;"); }
            catch { /* ket noi bi ngat khi restore la binh thuong */ }
        }
    }
}
