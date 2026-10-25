using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Data;
using QLThiTN.API.Services;

namespace QLThiTN.API.Controllers;

[ApiController]
[Route("api/examactivity")]
public sealed class ExamActivityController(QLThiTNDbContext db) : ControllerBase
{
    public const string SchemaSql = """
        IF OBJECT_ID(N'dbo.ExamActivity', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ExamActivity (
                EventID uniqueidentifier NOT NULL PRIMARY KEY,
                BaiLamID int NOT NULL REFERENCES BaiLam(BaiLamID),
                EventType varchar(20) NOT NULL,
                ReceivedAt datetime2 NOT NULL DEFAULT SYSDATETIME()
            );
            CREATE INDEX IX_ExamActivity_Attempt ON dbo.ExamActivity(BaiLamID, ReceivedAt);
        END
        """;

    public sealed record Activity(int BaiLamID, Guid EventID, string EventType);

    [Authorize(Roles = "HocVien")]
    [HttpPost]
    public async Task<IActionResult> Record(Activity activity)
    {
        if (activity.EventID == Guid.Empty || activity.EventType is not ("hidden" or "visible" or "leave"))
            return BadRequest();
        await using var transaction = await ExamTransaction.BeginAsync(db, $"attempt:{activity.BaiLamID}");
        var attempt = await db.BaiLams.Include(b => b.DotThi)
            .SingleOrDefaultAsync(b => b.BaiLamID == activity.BaiLamID && b.HocVienID == User.StudentId());
        if (attempt is null) return NotFound();
        if (attempt.TrangThai != "DangLam" || attempt.DotThi is null || DateTime.Now >= ExamRules.Deadline(attempt, attempt.DotThi))
            return Conflict();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            IF NOT EXISTS (SELECT 1 FROM dbo.ExamActivity WHERE EventID = {activity.EventID})
               AND (SELECT COUNT(*) FROM dbo.ExamActivity WHERE BaiLamID = {activity.BaiLamID}) < 1000
                INSERT INTO dbo.ExamActivity(EventID, BaiLamID, EventType)
                VALUES ({activity.EventID}, {activity.BaiLamID}, {activity.EventType});
            """);
        await transaction.CommitAsync();
        return Ok(new { success = true });
    }
}
