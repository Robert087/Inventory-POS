using AutoPartsPOS.Domain.Settings;

namespace AutoPartsPOS.Application.Common.Interfaces;

public interface IAppDbContext
{
    IQueryable<AppSetting> AppSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
