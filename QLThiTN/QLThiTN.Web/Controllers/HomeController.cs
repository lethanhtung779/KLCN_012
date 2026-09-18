using Microsoft.AspNetCore.Mvc;
using QLThiTN.Web.ViewModels;

namespace QLThiTN.Web.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var model = new HomeViewModel
        {
            TotalExams = MockData.Exams.Count,
            UpcomingExams = MockData.Exams.Count(e => e.StartTime > DateTime.Now),
            CompletedExams = MockData.CompletedExams.Count,
            AverageScore = MockData.CompletedExams.Any() ? MockData.CompletedExams.Average(e => e.Score) : 0m,
            LatestExams = MockData.Exams.Take(6).ToList(),
            LatestPublicExams = MockData.Exams.Where(e => e.IsPublic).Take(3).ToList()
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult Error() => View();
}