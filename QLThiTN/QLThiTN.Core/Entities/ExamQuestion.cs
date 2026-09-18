namespace QLThiTN.Core.Entities;

public class ExamQuestion
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public int QuestionId { get; set; }
    public int OrderIndex { get; set; }
}