namespace EmployeeNotebook.Core;

public interface IEmployeeService
{
    decimal CalculatePay(decimal salary, decimal rate);
    OperationResult Validate(Employee employee);
}
