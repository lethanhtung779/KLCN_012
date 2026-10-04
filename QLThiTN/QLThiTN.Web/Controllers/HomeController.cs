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
        var stats = await _api.GetExamStatsAsync();
        var statsByDe = (stats?.TheoDe ?? new()).ToDictionary(x => x.DeThiID);

        int completedExams = 0;
        decimal averageScore = 0m;

        var hocVienId = HttpContext.Session.GetInt32("HocVienId");
        if (hocVienId.HasValue)
        {
            var stats2 = await _api.GetStudentStatsAsync(hocVienId.Value);
            if (stats2 != null)
            {
                completedExams = stats2.TotalExams;
                averageScore = stats2.AverageScore;
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
                .Select(s =>
                {
                    var item = new KhoDeExamItem
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Description = s.Description,
                        Duration = s.Duration,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        MaxStudents = s.MaxStudents,
                        SubjectName = s.SubjectName,
                        QuestionCount = s.QuestionCount,
                        HasRegistered = s.HasRegistered,
                        CanRegister = s.CanRegister,
                        ExamKind = s.ExamKind,
                        IsAssignedToMe = s.IsAssignedToMe,
                        ScorePublished = s.ScorePublished,
                        DotThiID = s.DotThiID,
                        SoDangKy = s.SoDangKy,
                        SoChoConLai = s.SoChoConLai,
                        Nam = s.StartTime?.Year ?? DateTime.Now.Year
                    };
                    if (statsByDe.TryGetValue(s.Id, out var st))
                    {
                        item.SoLuotThi = st.SoLuotThi;
                        item.DiemTrungBinh = st.DiemTrungBinh;
                    }
                    return item;
                })
                .ToList(),
            TongCauHoi = stats?.TongCauHoi ?? 0,
            TongLuotLamBai = stats?.TongLuotLamBai ?? 0,
            TongHocVien = stats?.TongHocVien ?? 0
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult Error() => View();
}