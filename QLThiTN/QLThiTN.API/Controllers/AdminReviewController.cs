using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Data;
using QLThiTN.Shared;

namespace QLThiTN.API.Controllers;

[ApiController, Route("api/admin-review"), Authorize(Roles="QuanTriVien")]
public class AdminReviewController(QLThiTNDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await db.DeThis.OrderByDescending(d=>d.DeThiID)
        .Select(d=>new {d.DeThiID,d.TenDe,d.DaDuyet}).ToListAsync());
    [HttpGet("exam/{id:int}")]
    public async Task<IActionResult> Exam(int id)
    {
        await db.Database.OpenConnectionAsync();
        try { return Ok(ExamReview.Read(db.Database.GetDbConnection(),id)); }
        catch(InvalidOperationException ex){return BadRequest(new{message=ex.Message});}
    }
    [HttpGet("attempt/{id:int}")]
    public async Task<IActionResult> Attempt(int id)
    {
        await db.Database.OpenConnectionAsync();
        try{return Ok(ExamReview.Read(db.Database.GetDbConnection(),0,id));}
        catch(InvalidOperationException ex){return BadRequest(new{message=ex.Message});}
    }
    [HttpGet("attempts/{examId:int}")]
    public async Task<IActionResult> Attempts(int examId)=>Ok(await db.BaiLams.Where(b=>b.DotThi!.DeThiID==examId)
        .Select(b=>new{b.BaiLamID,b.HocVien!.HoTen,b.TrangThai,b.TongDiem}).ToListAsync());
    public sealed record Approval(string Fingerprint, bool Reviewed);
    [HttpPost("exam/{id:int}/approve")]
    public async Task<IActionResult> Approve(int id,Approval request)
    {
        if(!request.Reviewed)return BadRequest(new{message="Hãy xác nhận đã xem toàn bộ mã đề."});
        await db.Database.OpenConnectionAsync();
        try { ExamReview.Approve(db.Database.GetDbConnection(),id,User.AccountId(),request.Fingerprint);return Ok(new{message="Đã duyệt đề."}); }
        catch(InvalidOperationException ex){return Conflict(new{message=ex.Message});}
    }
}
