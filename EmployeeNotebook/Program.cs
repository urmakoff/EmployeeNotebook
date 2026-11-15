using System.Text;
using EmployeeNotebook.Application;
using EmployeeNotebook.Core;
using EmployeeNotebook.Infrastructure;
using EmployeeNotebook.Presentation;
using Microsoft.Data.Sqlite;

namespace EmployeeNotebook;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            switch (args)
            {
                case ["--help"] or ["-h"]:
                    PrintUsage();
                    return 0;
                case ["--demo", var stage]:
                    return RunDemo(stage);
                case []:
                    return RunNotebook(Path.Combine(AppContext.BaseDirectory, "employees.db"));
                case ["--db", var path] when !string.IsNullOrWhiteSpace(path):
                    return RunNotebook(path);
                default:
                    PrintUsage();
                    return 2;
            }
        }
        catch (Exception error) when (error is SqliteException or IOException
            or UnauthorizedAccessException or ArgumentException)
        {
            Console.Error.WriteLine($"Не удалось открыть или обработать базу данных: {error.Message}");
            return 1;
        }
    }

    private static int RunNotebook(string databasePath)
    {
        // Infrastructure: открыть локальное хранилище и заполнить новую базу примерами.
        var path = Path.GetFullPath(databasePath);
        var firstRun = !File.Exists(path);
        IEmployeeRepository repository = new SqliteEmployeeRepository(path);
        if (firstRun)
            SeedDataGenerator.Initialize(repository);

        // Core задаёт контракты, Application реализует расчёт, валидацию и поиск.
        IEmployeeService employeeService = new EmployeeService();
        ISearchService searchService = new LinqSearchService();

        // Presentation получает зависимости через конструктор и запускает меню.
        var mainView = new MainView(repository, employeeService, searchService);
        mainView.Run();
        return 0;
    }

    private static int RunDemo(string stage)
    {
        switch (stage)
        {
            case "4": StageDemos.Storage(); break;
            case "5": StageDemos.Seed(); break;
            case "6-pay": StageDemos.Pay(); break;
            case "6-validation": StageDemos.Validation(); break;
            case "7": StageDemos.Search(); break;
            default: PrintUsage(); return 2;
        }
        return 0;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("АРМ «Записная книжка сотрудников»");
        Console.WriteLine("  Без аргументов                        Открыть картотеку.");
        Console.WriteLine("  --db <путь>                           Использовать указанный файл SQLite.");
        Console.WriteLine("  --demo <4|5|6-pay|6-validation|7>      Запустить проверку этапа на вымышленных данных.");
    }
}
