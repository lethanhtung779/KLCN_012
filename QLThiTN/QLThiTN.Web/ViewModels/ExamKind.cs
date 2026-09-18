namespace QLThiTN.Web.ViewModels;

public enum ExamKind
{
    Homework = 0,
    QuickQuiz = 1,
    MonthlyMock = 2,
    PublicMock = 3
}

public static class ExamKindExtensions
{
    public static string ToDisplayName(this ExamKind kind) => kind switch
    {
        ExamKind.Homework => "Bài tập về nhà",
        ExamKind.QuickQuiz => "Kiểm tra 15 phút",
        ExamKind.MonthlyMock => "Thi thử cuối tháng",
        ExamKind.PublicMock => "Thi thử THPT miễn phí",
        _ => "Kỳ thi"
    };
}

public static class ExamKindIcon
{
    public static string ToIcon(this ExamKind kind) => kind switch
    {
        ExamKind.Homework => "bi-journal-text",
        ExamKind.QuickQuiz => "bi-stopwatch",
        ExamKind.MonthlyMock => "bi-calendar2-week",
        ExamKind.PublicMock => "bi-megaphone",
        _ => "bi-ui-checks"
    };
}