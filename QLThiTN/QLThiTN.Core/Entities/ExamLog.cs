namespace QLThiTN.Core.Entities;

public class ExamLog
{
    public int Id { get; set; }
    public int? StudentId { get; set; }
    public int? ExamId { get; set; }
    public string? Action { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string? IPAddress { get; set; }
}