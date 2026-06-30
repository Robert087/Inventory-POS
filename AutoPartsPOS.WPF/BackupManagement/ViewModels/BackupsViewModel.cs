using AutoPartsPOS.Application.Common.ViewModels;
using AutoPartsPOS.WPF.Backups.Models;
using AutoPartsPOS.WPF.Backups.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace AutoPartsPOS.WPF.Backups.ViewModels;

public sealed partial class BackupsViewModel(IDataBackupService backupService) : ViewModelBase
{
    public ObservableCollection<BackupFileInfo> Backups { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestoreCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private BackupFileInfo? _selectedBackup;

    [ObservableProperty]
    private string? _successMessage;

    public string BackupsDirectory => backupService.BackupsDirectory;

    public bool HasBackups => Backups.Count > 0;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Title = "النسخ الاحتياطية";
        await backupService.EnsureDailyBackupAsync(cancellationToken);
        RefreshList();
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        await RunAsync(async token =>
        {
            var backup = await backupService.CreateBackupAsync(cancellationToken: token);
            RefreshList(backup.FilePath);
            SuccessMessage = "تم إنشاء نسخة احتياطية كاملة بنجاح.";
        });
    }

    [RelayCommand]
    private void Refresh()
    {
        ClearMessages();
        RefreshList(SelectedBackup?.FilePath);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ExportAsync()
    {
        var backup = SelectedBackup!;
        var dialog = new SaveFileDialog
        {
            Title = "تصدير النسخة الاحتياطية",
            Filter = "SQLite Database (*.db)|*.db",
            FileName = backup.FileName,
            AddExtension = true,
            DefaultExt = ".db"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await RunAsync(async token =>
        {
            await backupService.ExportBackupAsync(backup.FilePath, dialog.FileName, token);
            SuccessMessage = "تم تصدير النسخة الاحتياطية إلى المكان المحدد.";
        });
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "استيراد نسخة احتياطية",
            Filter = "SQLite Database (*.db)|*.db",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            await ConfirmAndRestoreAsync(dialog.FileName);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private Task RestoreAsync() => ConfirmAndRestoreAsync(SelectedBackup!.FilePath);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Delete()
    {
        var backup = SelectedBackup!;
        var answer = ShowConfirmation(
            $"هل تريد حذف النسخة التالية نهائيًا؟\n{backup.FileName}",
            "حذف النسخة الاحتياطية",
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            backupService.DeleteBackup(backup.FilePath);
            RefreshList();
            SuccessMessage = "تم حذف النسخة الاحتياطية.";
        }
        catch (Exception exception)
        {
            ErrorMessage = $"تعذر حذف النسخة الاحتياطية: {exception.Message}";
        }
    }

    [RelayCommand]
    private void OpenBackupsFolder()
    {
        Directory.CreateDirectory(backupService.BackupsDirectory);
        Process.Start(new ProcessStartInfo(backupService.BackupsDirectory) { UseShellExecute = true });
    }

    private bool HasSelection() => SelectedBackup is not null;

    private async Task ConfirmAndRestoreAsync(string sourcePath)
    {
        var answer = ShowConfirmation(
            "سيتم استبدال جميع البيانات الحالية بمحتوى النسخة المحددة.\nسيُنشئ البرنامج نسخة أمان أولًا ثم يعيد التشغيل. هل تريد المتابعة؟",
            "تأكيد استعادة البيانات",
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        await RunAsync(async token =>
        {
            await backupService.RestoreBackupAsync(sourcePath, token);
            MessageBox.Show(
                "تمت استعادة البيانات بنجاح. سيُعاد تشغيل البرنامج الآن.",
                "اكتملت الاستعادة",
                MessageBoxButton.OK,
                MessageBoxImage.Information,
                MessageBoxResult.OK,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

            var executablePath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                Process.Start(new ProcessStartInfo(executablePath) { UseShellExecute = true });
            }

            System.Windows.Application.Current.Shutdown();
        });
    }

    private async Task RunAsync(Func<CancellationToken, Task> operation)
    {
        try
        {
            ClearMessages();
            await ExecuteBusyAsync(operation);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"تعذر إكمال العملية: {exception.Message}";
        }
    }

    private void RefreshList(string? selectedPath = null)
    {
        var backups = backupService.GetBackups();
        Backups.Clear();
        foreach (var backup in backups)
        {
            Backups.Add(backup);
        }

        SelectedBackup = Backups.FirstOrDefault(item =>
            string.Equals(item.FilePath, selectedPath, StringComparison.OrdinalIgnoreCase));
        OnPropertyChanged(nameof(HasBackups));
    }

    private void ClearMessages()
    {
        SuccessMessage = null;
        ErrorMessage = null;
    }

    private static MessageBoxResult ShowConfirmation(string message, string title, MessageBoxImage image) =>
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNo,
            image,
            MessageBoxResult.No,
            MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
}
