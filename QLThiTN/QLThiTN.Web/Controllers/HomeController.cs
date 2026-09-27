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

        var model = new HomeViewModel
        {
            TotalExams = schedules.Count,
            UpcomingExams = schedules.Count(e => e.EndTime >= DateTime.Now),
            CompletedExams = 0,
            AverageScore = 0m,
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