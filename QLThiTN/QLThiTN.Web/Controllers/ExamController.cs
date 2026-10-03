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

        var hocVienId = HttpContext.Session.GetInt32("HocVienId");

        var request = new ApiNopBaiRequest
        {
            DeThiID = model.ExamId,
            HocVienID = hocVienId,
            ThoiGianBatDau = model.StartTime > DateTime.MinValue ? model.StartTime : DateTime.Now.AddMinutes(-45),
            Answers = (model.Answers ?? new List<AnswerViewModel>())
                .Select(a => new ApiCauTraLoi
                {
                    CauHoiID = a.QuestionId,
                    DapAnID = a.SelectedOptionId,
                    NoiDungTraLoi = a.NoiDungTraLoi,
                    YChoices = (a.YChoices ?? new List<YChoiceViewModel>())
                        .Where(y => y.DapAnID > 0)
                        .Select(y => new ApiYChon
                        {
                            DapAnID = y.DapAnID,
                            LaDung = y.LaDung
                        }).ToList()
                }).ToList()
        };

        var result = await _api.NopBaiAsync(request);

        if (result.BaiLamID.HasValue && result.BaiLamID.Value > 0)
        {
            return RedirectToAction(nameof(Result), new { id = result.BaiLamID.Value });
        }

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
    public async Task<IActionResult> Result(int id)
    {
        var baiLam = await _api.GetBaiLamDetailAsync(id);
        if (baiLam == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy kết quả bài làm.";
            return RedirectToAction(nameof(History));
        }

        var vm = ExamMapping.ToResult(
            baiLam,
            string.IsNullOrWhiteSpace(baiLam.TenDe) ? $"Kỳ thi #{baiLam.DeThiID}" : baiLam.TenDe,
            baiLam.DurationUsed > 0 ? baiLam.DurationUsed : 45,
            baiLam.ThoiGianBatDau,
            _api.BaseUrl);

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        var hocVienId = HttpContext.Session.GetInt32("HocVienId");
        if (!hocVienId.HasValue)
        {
            ViewBag.RequireLogin = true;
            return View(new ExamHistoryViewModel());
        }

        var historyItems = await _api.GetHistoryAsync(hocVienId.Value);
        var model = new ExamHistoryViewModel
        {
            Items = historyItems.Select(h => new ExamHistoryItemViewModel
            {
                ExamId = h.BaiLamID,
                BaiLamId = h.BaiLamID,
                DeThiId = h.DeThiID,
                ExamTitle = h.TenDe,
                SubjectName = h.TenMon,
                ExamKindText = h.LoaiDeText,
                Score = h.TongDiem,
                EndTime = h.ThoiGianNop,
                StatusText = $"Lần thi {h.LanThi}",
                Duration = h.ThoiLuongLamBai,
                ScorePublished = h.ScorePublished
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