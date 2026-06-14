using AutoPartsPOS.Domain.Common;

namespace AutoPartsPOS.Domain.Settings;

public sealed class AppSetting : AuditableEntity
{
    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    public string? Description { get; set; }

    public bool IsSystem { get; set; }
}
