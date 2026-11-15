namespace EmployeeNotebook.Core;

public interface IEmployeeRepository
{
    IReadOnlyList<Employee> GetAll();
    Employee? GetById(int id);
    int Add(Employee employee);
    bool Update(Employee employee);
    bool Delete(int id);
}
