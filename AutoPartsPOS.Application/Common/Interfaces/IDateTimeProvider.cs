namespace AutoPartsPOS.Application.Common.Interfaces;

public interface IDateTimeProvider
{
    DateTimeOffset Now { get; }
}
