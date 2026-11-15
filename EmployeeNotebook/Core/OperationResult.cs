namespace EmployeeNotebook.Core;

public sealed record OperationResult(bool IsSuccess, List<string> Errors);
