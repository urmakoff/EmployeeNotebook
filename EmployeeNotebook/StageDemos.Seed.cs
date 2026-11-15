using EmployeeNotebook.Core;
using EmployeeNotebook.Infrastructure;

namespace EmployeeNotebook;

internal static partial class StageDemos
{
    public static void Seed()
    {
        WithTemporaryDatabase((repository, path) =>
        {
            SeedDataGenerator.Initialize(repository);
            var reopened = new SqliteEmployeeRepository(path);
            SeedDataGenerator.Initialize(reopened);
            var employees = reopened.GetAll();
            Require(employees.Count == 12 && employees.Select(e => e.Department).Distinct().Count() == 5,
                "Первичное заполнение без дубликатов при повторном запуске");
            Require(employees.Count(e => e.Status == "В отпуске") == 2
                && employees.Count(e => e.Status == "На больничном") == 1
                && employees.Count(e => e.Status == "Уволен") == 1, "Распределение статусов");
            Console.WriteLine($"В базу успешно загружено {employees.Count} сотрудников:");
            PrintEmployees(employees);
        });
    }

    private static void PrintEmployees(IEnumerable<Employee> employees)
    {
        Console.WriteLine($"{"ФИО",-29} {"Отдел",-16} {"Должность",-16} Статус");
        foreach (var employee in employees)
            Console.WriteLine($"{employee.FullName,-29} {employee.Department,-16} {employee.Position,-16} {employee.Status}");
    }
}
