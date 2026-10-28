using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using QLThiTN.WinForms.Models;

namespace QLThiTN.WinForms.Services;

public static class QuestionDuplicates
{
    public static string Normalize(string? text) => Regex.Replace(WebUtility.HtmlDecode(
        Regex.Replace(text ?? "", @"<[^>]+>", " ")).Normalize(NormalizationForm.FormC), @"\s+", " ").Trim().ToLowerInvariant();

    public static void Reject(SqlConnection conn, SqlTransaction tx, CauHoi question)
    {
        // All WinForms/import writers use the same transaction lock, including concurrent imports.
        using var gate = new SqlCommand("DECLARE @r int; EXEC @r=sp_getapplock @Resource=N'QuestionDuplicateWrite',@LockMode='Exclusive',@LockOwner='Transaction'; IF @r<0 THROW 50001,N'Không khóa được ngân hàng câu hỏi.',1;", conn, tx);
        gate.ExecuteNonQuery();
        var key = Normalize(question.NoiDung);
        using var read = new SqlCommand(@"SELECT c.CauHoiID,c.NoiDung FROM CauHoi c JOIN ChuDe cd ON cd.ChuDeID=c.ChuDeID
            WHERE cd.MonHocID=(SELECT MonHocID FROM ChuDe WHERE ChuDeID=@cd) AND c.CauHoiID<>@id", conn, tx);
        read.Parameters.AddWithValue("@cd", question.ChuDeID);
        read.Parameters.AddWithValue("@id", question.CauHoiID);
        var candidates=new List<(int Id,string Text)>();
        using(var reader = read.ExecuteReader())while(reader.Read())candidates.Add((reader.GetInt32(0),reader.GetString(1)));
        foreach(var candidate in candidates)
        {
            if(Normalize(candidate.Text)!=key)continue;
            if(key.Length==0)
            {
                using var images=new SqlCommand("SELECT DuongDan FROM HinhAnh WHERE CauHoiID=@id AND DapAnID IS NULL ORDER BY ThuTu,HinhAnhID",conn,tx);
                images.Parameters.AddWithValue("@id",candidate.Id);
                var stored=new List<QuestionImage>();using(var reader=images.ExecuteReader())while(reader.Read())stored.Add(new(){DuongDan=reader.GetString(0)});
                if(ImageKey(stored)!=ImageKey(question.HinhAnhs??[]) || stored.Count==0)continue;
            }
            throw new InvalidOperationException($"Câu hỏi trùng với câu #{candidate.Id} trong cùng môn (kể cả câu đã ẩn). Không thể lưu.");
        }
    }
    public static string ImageKey(IEnumerable<QuestionImage> images)=>string.Join(";",images.Select(i=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(QuestionImageStore.Read(i)))).Order());
}
