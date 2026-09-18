namespace QLThiTN.Core.Entities;

public class ExamResult
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public int StudentId { get; set; }
    public decimal Score { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public Enums.ExamStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}