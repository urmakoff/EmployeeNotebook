using System.Text.RegularExpressions;
using EmployeeNotebook.Core;

namespace EmployeeNotebook.Application;

public sealed partial class EmployeeService : IEmployeeService
{
    public static IReadOnlyList<string> Statuses { get; } =
        Array.AsReadOnly(new[] { "Работает", "В отпуске", "На больничном", "Уволен" });

    public decimal CalculatePay(decimal salary, decimal rate)
    {
        if (salary < 0 || rate <= 0)
            return 0m;
        return Math.Round(salary * rate, 2, MidpointRounding.AwayFromZero);
    }

    public OperationResult Validate(Employee employee)
    {
        var errors = new List<string>();
        if (employee.FullName.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length < 2)
            errors.Add("ФИО обязательно: введите не менее двух слов.");
        if (string.IsNullOrWhiteSpace(employee.TabNumber))
            errors.Add("Табельный номер обязателен.");
        if (!SnilsPattern().IsMatch(employee.Snils))
            errors.Add("Неверный формат СНИЛС: ожидается XXX-XXX-XXX XX.");
        if (employee.Salary <= 0)
            errors.Add("Оклад должен быть больше 0 рублей.");
        if (employee.Rate is < 0.1m or > 2.0m)
            errors.Add("Ставка должна быть от 0,1 до 2,0.");
        else if (employee.Rate > 1m && employee.Salary > decimal.MaxValue / employee.Rate)
            errors.Add("Оклад и ставка дают сумму выплаты, превышающую допустимый предел.");

        if (employee.BirthDate == default || employee.BirthDate.Date > DateTime.Today)
            errors.Add("Укажите действительную дату рождения.");
        else if (employee.EmploymentDate == default || employee.EmploymentDate.Date > DateTime.Today
            || employee.EmploymentDate.Date < employee.BirthDate.Date.AddYears(16))
            errors.Add("Дата трудоустройства должна быть не раньше 16-летия и не позже сегодняшнего дня.");

        if (!Statuses.Contains(employee.Status, StringComparer.Ordinal))
            errors.Add("Выберите допустимый статус сотрудника.");
        return new OperationResult(errors.Count == 0, errors);
    }

    [GeneratedRegex(@"\A[0-9]{3}-[0-9]{3}-[0-9]{3} [0-9]{2}\z", RegexOptions.CultureInvariant)]
    private static partial Regex SnilsPattern();
}
