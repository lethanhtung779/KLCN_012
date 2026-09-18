namespace QLThiTN.Core.Entities;

public class Option
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }
}