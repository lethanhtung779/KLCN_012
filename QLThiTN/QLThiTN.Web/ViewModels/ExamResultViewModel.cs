namespace QLThiTN.Web.ViewModels;

public class ExamResultViewModel
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationUsed { get; set; }
    public List<QuestionResultViewModel> QuestionResults { get; set; } = new();
}

public class QuestionResultViewModel
{
    public int OrderIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<OptionResultViewModel> Options { get; set; } = new();
    public int? SelectedOptionId { get; set; }
    public bool IsCorrect { get; set; }
}

public class OptionResultViewModel
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool IsSelected { get; set; }
}