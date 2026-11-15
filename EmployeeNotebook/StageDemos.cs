using EmployeeNotebook.Core;
using EmployeeNotebook.Infrastructure;

namespace EmployeeNotebook;

internal static partial class StageDemos
{
    public static void Storage()
    {
        WithTemporaryDatabase((repository, path) =>
        {
            var record = DemoEmployee();
            var id = repository.Add(record);
            var reopened = new SqliteEmployeeRepository(path);
            var read = reopened.GetById(id);
            Require(File.Exists(path), "Создание файла БД");
            Require(read is not null && read.FullName == record.FullName
                && read.Salary == record.Salary && read.Rate == record.Rate, "Чтение записанной карточки");
            Console.WriteLine($"SQLite: создан файл {Path.GetFileName(path)}");
            Console.WriteLine($"INSERT: добавлена карточка с ID {id}");
            Console.WriteLine($"SELECT после повторного открытия: {read!.FullName}, {read.TabNumber}");

            read.Position = "Старший специалист";
            Require(reopened.Update(read) && reopened.GetById(id)?.Position == read.Position,
                "Изменение карточки");
            Require(reopened.Delete(id) && reopened.GetById(id) is null && reopened.GetAll().Count == 0,
                "Удаление карточки");
            Console.WriteLine("UPDATE и DELETE: проверки пройдены.");
        });
    }

    internal static Employee DemoEmployee() => new()
    {
        TabNumber = "ТН-0999", Snils = "000-000-999 00",
        BirthDate = new(1990, 2, 10), EmploymentDate = new(2020, 9, 1),
        FullName = "Петрова Тестовая Анна", Department = "ИТ", Position = "Специалист",
        Contacts = "demo@example.invalid", Skills = "C#, SQLite",
        ExperienceProjects = "Демонстрационная картотека", Status = "Работает",
        Salary = 85_000m, Rate = 0.75m
    };

    private static void WithTemporaryDatabase(Action<IEmployeeRepository, string> action)
    {
        var directory = Directory.CreateTempSubdirectory("employee-notebook-");
        try
        {
            var path = Path.Combine(directory.FullName, "employees.db");
            action(new SqliteEmployeeRepository(path), path);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    internal static void Require(bool condition, string scenario)
    {
        if (!condition)
            throw new InvalidOperationException($"Проверка не пройдена: {scenario}");
    }
}
