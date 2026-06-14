using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsPOS.Persistence.Repositories;

public class Repository<TEntity>(AppDbContext dbContext) : IRepository<TEntity>
    where TEntity : Entity
{
    public IQueryable<TEntity> Query()
    {
        return dbContext.Set<TEntity>().AsQueryable();
    }

    public Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<TEntity>().FindAsync([id], cancellationToken).AsTask();
    }

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        return dbContext.Set<TEntity>().AddAsync(entity, cancellationToken).AsTask();
    }

    public void Update(TEntity entity)
    {
        dbContext.Set<TEntity>().Update(entity);
    }
}
