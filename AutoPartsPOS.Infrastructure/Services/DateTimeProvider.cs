using AutoPartsPOS.Application.Common.Interfaces;

namespace AutoPartsPOS.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}
