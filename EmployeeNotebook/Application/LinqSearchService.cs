using System.Globalization;
using EmployeeNotebook.Core;

namespace EmployeeNotebook.Application;

public sealed class LinqSearchService : ISearchService
{
    private static readonly StringComparer NameComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("ru-RU"), ignoreCase: true);

    public IReadOnlyList<Employee> Filter(IEnumerable<Employee> source, EmployeeFilterDto filter)
    {
        var search = filter.SearchQuery?.Trim() ?? "";
        var department = filter.Department?.Trim();
        var status = filter.Status?.Trim();
        return source
            .Where(e => search.Length == 0
                || e.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || e.TabNumber.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Where(e => IsUnrestricted(department)
                || string.Equals(e.Department, department, StringComparison.OrdinalIgnoreCase))
            .Where(e => IsUnrestricted(status)
                || string.Equals(e.Status, status, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.FullName, NameComparer)
            .ThenBy(e => e.Id)
            .ToList();
    }

    private static bool IsUnrestricted(string? value) => string.IsNullOrEmpty(value)
        || string.Equals(value, "Все", StringComparison.OrdinalIgnoreCase);
}
