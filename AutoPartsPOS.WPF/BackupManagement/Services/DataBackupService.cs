using AutoPartsPOS.WPF.Backups.Models;
using Microsoft.Data.Sqlite;
using System.IO;

namespace AutoPartsPOS.WPF.Backups.Services;

public sealed class DataBackupService : IDataBackupService
{
    private readonly string _databasePath = Path.Combine(AppContext.BaseDirectory, "Data", "Database.db");

    public DataBackupService()
    {
        BackupsDirectory = Path.Combine(AppContext.BaseDirectory, "Backups");
        Directory.CreateDirectory(BackupsDirectory);
    }

    public string BackupsDirectory { get; }

    public async Task EnsureDailyBackupAsync(CancellationToken cancellationToken = default)
    {
        var dailyPath = Path.Combine(BackupsDirectory, $"TaisonSystem-Auto-{DateTime.Today:yyyy-MM-dd}.db");
        if (!File.Exists(dailyPath))
        {
            await CreateDatabaseCopyAsync(_databasePath, dailyPath, cancellationToken);
        }
    }

    public async Task<BackupFileInfo> CreateBackupAsync(bool automatic = false, CancellationToken cancellationToken = default)
    {
        var prefix = automatic ? "Auto" : "Manual";
        var filePath = Path.Combine(BackupsDirectory, $"TaisonSystem-{prefix}-{DateTime.Now:yyyy-MM-dd-HHmmss-fff}.db");
        await CreateDatabaseCopyAsync(_databasePath, filePath, cancellationToken);
        return ToBackupInfo(new FileInfo(filePath));
    }

    public IReadOnlyList<BackupFileInfo> GetBackups()
    {
        Directory.CreateDirectory(BackupsDirectory);
        return new DirectoryInfo(BackupsDirectory)
            .EnumerateFiles("*.db", SearchOption.TopDirectoryOnly)
            .OrderByDescending(file => file.LastWriteTime)
            .Select(ToBackupInfo)
            .ToList();
    }

    public async Task ExportBackupAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
    {
        ValidateBackupPath(sourcePath, requireManagedBackup: true);
        await CreateDatabaseCopyAsync(sourcePath, destinationPath, cancellationToken);
    }

    public async Task RestoreBackupAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ValidateBackupPath(sourcePath, requireManagedBackup: false);
        await ValidateDatabaseAsync(sourcePath, cancellationToken);

        await CreateBackupAsync(cancellationToken: cancellationToken);
        SqliteConnection.ClearAllPools();
        await CreateDatabaseCopyAsync(sourcePath, _databasePath, cancellationToken);
        SqliteConnection.ClearAllPools();
    }

    public void DeleteBackup(string filePath)
    {
        ValidateBackupPath(filePath, requireManagedBackup: true);
        File.Delete(filePath);
    }

    private static async Task CreateDatabaseCopyAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        var temporaryPath = destinationPath + ".tmp";
        try
        {
            File.Delete(temporaryPath);
            await using (var source = new SqliteConnection($"Data Source={sourcePath};Mode=ReadOnly;Pooling=False"))
            await using (var destination = new SqliteConnection($"Data Source={temporaryPath};Pooling=False"))
            {
                await source.OpenAsync(cancellationToken);
                await destination.OpenAsync(cancellationToken);
                source.BackupDatabase(destination);
            }

            File.Move(temporaryPath, destinationPath, true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private static async Task ValidateDatabaseAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly;Pooling=False");
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check;";
        var result = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken));
        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("ملف النسخة الاحتياطية تالف أو ليس قاعدة بيانات صالحة.");
        }

        command.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsHistory';";
        if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 0)
        {
            throw new InvalidDataException("الملف المحدد لا ينتمي إلى نظام إدارة المتجر.");
        }
    }

    private void ValidateBackupPath(string filePath, bool requireManagedBackup)
    {
        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath) || !string.Equals(Path.GetExtension(fullPath), ".db", StringComparison.OrdinalIgnoreCase))
        {
            throw new FileNotFoundException("ملف النسخة الاحتياطية غير موجود أو غير صالح.", fullPath);
        }

        if (requireManagedBackup && !Path.GetDirectoryName(fullPath)!.Equals(BackupsDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("يمكن تنفيذ هذا الإجراء على النسخ الموجودة داخل مجلد البرنامج فقط.");
        }
    }

    private static BackupFileInfo ToBackupInfo(FileInfo file) => new(
        file.FullName,
        file.Name,
        file.LastWriteTime,
        file.Length,
        file.Name.Contains("-Auto-", StringComparison.OrdinalIgnoreCase));
}
