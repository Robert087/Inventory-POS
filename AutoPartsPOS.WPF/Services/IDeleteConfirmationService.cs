namespace AutoPartsPOS.WPF.Services;

public interface IDeleteConfirmationService
{
    bool Confirm(string itemType, string itemName);

    bool ConfirmLineRemoval(string itemName);
}
