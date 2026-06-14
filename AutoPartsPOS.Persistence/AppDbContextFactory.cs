using AutoPartsPOS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutoPartsPOS.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var databasePath = Path.Combine(AppContext.BaseDirectory, "Data", "Database.db");
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        var connectionString = $"Data Source={databasePath}";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString, sqlite =>
            {
                sqlite.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            })
            .Options;

        return new AppDbContext(options, new DesignTimeDateTimeProvider(), new DesignTimeCurrentUserService());
    }

    private sealed class DesignTimeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset Now => DateTimeOffset.UtcNow;
    }

    private sealed class DesignTimeCurrentUserService : ICurrentUserService
    {
        public string UserName => "DesignTime";
    }
}
