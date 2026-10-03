using Microsoft.AspNetCore.Mvc;
using QLThiTN.Web.Filters;
using QLThiTN.Web.Services;
using QLThiTN.Web.ViewModels;

namespace QLThiTN.Web.Controllers;

public class ExamController : Controller
{
    private readonly ApiClient _api;

    public ExamController(ApiClient api) => _api = api;

    private int? HocVienId => HttpContext.Session.GetInt32("HocVienId");

    [HttpGet]
    public async Task<IActionResult> Schedule()
    {
        var exams = await _api.GetDeThisAsync();
        var dotThis = await _api.GetDotThisAsync(HocVienId);
        var dotByDeThi = dotThis
            .GroupBy(d => d.DeThiID)
            .ToDictionary(g => g.Key, g => g.First());

        return View(exams.Select(e =>
        {
            var vm = ExamMapping.ToSchedule(e);
            if (dotByDeThi.TryGetValue(e.DeThiID, out var dot))
            {
                vm.DotThiID = dot.DotThiID;
                vm.SoDangKy = dot.SoDangKy;
                vm.SoChoConLai = dot.SoChoConLai;
                vm.HasRegistered = dot.DaDangKy;
                vm.CanRegister = dot.TrangThai != "DaDong" && !dot.DaDangKy
                    && (!dot.SoChoConLai.HasValue || dot.SoChoConLai.Value > 0);
            }
            return vm;
        }).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var exam = await _api.GetDeThiAsync(id);
        if (exam == null)
            return NotFound();

        var now = DateTime.Now;
        var statusText = exam.ThoiGianMoCong > now
            ? "Sắp diễn ra"
            : exam.ThoiGianDongCong < now
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
            HasRegistered = false,
            CanStart = exam.ThoiGianMoCong <= now && exam.ThoiGianDongCong >= now,
            StatusText = statusText
        };

        // Thong tin dot thi: trang thai dang ky, so cho con lai
        if (HocVienId.HasValue)
        {
            var dot = (await _api.GetDotThisAsync(HocVienId))
                .FirstOrDefault(d => d.DeThiID == id);
            if (dot != null)
            {
                model.DotThiID = dot.DotThiID;
                model.SoDangKy = dot.SoDangKy;
                model.SoChoConLai = dot.SoChoConLai;
                model.HasRegistered = dot.DaDangKy;
                model.CanRegister = dot.TrangThai != "DaDong" && dot.ThoiGianDongCong >= now
                    && (!dot.SoChoConLai.HasValue || dot.SoChoConLai.Value > 0);
            }
        }

        return View(model);
    }

