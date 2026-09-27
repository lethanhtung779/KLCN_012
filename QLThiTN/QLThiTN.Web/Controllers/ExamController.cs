using Microsoft.AspNetCore.Mvc;
using QLThiTN.Web.Services;
using QLThiTN.Web.ViewModels;

namespace QLThiTN.Web.Controllers;

public class ExamController : Controller
{
    private readonly ApiClient _api;

    public ExamController(ApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Schedule()
    {
        var exams = await _api.GetDeThisAsync();
        return View(exams.Select(ExamMapping.ToSchedule).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var exam = await _api.GetDeThiAsync(id);
        if (exam == null)
            return NotFound();

        var statusText = exam.ThoiGianMoCong > DateTime.Now
            ? "Sắp diễn ra"
            : exam.ThoiGianDongCong < DateTime.Now
                ? "Đã kết thúc"
                : "Đang mở — đăng ký ngay";

        var model = new ExamDetailViewModel
        {
            Id = exam.DeThiID,
            Title = exam.TenDe,
            Description = string.IsNullOrWhiteSpace(exam.TenDotThi) ? exam.LoaiDeText : exam.TenDotThi,
            Duration = exam.ThoiLuongLamBai ?? 45,
            StartTime = exam.ThoiGianMoCong,
            EndTime = exam.ThoiGianDongCong,
            MaxStudents = null,
            SubjectName = exam.TenMon,
            QuestionCount = exam.SoLuongCauHoi,
            HasRegistered = ExamMapping.ToExamKind(exam.LoaiDe) != ExamKind.PublicMock,
            CanStart = exam.ThoiGianMoCong <= DateTime.Now && exam.ThoiGianDongCong >= DateTime.Now,
            StatusText = statusText
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Taking(int id)
    {
        var exam = await _api.GetDeThiAsync(id);
        if (exam == null)
            return NotFound();

        var questions = await _api.GetCauHoiAsync(id);
        var model = new TakingExamViewModel
        {
            ExamId = exam.DeThiID,
            ExamTitle = exam.TenDe,
            Duration = exam.ThoiLuongLamBai ?? 45,
            StartTime = DateTime.Now,
            Questions = ExamMapping.ToQuestions(questions, _api.BaseUrl)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmitExamViewModel model)
    {
        if (model.Answers == null || model.Answers.Count == 0)
        {
            TempData["ErrorMessage"] = "Bạn chưa trả lời câu hỏi nào.";
            return RedirectToAction(nameof(Taking), new { id = model.ExamId });
        }

        var request = new ApiNopBaiRequest
        {
            DeThiID = model.ExamId,
            Answers = (model.Answers ?? new List<AnswerViewModel>())
                .Select(a => new ApiCauTraLoi
                {
                    CauHoiID = a.QuestionId,
                    DapAnID = a.SelectedOptionId
                }).ToList()
        };

        var result = await _api.NopBaiAsync(request);

        var exam = await _api.GetDeThiAsync(model.ExamId);
        var vm = ExamMapping.ToResult(
            result,
            exam?.TenDe ?? $"Kỳ thi #{model.ExamId}",
            exam?.ThoiLuongLamBai ?? 45,
            DateTime.Now,
            _api.BaseUrl);

        return View("Result", vm);
    }

    [HttpGet]
    public IActionResult Result(int id)
    {
        // TODO: Gọi API /api/results/{id} để tải chi tiết kết quả đã lưu
        return RedirectToAction(nameof(History));
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        var exams = await _api.GetDeThisAsync();
        var model = new ExamHistoryViewModel
        {
            Items = exams
                .Where(e => e.TrangThai == "DaDong")
                .Select(e =>
                {
                    var kind = ExamMapping.ToExamKind(e.LoaiDe);
                    return new ExamHistoryItemViewModel
                    {
                        ExamId = e.DeThiID,
                        ExamTitle = e.TenDe,
                        SubjectName = e.TenMon,
                        ExamKindText = kind.ToDisplayName(),
                        Score = 0m,
                        EndTime = e.ThoiGianDongCong,
                        StatusText = "Đã kết thúc",
                        Duration = e.ThoiLuongLamBai ?? 45,
                        ScorePublished = false
                    };
                }).ToList()
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> LearningSchedule()
    {
        var exams = await _api.GetDeThisAsync();
        var model = new LearningScheduleViewModel
        {
            Items = exams
                .OrderBy(e => e.ThoiGianMoCong)
                .Select((e, i) => new LearningItemViewModel
                {
                    Id = i + 1,
                    Title = e.TenDe,
                    SubjectName = e.TenMon,
                    Type = ExamMapping.ToExamKind(e.LoaiDe).ToDisplayName(),
                    Description = e.LoaiDeText,
                    DueTime = e.ThoiGianDongCong,
                    ExamId = e.DeThiID,
                    IsDone = e.TrangThai == "DaDong",
                    StatusText = e.TrangThai switch
                    {
                        "DangMo" => "Đang mở",
                        "SapMo" => "Sắp đến hạn",
                        _ => "Đã hoàn thành"
                    }
                }).ToList()
        };
        return View(model);
    }
}