namespace EmployeeNotebook.Core;

public sealed record EmployeeFilterDto(string? SearchQuery, string? Department, string? Status);
