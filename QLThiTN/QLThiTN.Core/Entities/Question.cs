namespace QLThiTN.Core.Entities;

public class Question
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public Enums.QuestionType Type { get; set; }
    public int? SubjectId { get; set; }
    public int? TopicId { get; set; }
    public Enums.DifficultyLevel Difficulty { get; set; }
    public int? CreatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}