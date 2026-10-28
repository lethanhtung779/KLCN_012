using Microsoft.AspNetCore.Mvc;
using QLThiTN.Shared;
using QLThiTN.Web.Services;

namespace QLThiTN.Web.Controllers;
public class AdminReviewController(ApiClient api) : Controller
{
    private bool Admin=>HttpContext.Session.GetString("UserRole")=="QuanTriVien";
    public record ExamRow(int DeThiID,string TenDe,bool DaDuyet);
    public record AttemptRow(int BaiLamID,string HoTen,string TrangThai,decimal? TongDiem);
    public async Task<IActionResult> Index()
    {
        if(!Admin)return RedirectToAction("Login","Account",new{returnUrl="/AdminReview"});
        return View(await api.AdminReadAsync<List<ExamRow>>("") ?? []);
    }
    public async Task<IActionResult> Exam(int id)
    {
        if(!Admin)return StatusCode(403);
        ViewBag.Attempts=await api.AdminReadAsync<List<AttemptRow>>($"attempts/{id}") ?? [];
        ViewBag.ApiBase=api.BaseUrl;
        return View("Review",await api.AdminReadAsync<ReviewDocument>($"exam/{id}"));
    }
    public async Task<IActionResult> Attempt(int id)
    {
        if(!Admin)return StatusCode(403);
        ViewBag.ApiBase=api.BaseUrl;
        return View("Review",await api.AdminReadAsync<ReviewDocument>($"attempt/{id}"));
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id,string fingerprint,bool reviewed)
    {
        if(!Admin)return StatusCode(403);
        TempData["ReviewMessage"]=await api.ApproveExamAsync(id,fingerprint,reviewed) ?? "Đã duyệt. Đề được phép xuất hiện trên web; lịch mở/đóng và quyền dự thi vẫn áp dụng.";
        return RedirectToAction(nameof(Exam),new{id});
    }
}
