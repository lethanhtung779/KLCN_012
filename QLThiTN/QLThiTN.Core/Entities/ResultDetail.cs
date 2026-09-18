namespace QLThiTN.Core.Entities;

public class ResultDetail
{
    public int Id { get; set; }
    public int ResultId { get; set; }
    public int QuestionId { get; set; }
    public int? SelectedOptionId { get; set; }
    public bool IsCorrect { get; set; }
}