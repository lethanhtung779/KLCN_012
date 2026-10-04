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
        "Server=.\\SQLEXPRESS;Database=ThiTracNghiem;Trusted_Connection=True;TrustServerCertificate=True";

    public static string ConnectionString => _connectionString;

    public static void LoadConfig(string baseDir)
    {
        try
        {
            var path = Path.Combine(baseDir, "appsettings.json");
            if (!File.Exists(path)) return;
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                cs.TryGetProperty("ThiTracNghiem", out var val))
            {
                var s = val.GetString();
                if (!string.IsNullOrWhiteSpace(s)) _connectionString = s;
            }
        }
        catch
        {
            // giu connection string mac dinh
        }
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