    [HttpPost]
    [RequireLogin]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangKy(int id, int deThiId)
    {
        if (HocVienId is not int hocVienId)
            return RedirectToAction("Login", "Account");

        var (success, message) = await _api.DangKyDotThiAsync(id, hocVienId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;

        if (deThiId > 0)
            return RedirectToAction(nameof(Detail), new { id = deThiId });

        // Khong biet DeThiID -> tra ve lich thi
        return RedirectToAction(nameof(Schedule));
    }

    [HttpPost]
    [RequireLogin]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HuyDangKy(int id, int deThiId)
    {
        if (HocVienId is not int hocVienId)
            return RedirectToAction("Login", "Account");

        var (success, message) = await _api.HuyDangKyDotThiAsync(id, hocVienId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;

        if (deThiId > 0)
            return RedirectToAction(nameof(Detail), new { id = deThiId });

        return RedirectToAction(nameof(Schedule));
    }

    [HttpGet]
    [RequireLogin]
    public async Task<IActionResult> Taking(int id)
    {
        if (HocVienId is not int hocVienId)
            return RedirectToAction("Login", "Account");

        var exam = await _api.GetDeThiAsync(id);
        if (exam == null)
            return NotFound();

        // Bat dau (hoac phuc hoi) bai thi: server kiem tra thoi gian/so lan thi/
        // dang ky va tra ve so giay con lai tinh theo dong ho server.
        var (batDau, error) = await _api.BatDauAsync(id, hocVienId);
        if (batDau == null)
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction(nameof(Detail), new { id });
        }

        var questions = await _api.GetCauHoiAsync(id);
        var model = new TakingExamViewModel
        {
            ExamId = exam.DeThiID,
            ExamTitle = exam.TenDe,
            Duration = batDau.ThoiLuongLamBai,
            StartTime = DateTime.Now,
            BaiLamID = batDau.BaiLamID,
            RemainingSeconds = batDau.RemainingSeconds,
            IsResume = batDau.IsResume,
            SavedAnswers = batDau.SavedAnswers,
            Questions = ExamMapping.ToQuestions(questions, _api.BaseUrl)
        };
        return View(model);
    }

    /// <summary>Autosave: web goi dinh ky (JSON) de luu tien trinh lam bai ve server.</summary>
    [HttpPost]
    [RequireLogin]
    public async Task<IActionResult> LuuTam([FromBody] SubmitExamViewModel model)
    {
        if (HocVienId is not int hocVienId || model.BaiLamID <= 0)
            return Json(new { success = false, message = "Phiên đăng nhập không hợp lệ." });

        var (success, error) = await _api.LuuTamAsync(model.ExamId, new ApiLuuTamRequest
        {
            BaiLamID = model.BaiLamID,
            HocVienID = hocVienId,
            Answers = (model.Answers ?? new List<AnswerViewModel>())
                .Select(a => new ApiCauTraLoi
                {
                    CauHoiID = a.QuestionId,
                    DapAnID = a.SelectedOptionId,
                    NoiDungTraLoi = a.NoiDungTraLoi,
                    YChoices = (a.YChoices ?? new List<YChoiceViewModel>())
                        .Where(y => y.DapAnID > 0 && y.LaDung != null)
                        .Select(y => new ApiYChon { DapAnID = y.DapAnID, LaDung = y.LaDung })
                        .ToList()
                }).ToList()
        });

        return Json(new
        {
            success,
            message = error,
            luuLuc = success ? DateTime.Now.ToString("HH:mm:ss") : null
        });
    }

    [HttpPost]
    [RequireLogin]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmitExamViewModel model)
    {
        if (model.Answers == null || model.Answers.Count == 0)
        {
            TempData["ErrorMessage"] = "Bạn chưa trả lời câu hỏi nào.";
            return RedirectToAction(nameof(Taking), new { id = model.ExamId });
        }

        if (HocVienId is not int hocVienId)
            return RedirectToAction("Login", "Account");

        var request = new ApiNopBaiRequest
        {
            DeThiID = model.ExamId,
            HocVienID = hocVienId,
            BaiLamID = model.BaiLamID > 0 ? model.BaiLamID : null,
            HetGio = model.HetGio,
            ThoiGianBatDau = model.StartTime > DateTime.MinValue ? model.StartTime : null,
            Answers = (model.Answers ?? new List<AnswerViewModel>())
                .Select(a => new ApiCauTraLoi
                {
                    CauHoiID = a.QuestionId,
                    DapAnID = a.SelectedOptionId,
                    NoiDungTraLoi = a.NoiDungTraLoi,
                    YChoices = (a.YChoices ?? new List<YChoiceViewModel>())
                        .Where(y => y.DapAnID > 0 && y.LaDung != null)
                        .Select(y => new ApiYChon
                        {
                            DapAnID = y.DapAnID,
                            LaDung = y.LaDung
                        }).ToList()
                }).ToList()
        };

        var (result, error) = await _api.NopBaiSafeAsync(request);

        if (result != null)
        {
            if (result.BaiLamID.HasValue && result.BaiLamID.Value > 0)
                return RedirectToAction(nameof(Result), new { id = result.BaiLamID.Value });

            var exam = await _api.GetDeThiAsync(model.ExamId);
            var vm = ExamMapping.ToResult(
                result,
                exam?.TenDe ?? $"Kỳ thi #{model.ExamId}",
                exam?.ThoiLuongLamBai ?? 45,
                DateTime.Now,
                _api.BaseUrl);
            return View("Result", vm);
        }

        // Nop loi (het han, da nop roi...): dua ve trang de thi kem thong bao
        TempData["ErrorMessage"] = error;
        return RedirectToAction(nameof(Detail), new { id = model.ExamId });
    }

    [HttpGet]
    [RequireLogin]
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
        vm.ScorePublished = baiLam.ScorePublished;

        return View(vm);
    }

    [HttpGet]
    [RequireLogin]
    public async Task<IActionResult> History()
    {
        var hocVienId = HocVienId;
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
    [RequireLogin]
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
