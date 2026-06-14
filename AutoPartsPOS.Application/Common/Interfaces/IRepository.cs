using AutoPartsPOS.Domain.Common;

namespace AutoPartsPOS.Application.Common.Interfaces;

public interface IRepository<TEntity>
    where TEntity : Entity
{
    IQueryable<TEntity> Query();

    Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);
}
