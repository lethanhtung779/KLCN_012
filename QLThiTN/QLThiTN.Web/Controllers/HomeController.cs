using Microsoft.AspNetCore.Mvc;
using QLThiTN.Web.Services;
using QLThiTN.Web.ViewModels;

namespace QLThiTN.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _api;

    public HomeController(ApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var exams = await _api.GetDeThisAsync();
        var schedules = exams.Select(ExamMapping.ToSchedule).ToList();

        int completedExams = 0;
        decimal averageScore = 0m;

        var hocVienId = HttpContext.Session.GetInt32("HocVienId");
        if (hocVienId.HasValue)
        {
            var stats = await _api.GetStudentStatsAsync(hocVienId.Value);
            if (stats != null)
            {
                completedExams = stats.TotalExams;
                averageScore = stats.AverageScore;
            }
        }

        var model = new HomeViewModel
        {
            TotalExams = schedules.Count,
            UpcomingExams = schedules.Count(e => e.EndTime >= DateTime.Now),
            CompletedExams = completedExams,
            AverageScore = averageScore,
            LatestExams = schedules.Take(6).ToList(),
            LatestPublicExams = schedules
                .Where(e => e.ExamKind == ExamKind.PublicMock)
                .Take(3)
                .ToList()
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult Error() => View();
}