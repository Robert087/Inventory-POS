using AutoPartsPOS.Application.Common.Interfaces;

namespace AutoPartsPOS.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    public string UserName => Environment.UserName;
}
