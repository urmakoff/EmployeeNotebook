using System.Globalization;
using System.Net;
using EmployeeNotebook.Core;

namespace EmployeeNotebook.Presentation;

internal static class EmployeeReports
{
    public static string Card(Employee employee, IEmployeeService service, DateTime date, string actions = "")
    {
        var details = CardRows(employee, service, date)
            .Select(item => $"<tr><th scope=\"row\">{H(item.Label)}</th><td>{H(item.Value)}</td></tr>");
        return Page("Карточка сотрудника", $"""
            {actions}
            <h2>{H(employee.FullName)}</h2>
            <p>Данные на {date:dd.MM.yyyy}</p>
            <table><tbody>{string.Concat(details)}</tbody></table>
            """);
    }

    public static string Roster(IReadOnlyList<Employee> employees, EmployeeFilterDto filter, DateTime date)
    {
        var rows = employees.Select(employee => Row(employee.TabNumber, employee.FullName, employee.Department,
            employee.Position, employee.Status, employee.Education, EmployeeDates.Age(employee, date).ToString(),
            EmployeeDates.Tenure(employee, date)?.ToString() ?? "-"));
        var summary = employees.GroupBy(employee => employee.Department).Select(group =>
            Row(group.Key, group.Count().ToString(), group.Count(e => e.Status != "Уволен").ToString()));
        return Page("Отчёт по сотрудникам", $"""
            <p>Дата: {date:dd.MM.yyyy}</p>
            <p>Поиск: {H(filter.SearchQuery ?? "Все")} | Отдел: {H(filter.Department ?? "Все")} | Статус: {H(filter.Status ?? "Все")}</p>
            <p>Записей в отчёте: {employees.Count}</p>
            <h2>Список сотрудников</h2>
            {Table(["Табельный №", "ФИО", "Отдел", "Должность", "Статус", "Образование", "Возраст, лет", "Стаж в организации, лет"], rows)}
            <p>Возраст и стаж указаны в полных годах. Для уволенных стаж не рассчитан, поскольку дата увольнения не хранится.</p>
            <h2>Сводка по отделам</h2>
            {Table(["Отдел", "Всего в выборке", "В штате"], summary)}
            """);
    }

    private static (string Label, string Value)[] CardRows(Employee e, IEmployeeService service, DateTime date) =>
    [
        ("Табельный номер", e.TabNumber), ("СНИЛС", e.Snils), ("Отдел", e.Department),
        ("Должность", e.Position), ("Статус", e.Status), ("Контакты", e.Contacts),
        ("Дата рождения", e.BirthDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)),
        ("Возраст, полных лет", EmployeeDates.Age(e, date).ToString()),
        ("Дата трудоустройства", e.EmploymentDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)),
        ("Стаж в организации, полных лет", EmployeeDates.Tenure(e, date)?.ToString() ?? "Нет даты увольнения"),
        ("Образование", e.Education), ("Навыки", e.Skills), ("Опыт / проекты", e.ExperienceProjects),
        ("Оклад, руб.", ConsoleUi.Money(e.Salary)), ("Ставка", e.Rate.ToString(ConsoleUi.Russian)),
        ("К выплате, руб.", ConsoleUi.Money(service.CalculatePay(e.Salary, e.Rate)))
    ];

    internal static string Table(string[] headings, IEnumerable<string> rows) =>
        $"<div class=\"table\"><table><thead><tr>{string.Concat(headings.Select(h => $"<th scope=\"col\">{H(h)}</th>"))}</tr></thead><tbody>{string.Concat(rows)}</tbody></table></div>";

    private static string Row(params string[] cells) =>
        $"<tr>{string.Concat(cells.Select(cell => $"<td>{H(cell)}</td>"))}</tr>";

    internal static string H(string text) => WebUtility.HtmlEncode(text);

    internal static string Page(string title, string body) => $$"""
        <!doctype html>
        <html lang="ru"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
        <title>{{H(title)}}</title>
        <style>
          body{font:16px system-ui,sans-serif;max-width:70rem;margin:1rem auto;padding:0 1rem}
          table{border-collapse:collapse;width:100%} th,td{border:1px solid #aaa;padding:.45rem;text-align:left}
          th{background:#eee} .table{overflow:auto} label{display:block;margin:.45rem 0}
          input,select,button{font:inherit} input,select{max-width:100%;box-sizing:border-box}
          .actions{margin:.75rem 0} .actions a{margin-right:1rem}
          @media print{.actions{display:none} body{max-width:none} .table{overflow:visible} }
        </style></head><body><h1>{{H(title)}}</h1>{{body}}</body></html>
        """;
}
