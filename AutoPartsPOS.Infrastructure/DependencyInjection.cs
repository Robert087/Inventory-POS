using AutoPartsPOS.Application.Common.Interfaces;
using AutoPartsPOS.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPartsPOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
