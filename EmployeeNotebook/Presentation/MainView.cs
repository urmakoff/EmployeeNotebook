using EmployeeNotebook.Core;

namespace EmployeeNotebook.Presentation;

public sealed class MainView
{
    private readonly IEmployeeRepository repository;
    private readonly IEmployeeService employeeService;
    private readonly ISearchService searchService;
    private readonly EmployeeCardView card;
    private EmployeeFilterDto filter = new(null, null, null);

    public MainView(IEmployeeRepository repository, IEmployeeService employeeService, ISearchService searchService)
    {
        this.repository = repository;
        this.employeeService = employeeService;
        this.searchService = searchService;
        card = new EmployeeCardView(employeeService);
    }

    public void Run()
    {
        try
        {
            while (true)
            {
                ShowDirectory();
                Console.WriteLine("[1] Карточка  [2] Добавить  [3] Удалить");
                Console.WriteLine("[4] Поиск и фильтры  [5] Сбросить фильтры  [6] Отчёт  [0] Выход");
                switch (ConsoleUi.Read("Действие: "))
                {
                    case "1": OpenCard(); break;
                    case "2": AddCard(); break;
                    case "3": DeleteCard(); break;
                    case "4": SetFilters(); break;
                    case "5": filter = new(null, null, null); break;
                    case "6": SaveReport(); break;
                    case "0": Console.WriteLine("Выход из записной книжки."); return;
                    default: ConsoleUi.WriteColor("Выберите действие от 0 до 6.", ConsoleColor.Red); break;
                }
            }
        }
        catch (EndOfStreamException)
        {
            Console.WriteLine();
        }
    }

    private void ShowDirectory()
    {
        var employees = repository.GetAll();
        var visible = searchService.Filter(employees, filter);
        Console.WriteLine();
        ConsoleUi.WriteColor("АРМ «ЗАПИСНАЯ КНИЖКА СОТРУДНИКОВ» (УПАСбд-31)", ConsoleColor.Cyan);
        Console.WriteLine($"[ФИЛЬТРЫ] Поиск: {filter.SearchQuery ?? "Все"} | Отдел: {filter.Department ?? "Все"} | Статус: {filter.Status ?? "Все"}");
        Console.WriteLine($"Отображено записей: {visible.Count} из {employees.Count}");
        Console.WriteLine($"{"ID",3} {"Таб. №",-8} {"ФИО",-25} {"Отдел",-13} {"Должность",-14} {"Статус",-13} {"К выплате, ₽",13}  Контакты");
        Console.WriteLine(new string('─', 107));
        foreach (var e in visible)
            Console.WriteLine($"{e.Id,3} {Cell(e.TabNumber, 8)} {Cell(e.FullName, 25)} {Cell(e.Department, 13)} "
                + $"{Cell(e.Position, 14)} {Cell(e.Status, 13)} {ConsoleUi.Money(employeeService.CalculatePay(e.Salary, e.Rate)),13}  {Cell(e.Contacts, 13).TrimEnd()}");
        if (visible.Count == 0)
            Console.WriteLine("По заданным условиям сотрудники не найдены.");
    }

    private void OpenCard()
    {
        var employee = ReadEmployee();
        if (employee is null)
            return;
        card.Show(employee);
        var action = ConsoleUi.Read("[1] Изменить  [2] Выгрузить карточку  [Enter] Назад: ");
        if (action == "2")
            SaveHtml("Карточка.html", EmployeeReports.Card(employee, employeeService, DateTime.Today));
        if (action != "1")
            return;
        var edited = card.ReadForm(employee);
        if (edited is null || !IsUnique(edited))
            return;
        if (!repository.Update(edited))
        {
            ConsoleUi.WriteColor("Карточка больше не существует.", ConsoleColor.Red);
            return;
        }
        ConsoleUi.WriteColor("Изменения сохранены.", ConsoleColor.Green);
        card.Show(edited);
        ConsoleUi.Read("[Enter] К списку: ");
    }

    private void SaveReport()
    {
        var visible = searchService.Filter(repository.GetAll(), filter);
        SaveHtml("Отчёт.html", EmployeeReports.Roster(visible, filter, DateTime.Today));
    }

    private static void SaveHtml(string fileName, string html)
    {
        try
        {
            var folder = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Отчёты"));
            var path = Path.Combine(folder.FullName, fileName);
            // Повторная выгрузка не затирает предыдущий отчёт.
            for (var number = 2; File.Exists(path); number++)
                path = Path.Combine(folder.FullName, $"{Path.GetFileNameWithoutExtension(fileName)} {number}.html");
            using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            using var writer = new StreamWriter(file);
            writer.Write(html);
            Console.WriteLine($"Сохранено: {path}");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            ConsoleUi.WriteColor($"Не удалось сохранить отчёт: {error.Message}", ConsoleColor.Red);
        }
    }

    private void AddCard()
    {
        var employee = card.ReadForm();
        if (employee is null || !IsUnique(employee))
            return;
        employee.Id = repository.Add(employee);
        ConsoleUi.WriteColor($"Карточка добавлена: ID {employee.Id}, {employee.TabNumber}, {employee.FullName}.", ConsoleColor.Green);
        ConsoleUi.Read("[Enter] К списку: ");
    }

    private void DeleteCard()
    {
        var employee = ReadEmployee();
        if (employee is null)
            return;
        if (ConsoleUi.Read($"Удалить «{employee.FullName}»? [1] Да  [0] Нет: ") != "1")
            return;
        if (repository.Delete(employee.Id))
            ConsoleUi.WriteColor("Карточка удалена.", ConsoleColor.Green);
        else
            ConsoleUi.WriteColor("Карточка уже удалена.", ConsoleColor.Red);
    }

    private bool IsUnique(Employee employee)
    {
        if (!repository.GetAll().Any(e => e.Id != employee.Id
                && string.Equals(e.TabNumber, employee.TabNumber, StringComparison.OrdinalIgnoreCase)))
            return true;
        ConsoleUi.WriteColor("Этот табельный номер уже занят другой карточкой.", ConsoleColor.Red);
        return false;
    }

    private Employee? ReadEmployee()
    {
        var value = ConsoleUi.Read("ID сотрудника: ");
        var employee = int.TryParse(value, out var id) && id > 0 ? repository.GetById(id) : null;
        if (employee is null)
            ConsoleUi.WriteColor("Карточка с таким ID не найдена.", ConsoleColor.Red);
        return employee;
    }

    private void SetFilters()
    {
        var query = ConsoleUi.Read("Поиск по ФИО или табельному номеру (Enter - все): ");
        var department = ConsoleUi.Pick("Отдел", ["Все", .. EmployeeCardView.DepartmentOptions], "Все");
        var status = ConsoleUi.Pick("Статус", ["Все", .. EmployeeCardView.StatusOptions], "Все");
        filter = new(query.Length == 0 ? null : query, department, status);
    }

    private static string Cell(string value, int width) =>
        (value.Length <= width ? value : value[..(width - 1)] + "…").PadRight(width);
}
