namespace EmployeeNotebook.Core;

public interface ISearchService
{
    IReadOnlyList<Employee> Filter(IEnumerable<Employee> source, EmployeeFilterDto filter);
}
