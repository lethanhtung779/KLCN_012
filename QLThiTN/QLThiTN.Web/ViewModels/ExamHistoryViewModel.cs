namespace QLThiTN.Web.ViewModels;

public class ExamHistoryViewModel
{
    public List<ExamHistoryItemViewModel> Items { get; set; } = new();
}

public class ExamHistoryItemViewModel
{
    public int ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string ExamKindText { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public int Duration { get; set; }
    public bool ScorePublished { get; set; } = true;
}