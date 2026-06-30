namespace AutoPartsPOS.WPF.Backups.Models;

public sealed record BackupFileInfo(
    string FilePath,
    string FileName,
    DateTime CreatedAt,
    long SizeBytes,
    bool IsAutomatic)
{
    public string TypeDisplay => IsAutomatic ? "تلقائية" : "يدوية";

    public string CreatedAtDisplay => CreatedAt.ToString("dd/MM/yyyy - hh:mm tt");

    public string SizeDisplay => SizeBytes switch
    {
        >= 1024L * 1024L => $"{SizeBytes / (1024d * 1024d):N2} MB",
        >= 1024L => $"{SizeBytes / 1024d:N1} KB",
        _ => $"{SizeBytes:N0} B"
    };
}
