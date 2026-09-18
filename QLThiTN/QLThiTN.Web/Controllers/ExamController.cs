using Microsoft.AspNetCore.Mvc;
using QLThiTN.Web.ViewModels;

namespace QLThiTN.Web.Controllers;

public class ExamController : Controller
{
    [HttpGet]
    public IActionResult Schedule()
        => View(MockData.Exams);

    [HttpGet]
    public IActionResult Detail(int id)
    {
        var exam = MockData.Exams.FirstOrDefault(e => e.Id == id);
        if (exam == null)
            return NotFound();

        var statusText = exam.StartTime > DateTime.Now
            ? "Sắp diễn ra"
            : exam.EndTime < DateTime.Now
                ? "Đã kết thúc"
                : "Đang mở — đăng ký ngay";

        var model = new ExamDetailViewModel
        {
            Id = exam.Id,
            Title = exam.Title,
            Description = exam.Description,
            Duration = exam.Duration,
            StartTime = exam.StartTime,
            EndTime = exam.EndTime,
            MaxStudents = exam.MaxStudents,
            SubjectName = exam.SubjectName,
            QuestionCount = exam.QuestionCount,
            HasRegistered = exam.HasRegistered,
            CanStart = exam.StartTime <= DateTime.Now && exam.EndTime >= DateTime.Now,
            StatusText = statusText
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult Taking(int id)
    {
        var exam = MockData.Exams.FirstOrDefault(e => e.Id == id);
        if (exam == null)
            return NotFound();

        var model = new TakingExamViewModel
        {
            ExamId = exam.Id,
            ExamTitle = exam.Title,
            Duration = exam.Duration,
            StartTime = DateTime.Now,
            Questions = MockData.GetQuestionsForExam(exam.Id)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Submit(SubmitExamViewModel model)
    {
        // TODO: Gọi API /api/results để chấm điểm và lưu kết quả
        var questions = MockData.GetQuestionsForExam(model.ExamId);
        var result = MockData.BuildResultFromQuestions(model.ExamId, questions);
        return View("Result", result);
    }

    [HttpGet]
    public IActionResult Result(int id)
    {
        var questions = MockData.GetQuestionsForExam(id);
        var result = MockData.BuildResultFromQuestions(id, questions);
        return View(result);
    }

    [HttpGet]
    public IActionResult History()
    {
        var model = new ExamHistoryViewModel
        {
            Items = MockData.CompletedExams
                .OrderByDescending(r => r.EndTime)
                .Select(r =>
                {
                    var exam = MockData.Exams.FirstOrDefault(e => e.Id == r.ExamId);
                    return new ExamHistoryItemViewModel
                    {
                        ExamId = r.ExamId,
                        ExamTitle = r.ExamTitle,
                        SubjectName = exam?.SubjectName ?? "—",
                        ExamKindText = exam?.ExamKind.ToDisplayName() ?? "Kỳ thi",
                        Score = r.Score,
                        StartTime = r.StartTime,
                        EndTime = r.EndTime,
                        StatusText = "Hoàn thành",
                        Duration = r.DurationUsed,
                        ScorePublished = exam?.ScorePublished ?? true
                    };
                }).ToList()
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult LearningSchedule()
    {
        var model = new LearningScheduleViewModel
        {
            Items = MockData.GetLearningSchedule()
        };
        return View(model);
    }

    private static string SubjectOf(int examId)
        => MockData.Exams.FirstOrDefault(e => e.Id == examId)?.SubjectName ?? "—";
}