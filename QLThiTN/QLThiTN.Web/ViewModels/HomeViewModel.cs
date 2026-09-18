namespace QLThiTN.Web.ViewModels;

public class HomeViewModel
{
    public int TotalExams { get; set; }
    public int UpcomingExams { get; set; }
    public int CompletedExams { get; set; }
    public decimal AverageScore { get; set; }
    public List<ExamScheduleViewModel> LatestExams { get; set; } = new();
    public List<ExamScheduleViewModel> LatestPublicExams { get; set; } = new();
}