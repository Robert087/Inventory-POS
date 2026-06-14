using AutoPartsPOS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AutoPartsPOS.Persistence.Repositories;

public sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
}
