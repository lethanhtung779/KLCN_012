using System.Data;
using Microsoft.Data.SqlClient;

namespace QLThiTN.WinForms.Services;

public class BackupService
{
    public static readonly string ThuMucBackupMacDinh = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "QLThiTN", "backup");

    private static string Name()
    {
        var name = new SqlConnectionStringBuilder(Database.ConnectionString).InitialCatalog;
        return string.IsNullOrWhiteSpace(name)
            ? throw new InvalidOperationException("Connection string chưa xác định database.") : name;
    }

    private static string Id(string value) => "[" + value.Replace("]", "]]", StringComparison.Ordinal) + "]";
    private static string Literal(string value) => "N'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    private static SqlConnection Master()
    {
        var builder = new SqlConnectionStringBuilder(Database.ConnectionString) { InitialCatalog = "master" };
        var conn = new SqlConnection(builder.ConnectionString);
        conn.Open();
        return conn;
    }

    private static void Run(SqlConnection conn, string sql)
    {
        using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 0 };
        cmd.ExecuteNonQuery();
    }

    private static DataTable Query(SqlConnection conn, string sql, Action<SqlParameterCollection>? bind = null)
    {
        using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 0 };
        bind?.Invoke(cmd.Parameters);
        using var adapter = new SqlDataAdapter(cmd);
        var data = new DataTable();
        adapter.Fill(data);
        return data;
    }

    public string SaoLuu(string thuMuc)
    {
        Session.YeuCauQuanTriVien();
        if (string.IsNullOrWhiteSpace(thuMuc)) throw new ArgumentException("Hãy chọn thư mục sao lưu.");
        var name = Name();
        Directory.CreateDirectory(thuMuc);
        var file = Path.Combine(thuMuc, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
        using var conn = Master();
        Run(conn, $"BACKUP DATABASE {Id(name)} TO DISK = {Literal(file)} WITH INIT, CHECKSUM;");
        Run(conn, $"RESTORE VERIFYONLY FROM DISK = {Literal(file)} WITH CHECKSUM;");
        return file;
    }

    public void PhucHoi(string fileBak)
    {
        Session.YeuCauQuanTriVien();
        if (!File.Exists(fileBak)) throw new FileNotFoundException("Không tìm thấy file backup.", fileBak);
        var name = Name();
        using var conn = Master();
        var header = Query(conn, $"RESTORE HEADERONLY FROM DISK = {Literal(fileBak)}");
        if (header.Rows.Count == 0 || !string.Equals(header.Rows[0]["DatabaseName"].ToString(), name, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("File sao lưu không thuộc database hiện tại.");
        Run(conn, $"RESTORE VERIFYONLY FROM DISK = {Literal(fileBak)} WITH CHECKSUM;");

        var source = Query(conn, $"RESTORE FILELISTONLY FROM DISK = {Literal(fileBak)}");
        var target = Query(conn, "SELECT type_desc,physical_name FROM sys.master_files WHERE database_id=DB_ID(@db) ORDER BY file_id",
            p => p.AddWithValue("@db", name));
        if (source.Rows.Count != target.Rows.Count || target.Rows.Count == 0)
            throw new InvalidOperationException("Cấu trúc file backup không khớp database. Hãy thử phục hồi trên bản sao.");
        var data = target.Rows.Cast<DataRow>().Where(r => r["type_desc"].ToString() == "ROWS")
            .Select(r => r["physical_name"].ToString()!).ToList();
        var logs = target.Rows.Cast<DataRow>().Where(r => r["type_desc"].ToString() == "LOG")
            .Select(r => r["physical_name"].ToString()!).ToList();
        var moves = new List<string>();
        foreach (DataRow row in source.Rows)
        {
            var paths = row["Type"].ToString() == "L" ? logs : data;
            if (paths.Count == 0) throw new InvalidOperationException("Loại file backup không khớp database.");
            moves.Add($"MOVE {Literal(row["LogicalName"].ToString()!)} TO {Literal(paths[0])}");
            paths.RemoveAt(0);
        }

        var identifier = Id(name);
        var singleUser = false;
        try
        {
            Run(conn, $"ALTER DATABASE {identifier} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;");
            singleUser = true;
            Run(conn, $"RESTORE DATABASE {identifier} FROM DISK = {Literal(fileBak)} WITH REPLACE, RECOVERY, {string.Join(", ", moves)};");
        }
        finally
        {
            if (singleUser)
            {
                try { Run(conn, $"ALTER DATABASE {identifier} SET MULTI_USER;"); }
                catch { /* SQL Server có thể đã tự chuyển trạng thái sau restore. */ }
            }
        }
    }
}
