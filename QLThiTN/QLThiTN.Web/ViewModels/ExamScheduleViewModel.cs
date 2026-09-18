namespace QLThiTN.Web.ViewModels;

public class ExamScheduleViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Duration { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? MaxStudents { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public bool HasRegistered { get; set; }
    public bool CanRegister { get; set; }
    public ExamKind ExamKind { get; set; } = ExamKind.MonthlyMock;
    public bool IsPublic => ExamKind == ExamKind.PublicMock;
    public bool IsAssignedToMe { get; set; }
    public bool ScorePublished { get; set; } = true;
}