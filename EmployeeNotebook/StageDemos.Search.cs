using EmployeeNotebook.Application;
using EmployeeNotebook.Core;
using EmployeeNotebook.Infrastructure;

namespace EmployeeNotebook;

internal static partial class StageDemos
{
    public static void Search()
    {
        WithTemporaryDatabase((repository, _) =>
        {
            SeedDataGenerator.Initialize(repository);
            var source = repository.GetAll();
            var service = new LinqSearchService();
            var it = service.Filter(source, new EmployeeFilterDto(null, "ИТ", "Все"));
            var name = service.Filter(source, new EmployeeFilterDto("иванов", "ит", "работает"));
            var tab = service.Filter(source, new EmployeeFilterDto("тн-0004", null, null));
            Require(source.Count == 12 && it.Count == 3 && it.All(e => e.Department == "ИТ"), "Фильтр по отделу");
            Require(name.Count == 2 && name.All(e => e.FullName.StartsWith("Иванов", StringComparison.Ordinal)
                && e.Department == "ИТ" && e.Status == "Работает"), "Сочетание поиска, отдела и статуса");
            Require(tab.Count == 1 && tab[0].TabNumber == "ТН-0004", "Поиск по табельному номеру");
            Require(service.Filter(source, new EmployeeFilterDto("не найдено", null, null)).Count == 0,
                "Пустой результат поиска");

            Console.WriteLine($"Исходная картотека: {source.Count} сотрудников.");
            Console.WriteLine($"Отдел = ИТ: найдено {it.Count}.");
            PrintEmployees(it);
            Console.WriteLine();
            Console.WriteLine($"Поиск «иванов» + ИТ + «Работает»: найдено {name.Count}.");
            PrintEmployees(name);
        });
    }
}
