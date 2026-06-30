using System.Windows;

namespace AutoPartsPOS.WPF.Services;

public sealed class DeleteConfirmationService(MainWindow mainWindow) : IDeleteConfirmationService
{
    public bool Confirm(string itemType, string itemName)
    {
        var result = MessageBox.Show(
            mainWindow,
            $"هل أنت متأكد من حذف {itemType} \"{itemName}\" نهائيًا؟\n\nلا يمكن التراجع عن هذا الإجراء، ولن يتم الحذف إذا كان السجل مرتبطًا ببيانات أخرى.",
            "تأكيد الحذف النهائي",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No,
            MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

        return result == MessageBoxResult.Yes;
    }

    public bool ConfirmLineRemoval(string itemName)
    {
        var result = MessageBox.Show(
            mainWindow,
            $"هل تريد إزالة الصنف \"{itemName}\" من الفاتورة؟",
            "تأكيد إزالة السطر",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No,
            MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

        return result == MessageBoxResult.Yes;
    }
}
