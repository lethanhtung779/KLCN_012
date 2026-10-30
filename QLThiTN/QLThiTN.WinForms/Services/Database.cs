using System.Data;
using Microsoft.Data.SqlClient;

namespace QLThiTN.WinForms.Services;

/// <summary>
/// Ket noi va thuc thi lenh SQL Server (ADO.NET). Connection string doc tu
/// appsettings.json — dung chung co so du lieu ThiTracNghiem voi website.
/// </summary>
public static class Database
{
    private static string _connectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=ThiTracNghiem;Trusted_Connection=True;TrustServerCertificate=True";

    public static string ConnectionString => _connectionString;

    /// <summary>Them cot goi tai khoan va cot de mien phi neu co so du lieu cu chua co.</summary>
    public static void EnsureGoiColumns()
    {
        Execute(@"
IF COL_LENGTH('HocVien', 'LoaiGoi') IS NULL
    ALTER TABLE HocVien ADD LoaiGoi NVARCHAR(20) NOT NULL CONSTRAINT DF_HocVien_LoaiGoi DEFAULT N'MienPhi';
IF COL_LENGTH('DeThi', 'LaMienPhi') IS NULL
    ALTER TABLE DeThi ADD LaMienPhi BIT NOT NULL CONSTRAINT DF_DeThi_LaMienPhi DEFAULT 0;

DECLARE @constraint sysname;
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
SELECT name FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID(N'dbo.DeThi')
  AND definition LIKE N'%HinhThucTao%'
  AND definition NOT LIKE N'%MaTranTHPTQG%';
OPEN cur;
FETCH NEXT FROM cur INTO @constraint;
WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @dropSql nvarchar(max) = N'ALTER TABLE dbo.DeThi DROP CONSTRAINT ' + QUOTENAME(@constraint);
    EXEC sp_executesql @dropSql;
    FETCH NEXT FROM cur INTO @constraint;
END
CLOSE cur;
DEALLOCATE cur;

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.DeThi')
      AND definition LIKE N'%HinhThucTao%'
      AND definition LIKE N'%MaTranTHPTQG%'
)
BEGIN
    ALTER TABLE dbo.DeThi WITH CHECK ADD CONSTRAINT CK_DeThi_HinhThucTao
        CHECK (HinhThucTao IN (N'ThuCong', N'TuDong', N'MaTranTHPTQG'));
END");
    }

    public static void LoadConfig(string baseDir)
    {
            var path = Path.Combine(baseDir, "appsettings.json");
            if (!File.Exists(path))
                throw new FileNotFoundException("Không tìm thấy cấu hình cơ sở dữ liệu.", path);
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                cs.TryGetProperty("ThiTracNghiem", out var val))
            {
                var s = val.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    _connectionString = s;
                    return;
                }
            }
            throw new InvalidOperationException("Thiếu ConnectionStrings:ThiTracNghiem trong " + path);
    }

    public static SqlConnection Open()
    {
        var conn = new SqlConnection(_connectionString);
        conn.Open();
        return conn;
    }

    /// <summary>Thuc thi lenh khong tra ve du lieu; tra ve so dong bi anh huong.</summary>
    public static int Execute(string sql, Action<SqlParameterCollection>? bind = null)
    {
        using var conn = Open();
        using var cmd = new SqlCommand(sql, conn);
        bind?.Invoke(cmd.Parameters);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>Thuc thi lenh tra ve 1 gia tri (thuong la IDENTITY hay COUNT).</summary>
    public static object? ExecuteScalar(string sql, Action<SqlParameterCollection>? bind = null)
    {
        using var conn = Open();
        using var cmd = new SqlCommand(sql, conn);
        bind?.Invoke(cmd.Parameters);
        return cmd.ExecuteScalar();
    }

    public static int ExecuteScalarInt(string sql, Action<SqlParameterCollection>? bind = null) =>
        Convert.ToInt32(ExecuteScalar(sql, bind) ?? 0);

    public static string ExecuteScalarString(string sql, Action<SqlParameterCollection>? bind = null) =>
        ExecuteScalar(sql, bind)?.ToString() ?? "";

    /// <summary>Lay DataTable cho DataGridView va cac man hinh danh sach.</summary>
    public static DataTable Query(string sql, Action<SqlParameterCollection>? bind = null)
    {
        using var conn = Open();
        using var cmd = new SqlCommand(sql, conn);
        bind?.Invoke(cmd.Parameters);
        using var da = new SqlDataAdapter(cmd);
        var dt = new DataTable();
        da.Fill(dt);
        return dt;
    }

    /// <summary>Doc tung dong bang delegate — dung cho cac truy van manh hon DataTable.</summary>
    public static List<T> ReadList<T>(string sql, Func<SqlDataReader, T> map, Action<SqlParameterCollection>? bind = null)
    {
        var list = new List<T>();
        using var conn = Open();
        using var cmd = new SqlCommand(sql, conn);
        bind?.Invoke(cmd.Parameters);
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) list.Add(map(reader));
        return list;
    }
}
