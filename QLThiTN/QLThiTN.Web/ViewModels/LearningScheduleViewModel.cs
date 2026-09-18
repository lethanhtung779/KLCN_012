namespace QLThiTN.Web.ViewModels;

public class LearningScheduleViewModel
{
    public List<LearningItemViewModel> Items { get; set; } = new();
}

public class LearningItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueTime { get; set; }
    public bool IsDone { get; set; }
    public int? ExamId { get; set; }
    public string? StatusText { get; set; }
}