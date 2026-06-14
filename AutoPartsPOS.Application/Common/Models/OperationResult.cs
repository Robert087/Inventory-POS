namespace AutoPartsPOS.Application.Common.Models;

public sealed class OperationResult
{
    private OperationResult(bool succeeded, IReadOnlyDictionary<string, string[]> errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public bool Succeeded { get; }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public string ErrorSummary => string.Join(Environment.NewLine, Errors.SelectMany(error => error.Value));

    public static OperationResult Success()
    {
        return new OperationResult(true, new Dictionary<string, string[]>());
    }

    public static OperationResult Failure(Dictionary<string, List<string>> errors)
    {
        return new OperationResult(
            false,
            errors.ToDictionary(error => error.Key, error => error.Value.ToArray()));
    }
}
