using System.Data;
using System.Data.Common;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QLThiTN.Shared;

public sealed record ReviewImage(int Id, string Path);
public sealed record ReviewAnswer(int Id, string Text, bool Correct, List<ReviewImage> Images, string? Selected);
public sealed record ReviewQuestion(int Id, string Type, string Text, List<ReviewImage> Images, List<ReviewAnswer> Answers, string? Response, decimal? Score, string Track);
public sealed record ReviewVariant(int Id, string Name, List<ReviewQuestion> Questions);
public sealed record ReviewDocument(int Id, string Title, string Subject, List<ReviewVariant> Variants, bool Approved, string Fingerprint, string? Student);

public static class ExamReview
{
    private static DataTable Query(DbConnection conn, DbTransaction? tx, string sql, params (string, object)[] parameters)
    {
        using var cmd = conn.CreateCommand(); cmd.Transaction=tx; cmd.CommandText=sql;
        foreach (var (name,value) in parameters) { var p=cmd.CreateParameter();p.ParameterName=name;p.Value=value;cmd.Parameters.Add(p); }
        using var reader=cmd.ExecuteReader(); var table=new DataTable();table.Load(reader);return table;
    }
    public static ReviewDocument Read(DbConnection conn, int examId, int? attemptId=null, DbTransaction? tx=null)
    {
        string? student=null; int? variantId=null;
        var choices=new DataTable();
        if(attemptId.HasValue)
        {
            var attempts=Query(conn,tx,@"SELECT dt.DeThiID,b.MaDeID,h.HoTen,b.TrangThai,b.BaiLamID FROM BaiLam b JOIN DotThi dt ON dt.DotThiID=b.DotThiID JOIN HocVien h ON h.HocVienID=b.HocVienID WHERE b.BaiLamID=@id",("@id",attemptId.Value));
            if(attempts.Rows.Count==0) throw new InvalidOperationException("Không tìm thấy bài làm.");
            var a=attempts.Rows[0];examId=(int)a["DeThiID"];variantId=(int)a["MaDeID"];
            student=$"{a["HoTen"]} · Bài #{attemptId} · {a["TrangThai"]}";
            choices=Query(conn,tx,@"SELECT ct.CauHoiID,ct.DiemDatDuoc,lc.DapAnID,lc.NoiDungTraLoi FROM ChiTietBaiLam ct LEFT JOIN LuaChonBaiLam lc ON lc.ChiTietID=ct.ChiTietID WHERE ct.BaiLamID=@id",("@id",attemptId.Value));
        }
        var exams=Query(conn,tx,"SELECT d.*,m.TenMon FROM DeThi d JOIN MonHoc m ON m.MonHocID=d.MonHocID WHERE d.DeThiID=@id",("@id",examId));
        if(exams.Rows.Count==0) throw new InvalidOperationException("Không tìm thấy đề thi.");
        var exam=exams.Rows[0];
        var variants=Query(conn,tx,"SELECT MaDeID,TenMaDe FROM MaDeThi WHERE DeThiID=@id ORDER BY MaDeID",("@id",examId));
        var questions=Query(conn,tx,@"SELECT c.*,dc.ThuTu FROM DeThi_CauHoi dc JOIN CauHoi c ON c.CauHoiID=dc.CauHoiID WHERE dc.DeThiID=@id ORDER BY dc.ThuTu",("@id",examId));
        var answers=Query(conn,tx,@"SELECT a.* FROM DapAn a JOIN DeThi_CauHoi dc ON dc.CauHoiID=a.CauHoiID WHERE dc.DeThiID=@id ORDER BY a.ThuTu,a.DapAnID",("@id",examId));
        var images=Query(conn,tx,@"SELECT h.* FROM HinhAnh h WHERE h.CauHoiID IN (SELECT CauHoiID FROM DeThi_CauHoi WHERE DeThiID=@id) OR h.DapAnID IN (SELECT a.DapAnID FROM DapAn a JOIN DeThi_CauHoi dc ON dc.CauHoiID=a.CauHoiID WHERE dc.DeThiID=@id) ORDER BY h.ThuTu,h.HinhAnhID",("@id",examId));
        var qOrder=Query(conn,tx,"SELECT x.* FROM MaDe_CauHoi x JOIN MaDeThi m ON m.MaDeID=x.MaDeID WHERE m.DeThiID=@id",("@id",examId));
        var aOrder=Query(conn,tx,"SELECT x.* FROM MaDe_DapAn x JOIN MaDeThi m ON m.MaDeID=x.MaDeID WHERE m.DeThiID=@id",("@id",examId));
        var result=new List<ReviewVariant>();
        foreach(DataRow v in variants.Rows)
        {
            int id=(int)v["MaDeID"];
            if(variantId.HasValue && variantId!=id) continue;
            var order=qOrder.AsEnumerable().Where(r=>(int)r["MaDeID"]==id).ToDictionary(r=>(int)r["CauHoiID"],r=>(int)r["ThuTu"]);
            if(order.Count!=questions.Rows.Count || order.Values.Distinct().Count()!=order.Count || questions.AsEnumerable().Any(q=>!order.ContainsKey((int)q["CauHoiID"])))
                throw new InvalidOperationException($"Mã đề {v["TenMaDe"]} thiếu hoặc trùng thứ tự câu hỏi. Hãy trộn lại trước khi duyệt.");
            var qs=new List<ReviewQuestion>();
            foreach(var q in questions.AsEnumerable().OrderBy(r=>order.GetValueOrDefault((int)r["CauHoiID"],(int)r["ThuTu"])))
            {
                int qid=(int)q["CauHoiID"];
                var selected=choices.AsEnumerable().Where(r=>(int)r["CauHoiID"]==qid).ToArray();
                var aos=aOrder.AsEnumerable().Where(r=>(int)r["MaDeID"]==id && (int)r["CauHoiID"]==qid).ToDictionary(r=>(int)r["DapAnID"],r=>(int)r["ThuTu"]);
                var answerIds=answers.AsEnumerable().Where(r=>(int)r["CauHoiID"]==qid).Select(r=>(int)r["DapAnID"]).ToHashSet();
                if(aos.Count!=answerIds.Count || !answerIds.SetEquals(aos.Keys) || aos.Values.Distinct().Count()!=aos.Count)
                    throw new InvalidOperationException($"Mã đề {v["TenMaDe"]}, câu #{qid} thiếu thứ tự đáp án. Hãy trộn lại.");
                var ans=answers.AsEnumerable().Where(r=>(int)r["CauHoiID"]==qid).OrderBy(r=>aos.GetValueOrDefault((int)r["DapAnID"],(int)r["ThuTu"]))
                    .Select(r=>new ReviewAnswer((int)r["DapAnID"],r["NoiDung"].ToString()!, (bool)r["LaDapAnDung"],
                        images.AsEnumerable().Where(h=>h["DapAnID"] is int aid && aid==(int)r["DapAnID"]).Select(h=>new ReviewImage((int)h["HinhAnhID"],h["DuongDan"].ToString()!)).ToList(),
                        selected.Where(c=>c["DapAnID"] is int aid && aid==(int)r["DapAnID"]).Select(c=>c["NoiDungTraLoi"]==DBNull.Value ? "Đã chọn" : c["NoiDungTraLoi"].ToString()).FirstOrDefault())).ToList();
                qs.Add(new(qid,q["LoaiCauHoi"].ToString()!,q["NoiDung"].ToString()!,
                    images.AsEnumerable().Where(h=>h["CauHoiID"] is int cid && cid==qid && h["DapAnID"]==DBNull.Value).Select(h=>new ReviewImage((int)h["HinhAnhID"],h["DuongDan"].ToString()!)).ToList(),ans,
                    selected.FirstOrDefault(c=>c["DapAnID"]==DBNull.Value)?["NoiDungTraLoi"].ToString(),
                    selected.FirstOrDefault()?["DiemDatDuoc"] is decimal score ? score : null,q["NhomTHPT"].ToString()!));
            }
            result.Add(new(id,v["TenMaDe"].ToString()!,qs));
        }
        var fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{Title=exam["TenDe"].ToString(),Subject=exam["TenMon"].ToString(),Variants=result}))));
        return new(examId,exam["TenDe"].ToString()!,exam["TenMon"].ToString()!,result,(bool)exam["DaDuyet"],fingerprint,student);
    }
    public static void Approve(DbConnection conn,int examId,int accountId,string fingerprint)
    {
        using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        var report=Read(conn,examId,null,tx);
        if(report.Fingerprint!=fingerprint) throw new InvalidOperationException("Nội dung đã thay đổi. Hãy mở và xem lại tất cả mã đề trước khi duyệt.");
        if(report.Variants.Count==0) throw new InvalidOperationException("Đề chưa có mã đề.");
        var format=ExamFormat.ForSubject(report.Subject);
        foreach(var variant in report.Variants)
        {
            format.Validate(variant.Questions.Select(q=>q.Type));
            int Rank(ReviewQuestion q)=>q.Type=="TracNghiem"?0:q.Type=="DungSai"?1:2;
            if(!variant.Questions.Select(Rank).SequenceEqual(variant.Questions.Select(Rank).Order()))
                throw new InvalidOperationException("Thứ tự các phần chưa đúng. Hãy trộn lại đề trước khi duyệt.");
            if(report.Subject.Contains("Tin",StringComparison.OrdinalIgnoreCase))
            {
                var ds=variant.Questions.Where(q=>q.Type=="DungSai").ToList();
                if(ds.Count(q=>q.Track=="Chung")!=2 || !(ds.Count(q=>q.Track=="KhoaHocMayTinh")==2 || ds.Count(q=>q.Track=="TinHocUngDung")==2))
                    throw new InvalidOperationException("Tin học cần 2 câu đúng/sai chung và 2 câu cùng định hướng. Phân loại nhánh trong ngân hàng trước khi duyệt.");
            }
            foreach(var q in variant.Questions)
                if(q.Type=="TracNghiem" && (q.Answers.Count!=4 || q.Answers.Count(a=>a.Correct)!=1) ||
                   q.Type=="DungSai" && q.Answers.Count!=4 || q.Type=="TraLoiNgan" && q.Answers.Count!=1)
                    throw new InvalidOperationException($"Câu #{q.Id} thiếu/sai cấu trúc đáp án.");
        }
        Query(conn,tx,"UPDATE DeThi SET DaDuyet=1,NguoiDuyetID=@user,NgayDuyet=SYSDATETIME() WHERE DeThiID=@id",("@id",examId),("@user",accountId));
        tx.Commit();
    }
    public static string Html(ReviewDocument doc,Func<ReviewImage,string> imageUrl)
    {
        string E(string? s)=>WebUtility.HtmlEncode(s) ?? "";
        var b=new StringBuilder(@"<article class='exam-review'>
            <style>
            .variant-nav{position:sticky;top:0;z-index:5;background:#fff;border:1px solid #b9d7ff;border-radius:8px;padding:12px;box-shadow:0 2px 8px #0001}
            .variant-tab{margin:5px 0 5px 10px;padding:7px 16px;border:1px solid #9bc9f7;border-radius:18px;background:#e8f4ff;color:#075db3;font-weight:700;cursor:pointer}
            .variant-tab.active{background:#087bd3;color:#fff;border-color:#087bd3}
            .variant-panel{display:none;border:1px solid #d9e5f3;border-radius:10px;padding:24px;margin:20px 0;background:#fff}
            .variant-panel.active{display:block}
            </style>");
        b.Append("<h1>").Append(E(doc.Title)).Append("</h1><p>").Append(E(doc.Subject)).Append("</p>");
        if(doc.Student!=null)b.Append("<h2>").Append(E(doc.Student)).Append("</h2><p>Câu không có lựa chọn được ghi là chưa trả lời.</p>");
        if(doc.Variants.Count>0)
        {
            b.Append("<nav class='variant-nav'><strong>Chọn mã đề:</strong>");
            for(var i=0;i<doc.Variants.Count;i++)
            {
                var variant=doc.Variants[i];
                b.Append("<button type='button' class='variant-tab").Append(i==0?" active":"")
                    .Append("' data-target='ma-de-").Append(variant.Id).Append("'>Mã ").Append(E(variant.Name))
                    .Append(" · ").Append(variant.Questions.Count).Append(" câu</button>");
            }
            b.Append("</nav><p><b>Tổng số mã đề: ").Append(doc.Variants.Count).Append(".</b> Mỗi mã được tách thành một tab riêng; chọn tab để xem nội dung.</p>");
        }
        void Content(string text,List<ReviewImage> images){ b.Append("<div style='white-space:pre-wrap'>").Append(E(text)).Append("</div>");foreach(var img in images)b.Append("<img style='max-width:100%;max-height:350px' src='").Append(E(imageUrl(img))).Append("'>"); }
        for(var variantIndex=0;variantIndex<doc.Variants.Count;variantIndex++)
        {
            var v=doc.Variants[variantIndex];
            b.Append("<section class='variant-panel").Append(variantIndex==0?" active":"").Append("' id='ma-de-").Append(v.Id).Append("'><h2>Mã đề ").Append(E(v.Name)).Append("</h2>");string previous="";int n=0;
            if(doc.Subject.Contains("Tin",StringComparison.OrdinalIgnoreCase))b.Append("<p>Đề theo định hướng: ").Append(v.Questions.Any(q=>q.Track=="TinHocUngDung")?"Tin học ứng dụng":"Khoa học máy tính").Append(" — 2 câu đúng/sai phần chung và 2 câu phần định hướng.</p>");
            foreach(var q in v.Questions)
            {
                if(previous!=q.Type){previous=q.Type;n=0;b.Append("<h3>").Append(q.Type switch{"TracNghiem"=>"Phần I. Trắc nghiệm","DungSai"=>"Phần II. Đúng / Sai",_=>"Phần III. Trả lời ngắn"}).Append("</h3>");}
                b.Append("<div style='border-top:1px solid #ddd;padding:16px 0'><b>Câu ").Append(++n).Append(" · ID ").Append(q.Id).Append("</b>");Content(q.Text,q.Images);
                if(doc.Student!=null)b.Append("<p><b>Thí sinh: ").Append(E(q.Response ?? (q.Answers.Any(a=>a.Selected!=null)?"Lựa chọn được đánh dấu bên dưới":"Chưa trả lời"))).Append("</b> · Điểm câu: ").Append(q.Score?.ToString()??"Chưa có").Append("</p>");
                for(int i=0;i<q.Answers.Count;i++){var a=q.Answers[i];b.Append("<div style='margin:8px;padding:8px;background:").Append(a.Selected!=null?"#bfeaff":"#f7f7f7").Append("'><b>").Append(q.Type=="TraLoiNgan"?"Đáp số":((char)((q.Type=="DungSai"?'a':'A')+i)).ToString()).Append(" · ").Append(q.Type=="DungSai"?(a.Correct?"Đúng":"Sai"):(a.Correct?"Đáp án đúng":"")).Append(a.Selected!=null?(a.Selected=="Đã chọn"?" · ☑ THÍ SINH ĐÃ CHỌN":" · ☑ Thí sinh: "+E(a.Selected)):doc.Student!=null?" · Chưa chọn":"").Append("</b>");Content(a.Text,a.Images);b.Append("</div>");}
                b.Append("</div>");
            }
            b.Append("</section>");
        }
        return b.Append(@"<script>
            document.querySelectorAll('.variant-tab').forEach(function(button){
              button.addEventListener('click',function(){
                document.querySelectorAll('.variant-tab').forEach(function(x){x.classList.remove('active')});
                document.querySelectorAll('.variant-panel').forEach(function(x){x.classList.remove('active')});
                button.classList.add('active');
                var panel=document.getElementById(button.dataset.target);
                if(panel){panel.classList.add('active');panel.scrollIntoView({behavior:'smooth',block:'start'});}
              });
            });
            </script></article>").ToString();
    }
}
