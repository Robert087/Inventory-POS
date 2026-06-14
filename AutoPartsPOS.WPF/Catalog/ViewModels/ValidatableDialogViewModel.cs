using AutoPartsPOS.Application.Common.ViewModels;
using System.Collections;
using System.ComponentModel;

namespace AutoPartsPOS.WPF.Catalog.ViewModels;

public abstract class ValidatableDialogViewModel : ViewModelBase, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public event EventHandler<bool?>? RequestClose;

    public bool HasErrors => _errors.Count > 0;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return _errors.SelectMany(error => error.Value);
        }

        return _errors.TryGetValue(propertyName, out var errors)
            ? errors
            : Array.Empty<string>();
    }

    protected void ApplyErrors(IReadOnlyDictionary<string, string[]> errors)
    {
        var changedProperties = _errors.Keys.Union(errors.Keys).Distinct().ToArray();
        _errors.Clear();

        foreach (var error in errors)
        {
            _errors[error.Key] = [.. error.Value];
        }

        foreach (var propertyName in changedProperties)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        OnPropertyChanged(nameof(HasErrors));
    }

    protected void Close(bool? dialogResult)
    {
        RequestClose?.Invoke(this, dialogResult);
    }
}
