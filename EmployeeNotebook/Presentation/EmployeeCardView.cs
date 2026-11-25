using System.Globalization;
using EmployeeNotebook.Core;

namespace EmployeeNotebook.Presentation;

public sealed class EmployeeCardView(IEmployeeService service)
{
    private static readonly string[] Departments = ["ИТ", "Бухгалтерия", "Отдел кадров", "Отдел продаж", "Производство"];
    private static readonly string[] Statuses = ["Работает", "В отпуске", "На больничном", "Уволен"];
    internal static IReadOnlyList<string> DepartmentOptions => Departments;
    internal static IReadOnlyList<string> StatusOptions => Statuses;

    public void Show(Employee employee)
    {
        var today = DateTime.Today;
        ConsoleUi.WriteColor($"КАРТОЧКА СОТРУДНИКА · ID {employee.Id}", ConsoleColor.Cyan);
        Console.WriteLine($" 1. Табельный номер:     {employee.TabNumber}");
        Console.WriteLine($" 2. СНИЛС:              {employee.Snils}");
        Console.WriteLine($" 3. Дата рождения:      {employee.BirthDate:dd.MM.yyyy}");
        Console.WriteLine($" 4. Дата трудоустройства: {employee.EmploymentDate:dd.MM.yyyy}");
        Console.WriteLine($" 5. ФИО:                {employee.FullName}");
        Console.WriteLine($" 6. Отдел:              {employee.Department}");
        Console.WriteLine($" 7. Должность:          {employee.Position}");
        Console.WriteLine($" 8. Контакты:           {employee.Contacts}");
        Console.WriteLine($" 9. Навыки:             {employee.Skills}");
        Console.WriteLine($"10. Опыт / проекты:     {employee.ExperienceProjects}");
        Console.WriteLine($"11. Статус:             {employee.Status}");
        Console.WriteLine($"12. Оклад / ставка:     {ConsoleUi.Money(employee.Salary)} руб. / {employee.Rate.ToString("0.##", ConsoleUi.Russian)}");
        Console.WriteLine($"    Образование:        {ShowValue(employee.Education)}");
        Console.WriteLine($"    Возраст, полных лет: {EmployeeDates.Age(employee, today)}");
        Console.WriteLine($"    Стаж в организации, полных лет: {EmployeeDates.Tenure(employee, today)?.ToString() ?? "нет даты увольнения"}");
        ConsoleUi.WriteColor($"Итого к выплате (Оклад × Ставка): {ConsoleUi.Money(service.CalculatePay(employee.Salary, employee.Rate))} руб.",
            ConsoleColor.Green);
    }

    public Employee? ReadForm(Employee? existing = null)
    {
        var draft = existing;
        while (true)
        {
            ConsoleUi.WriteColor(existing is null ? "НОВЫЙ СОТРУДНИК" : $"ИЗМЕНЕНИЕ КАРТОЧКИ · ID {existing.Id}", ConsoleColor.Cyan);
            Console.WriteLine(draft is null
                ? "Введите данные по подсказкам. Отдел, статус и ставку можно оставить по умолчанию."
                : "Enter - оставить значение, «-» - очистить необязательное поле.");
            var employee = new Employee
            {
                Id = existing?.Id ?? 0,
                TabNumber = Text("Табельный номер (ТН-0001)", draft?.TabNumber),
                Snils = Text("СНИЛС (XXX-XXX-XXX XX)", draft?.Snils),
                BirthDate = Date("Дата рождения (ДД.ММ.ГГГГ)", draft?.BirthDate),
                EmploymentDate = Date("Дата трудоустройства (ДД.ММ.ГГГГ)", draft?.EmploymentDate),
                FullName = Text("ФИО (минимум 2 слова)", draft?.FullName),
                Department = ConsoleUi.Pick("Отдел", Departments, draft?.Department ?? "ИТ"),
                Position = Text("Должность", draft?.Position),
                Contacts = Text("Контакты", draft?.Contacts, optional: true),
                Skills = Text("Навыки (через запятую)", draft?.Skills, optional: true),
                ExperienceProjects = Text("Опыт / проекты", draft?.ExperienceProjects, optional: true),
                Status = ConsoleUi.Pick("Статус", Statuses, draft?.Status ?? "Работает"),
                Salary = Number("Оклад, руб.", draft?.Salary),
                Rate = Number("Ставка (от 0,1 до 2,0)", draft?.Rate ?? 1m),
                Education = Text("Образование (необязательно)", draft?.Education, optional: true)
            };
            var result = service.Validate(employee);
            if (result.IsSuccess)
            {
                var confirm = ConsoleUi.Read("[1] Сохранить  [0] Отмена: ");
                if (confirm == "1")
                    return employee;
                if (confirm == "0")
                    return null;
                ConsoleUi.WriteColor("Выберите 1 для сохранения или 0 для отмены.", ConsoleColor.Red);
            }
            else
            {
                ConsoleUi.WriteColor("[ОШИБКА СОХРАНЕНИЯ] Нарушены бизнес-правила:", ConsoleColor.Red);
                foreach (var error in result.Errors)
                    ConsoleUi.WriteColor($" • {error}", ConsoleColor.Red);
                if (ConsoleUi.Read("[1] Исправить  [0] Отмена: ") != "1")
                    return null;
            }
            draft = employee;
        }
    }

    private static string ShowValue(string value) => value.Length == 0 ? "Не указано" : value;

    private static string Text(string label, string? current, bool optional = false)
    {
        var value = ConsoleUi.Read(current is null ? $"{label}: " : $"{label} [{current}]: ");
        return optional && value == "-" ? "" : value.Length == 0 ? current ?? "" : value;
    }

    private static DateTime Date(string label, DateTime? current)
    {
        var visibleDate = current is { } date && date != default
            ? date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : null;
        while (true)
        {
            var value = Text(label, visibleDate);
            if (value.Length == 0)
                return default;
            if (DateTime.TryParseExact(value, "dd.MM.yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsed))
                return parsed;
            ConsoleUi.WriteColor("Введите существующую дату в формате ДД.ММ.ГГГГ.", ConsoleColor.Red);
        }
    }

    private static decimal Number(string label, decimal? current)
    {
        while (true)
        {
            var value = Text(label, current?.ToString(ConsoleUi.Russian));
            if (value.Length == 0)
                return 0m;
            if (decimal.TryParse(value.Replace(',', '.'),
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var parsed))
                return parsed;
            ConsoleUi.WriteColor("Введите число, например 85000 или 0,75.", ConsoleColor.Red);
        }
    }
}
