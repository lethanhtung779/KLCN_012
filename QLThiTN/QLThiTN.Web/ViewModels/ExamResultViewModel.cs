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

    /// <summary>false = dot thi chua cho phep cong bo diem (CongBoDiemSom = 0
    /// va dot thi chua ket thuc) — hien trang "cho cong bo" thay vi diem chi tiet.</summary>
    public bool ScorePublished { get; set; } = true;

    public List<QuestionResultViewModel> QuestionResults { get; set; } = new();
}

public class QuestionResultViewModel
{
    public int OrderIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Loai { get; set; } = string.Empty;
    public List<ImageViewModel> Images { get; set; } = new();
    public List<OptionResultViewModel> Options { get; set; } = new();
    public int? SelectedOptionId { get; set; }
    public bool IsCorrect { get; set; }

    public List<YResultViewModel> YResults { get; set; } = new();
    public string? NoiDungTraLoi { get; set; }
    public string? DapAnTraLoiNgan { get; set; }
    public decimal DiemDat { get; set; }
    public decimal DiemToiDa { get; set; }
}

public class YResultViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    /// <summary>Dap an dung: true = Dung, false = Sai.</summary>
    public bool DapAnDung { get; set; }
    /// <summary>Thi sinh chon: null = bo trong.</summary>
    public bool? ClientLaDung { get; set; }
    public bool IsCorrect { get; set; }
    public List<ImageViewModel> Images { get; set; } = new();
}

public class OptionResultViewModel
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool IsSelected { get; set; }
    public List<ImageViewModel> Images { get; set; } = new();
}