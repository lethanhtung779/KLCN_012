namespace QLThiTN.Web.ViewModels;

public class TakingExamViewModel
{
    public int ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public int Duration { get; set; }
    public DateTime StartTime { get; set; }
    public List<QuestionViewModel> Questions { get; set; } = new();
}

public class QuestionViewModel
{
    public int Id { get; set; }
    public int OrderIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    /// <summary>LoaiCauHoi goc: TracNghiem | DungSai | TraLoiNgan.</summary>
    public string Loai { get; set; } = string.Empty;
    public List<ImageViewModel> Images { get; set; } = new();
    public List<OptionViewModel> Options { get; set; } = new();
}

public class ImageViewModel
{
    public string Url { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
}

public class OptionViewModel
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public List<ImageViewModel> Images { get; set; } = new();
}

public class SubmitExamViewModel
{
    public int ExamId { get; set; }
    public List<AnswerViewModel> Answers { get; set; } = new();
}

public class AnswerViewModel
{
    public int QuestionId { get; set; }
    public int? SelectedOptionId { get; set; }
    public string? NoiDungTraLoi { get; set; }
    public List<YChoiceViewModel> YChoices { get; set; } = new();
}

/// <summary>Mot lua chon Dung/Sai cho mot y cua cau DungSai.</summary>
public class YChoiceViewModel
{
    public int DapAnID { get; set; }
    public bool? LaDung { get; set; }
}