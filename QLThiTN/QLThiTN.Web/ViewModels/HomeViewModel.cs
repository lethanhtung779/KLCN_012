namespace QLThiTN.Web.ViewModels;

public class HomeViewModel
{
    public int TotalExams { get; set; }
    public int UpcomingExams { get; set; }
    public int CompletedExams { get; set; }
    public decimal AverageScore { get; set; }
    public List<ExamScheduleViewModel> LatestExams { get; set; } = new();

    /// <summary>Cac de thi thu cong khai voi bia tu sinh + thong ke.</summary>
    public List<KhoDeExamItem> LatestPublicExams { get; set; } = new();

    // Social-proof tong hop (GET api/dethi/stats)
    public int TongCauHoi { get; set; }
    public int TongLuotLamBai { get; set; }
    public int TongHocVien { get; set; }
}