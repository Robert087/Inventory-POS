using System.Windows;

namespace AutoPartsPOS.WPF.Catalog.Dialogs;

public partial class ExistingProductConfirmationDialog : Window
{
    public ExistingProductConfirmationDialog(string productName)
    {
        InitializeComponent();
        MessageTextBlock.Text =
            $"هذا الكود مستخدم بالفعل للصنف: {productName}{Environment.NewLine}هل تريد إضافة كمية جديدة او سعر جديد لهذا الصنف؟";
        AddStockButton.Click += (_, _) =>
        {
            DialogResult = true;
            Close();
        };
        CancelButton.Click += (_, _) =>
        {
            DialogResult = false;
            Close();
        };
    }
}
