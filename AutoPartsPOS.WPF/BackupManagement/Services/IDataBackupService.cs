using AutoPartsPOS.WPF.Backups.Models;

namespace AutoPartsPOS.WPF.Backups.Services;

public interface IDataBackupService
{
    string BackupsDirectory { get; }

    Task EnsureDailyBackupAsync(CancellationToken cancellationToken = default);

    Task<BackupFileInfo> CreateBackupAsync(bool automatic = false, CancellationToken cancellationToken = default);

    IReadOnlyList<BackupFileInfo> GetBackups();

    Task ExportBackupAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default);

    Task RestoreBackupAsync(string sourcePath, CancellationToken cancellationToken = default);

    void DeleteBackup(string filePath);
}
