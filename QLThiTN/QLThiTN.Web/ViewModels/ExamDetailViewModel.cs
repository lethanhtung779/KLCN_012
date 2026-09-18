namespace QLThiTN.Web.ViewModels;

public class ExamDetailViewModel
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
    public bool CanStart { get; set; }
    public string StatusText { get; set; } = string.Empty;
}