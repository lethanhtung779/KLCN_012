using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QLThiTN.API.Data;

namespace QLThiTN.API.Services;

public static class ExamTransaction
{
    // SQL Server transaction-owned locks also serialize requests across API instances.
    public static async Task<IDbContextTransaction> BeginAsync(QLThiTNDbContext db, string resource)
    {
        var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive',
                    @LockOwner='Transaction', @LockTimeout=10000;
                IF @result < 0 THROW 51000, 'Cannot acquire exam transaction lock.', 1;
                """);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }
}
