using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Controllers;
using QLThiTN.API.Data;
using QLThiTN.API.DTOs;

namespace QLThiTN.API.Services;

// Uses the same transaction, attempt lock and grading path as a normal submission.
public sealed class ExamExpiryWorker(IServiceScopeFactory scopes, ILogger<ExamExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<QLThiTNDbContext>();
                var now = DateTime.Now;
                var expired = await db.BaiLams.AsNoTracking()
                    .Where(b => b.TrangThai == "DangLam" && b.DotThi != null &&
                        (b.DotThi.ThoiGianDongCong <= now || b.ThoiGianBatDau.AddMinutes(
                            b.DotThi.ThoiLuongLamBai > 0 ? b.DotThi.ThoiLuongLamBai : 45) <= now))
                    .OrderBy(b => b.BaiLamID)
                    .Select(b => new { b.BaiLamID, b.HocVienID, b.DotThi!.DeThiID })
                    .ToListAsync(stoppingToken);
                foreach (var attempt in expired)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    try
                    {
                        using var attemptScope = scopes.CreateScope();
                        var controller = new DeThiController(attemptScope.ServiceProvider.GetRequiredService<QLThiTNDbContext>());
                        var result = await controller.SubmitAttemptAsync(attempt.DeThiID,
                            new SubmitExamRequestDto { BaiLamID = attempt.BaiLamID, HocVienID = attempt.HocVienID },
                            attempt.HocVienID, expiredOnly: true);
                        if (result is not Microsoft.AspNetCore.Mvc.OkObjectResult)
                            logger.LogWarning("Could not finalize expired attempt {Attempt}: {Result}", attempt.BaiLamID, result.GetType().Name);
                    }
                    catch (Exception ex) { logger.LogError(ex, "Failed to finalize attempt {Attempt}", attempt.BaiLamID); }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Expired exam sweep failed"); }
            try { if (!await timer.WaitForNextTickAsync(stoppingToken)) break; }
            catch (OperationCanceledException) { break; }
        }
    }
}
