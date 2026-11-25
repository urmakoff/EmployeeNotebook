using System.Globalization;
using System.Security.Cryptography;
using EmployeeNotebook.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using static EmployeeNotebook.Presentation.EmployeeReports;

namespace EmployeeNotebook.Presentation;

internal static class WebNotebook
{
    private const string Address = "http://127.0.0.1:5080";
    private const string Back = """<a href="/">К списку сотрудников</a>""";

    public static void Run(IEmployeeRepository repository, IEmployeeService service, ISearchService search)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(Address);
        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            if (context.Request.Host.Value != "127.0.0.1:5080")
            {
                context.Response.StatusCode = 400;
                return;
            }
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'none'; style-src 'unsafe-inline'; form-action 'self'; frame-ancestors 'none'";
            await next(context);
        });

        app.MapGet("/", (string? q, string? department, string? status) =>
        {
            var filter = new EmployeeFilterDto(q, department, status);
            var employees = search.Filter(repository.GetAll(), filter);
            return Html(DirectoryPage(employees, filter));
        });
        app.MapGet("/report", (string? q, string? department, string? status) =>
        {
            var filter = new EmployeeFilterDto(q, department, status);
            return Download(Roster(search.Filter(repository.GetAll(), filter), filter, DateTime.Today), "Отчёт.html");
        });
        app.MapGet("/employees/{id:int}", (int id) => repository.GetById(id) is { } employee
            ? Html(Card(employee, service, DateTime.Today, $"""
                <nav class="actions">{Back}<a href="/employees/{id}/edit">Изменить</a>
                <a href="/employees/{id}/card">Скачать карточку</a><a href="/employees/{id}/delete">Удалить</a></nav>
                """))
            : Missing());
        app.MapGet("/employees/{id:int}/card", (int id) => repository.GetById(id) is { } employee
            ? Download(Card(employee, service, DateTime.Today), $"Карточка {id}.html")
            : Missing());
        app.MapGet("/employees/new", () => Html(Form(Values(new Employee { Department = "ИТ" }), null, token)));
        app.MapGet("/employees/{id:int}/edit", (int id) => repository.GetById(id) is { } employee
            ? Html(Form(Values(employee), id, token)) : Missing());
        app.MapGet("/employees/{id:int}/delete", (int id) => repository.GetById(id) is { } employee
            ? Html(Page("Удаление сотрудника", $$"""
                <p>Удалить карточку «{{H(employee.FullName)}}»?</p>
                <form method="post" action="/employees/{{id}}/delete">{{Token(token)}}
                <button type="submit">Удалить карточку</button>
                <a href="/employees/{{id}}">Отмена</a></form>
                """))
            : Missing());
        app.MapPost("/employees/{id:int}/delete", async (int id, HttpRequest request) =>
        {
            var form = await ReadForm(request, token);
            if (form is null)
                return Results.StatusCode(403);
            return repository.Delete(id) ? Results.Redirect("/") : Missing();
        });
        app.MapPost("/employees/save/{id:int?}", async (int? id, HttpRequest request) =>
        {
            var form = await ReadForm(request, token);
            if (form is null)
                return Results.StatusCode(403);
            return Save(repository, service, form, id, token);
        });
        Console.WriteLine($"Записная книжка: {Address}. Для остановки нажмите Ctrl+C.");
        app.Run();
    }

    private static async Task<IFormCollection?> ReadForm(HttpRequest request, string token)
    {
        if (!request.HasFormContentType)
            return null;
        var form = await request.ReadFormAsync();
        return form["token"] == token ? form : null;
    }

    private static IResult Save(IEmployeeRepository repository, IEmployeeService service, IFormCollection form, int? id, string token)
    {
        string Value(string key) => form[key].ToString().Trim();
        var employee = new Employee
        {
            Id = id ?? 0, TabNumber = Value("tab"), Snils = Value("snils"), FullName = Value("name"),
            Department = Value("department"), Position = Value("position"), Status = Value("status"),
            Contacts = Value("contacts"), Skills = Value("skills"), ExperienceProjects = Value("projects"),
            Education = Value("education"), BirthDate = ParseDate(Value("birth")),
            EmploymentDate = ParseDate(Value("hire")), Salary = ParseNumber(Value("salary")),
            Rate = ParseNumber(Value("rate"))
        };
        var errors = service.Validate(employee).Errors;
        if (!EmployeeCardView.DepartmentOptions.Contains(employee.Department))
            errors.Add("Выберите отдел из списка.");
        const string duplicate = "Этот табельный номер уже занят другой карточкой.";
        if (repository.GetAll().Any(e => e.Id != employee.Id &&
                e.TabNumber.Equals(employee.TabNumber, StringComparison.OrdinalIgnoreCase)))
            errors.Add(duplicate);
        var values = Fields.Select(field => field.Key).ToDictionary(key => key, Value);
        if (errors.Count != 0)
            return Html(Form(values, id, token, errors), 400);

        try
        {
            if (id is null)
                employee.Id = repository.Add(employee);
            else if (!repository.Update(employee))
                return Missing();
        }
        catch (SqliteException error) when (error.SqliteExtendedErrorCode == 2067)
        {
            return Html(Form(values, id, token, [duplicate]), 409);
        }
        return Results.Redirect($"/employees/{employee.Id}");
    }

    private static string DirectoryPage(IReadOnlyList<Employee> employees, EmployeeFilterDto filter)
    {
        var report = QueryString.Create(new Dictionary<string, string?>
        {
            ["q"] = filter.SearchQuery, ["department"] = filter.Department, ["status"] = filter.Status
        });
        var rows = employees.Select(e => $"""
            <tr><td>{H(e.TabNumber)}</td><td><a href="/employees/{e.Id}">{H(e.FullName)}</a></td>
            <td>{H(e.Department)}</td><td>{H(e.Position)}</td><td>{H(e.Status)}</td></tr>
            """);
        return Page("Записная книжка сотрудников", $$"""
            <nav class="actions"><a href="/employees/new">Добавить сотрудника</a>
            <a href="/report{{H(report.ToString())}}">Скачать отчёт</a></nav>
            <form method="get" action="/">
            <label>ФИО или табельный номер <input name="q" value="{{H(filter.SearchQuery ?? "")}}"></label>
            {{Select("department", "Отдел", filter.Department, EmployeeCardView.DepartmentOptions, allowAll: true)}}
            {{Select("status", "Статус", filter.Status, EmployeeCardView.StatusOptions, allowAll: true)}}
            <button type="submit">Найти</button> <a href="/">Сбросить</a>
            </form>
            <p>Найдено сотрудников: {{employees.Count}}</p>
            {{(employees.Count == 0 ? "<p>По заданным условиям сотрудники не найдены.</p>"
               : Table(["Табельный №", "ФИО", "Отдел", "Должность", "Статус"], rows))}}
            """);
    }

    private static readonly (string Key, string Label, string Type, bool Required)[] Fields =
    [
        ("name", "ФИО", "text", true), ("tab", "Табельный номер", "text", true),
        ("snils", "СНИЛС (XXX-XXX-XXX XX)", "text", true), ("birth", "Дата рождения", "date", true),
        ("hire", "Дата трудоустройства", "date", true), ("department", "Отдел", "", true),
        ("position", "Должность", "text", false), ("status", "Статус", "", true),
        ("education", "Образование", "text", false), ("contacts", "Контакты", "text", false),
        ("skills", "Навыки", "text", false), ("projects", "Опыт / проекты", "text", false),
        ("salary", "Оклад, руб.", "text", true), ("rate", "Ставка", "text", true)
    ];

    private static Dictionary<string, string> Values(Employee e) => new()
    {
        ["name"] = e.FullName, ["tab"] = e.TabNumber, ["snils"] = e.Snils,
        ["birth"] = DateValue(e.BirthDate), ["hire"] = DateValue(e.EmploymentDate),
        ["department"] = e.Department, ["position"] = e.Position, ["status"] = e.Status,
        ["education"] = e.Education, ["contacts"] = e.Contacts, ["skills"] = e.Skills, ["projects"] = e.ExperienceProjects,
        ["salary"] = e.Salary == 0 ? "" : e.Salary.ToString(ConsoleUi.Russian), ["rate"] = e.Rate.ToString(ConsoleUi.Russian)
    };

    private static string Form(Dictionary<string, string> values, int? id, string token, IReadOnlyList<string>? errors = null)
    {
        var inputs = Fields.Select(field => field.Key switch
        {
            "department" => Select(field.Key, field.Label, values[field.Key], EmployeeCardView.DepartmentOptions),
            "status" => Select(field.Key, field.Label, values[field.Key], EmployeeCardView.StatusOptions),
            _ => $"""<label>{H(field.Label)} <input name="{field.Key}" type="{field.Type}" value="{H(values[field.Key])}"{(field.Required ? " required" : "")}></label>"""
        });
        var errorList = errors is { Count: > 0 }
            ? $"<div role=\"alert\"><p>Исправьте данные:</p><ul>{string.Concat(errors.Select(e => $"<li>{H(e)}</li>"))}</ul></div>"
            : "";
        return Page(id is null ? "Новый сотрудник" : "Изменение карточки", $$"""
            {{errorList}}
            <form class="employee-form" method="post" action="/employees/save/{{id}}">{{Token(token)}}
            {{string.Concat(inputs)}}<button type="submit">Сохранить</button>
            <a href="{{(id is null ? "/" : $"/employees/{id}")}}">Отмена</a></form>
            """);
    }

    private static string Select(string key, string label, string? selected, IReadOnlyList<string> options, bool allowAll = false)
    {
        var all = allowAll ? """<option value="">Все</option>""" : "";
        var items = options.Select(option => $"""<option{(option == selected ? " selected" : "")}>{H(option)}</option>""");
        return $"""<label>{H(label)} <select name="{key}">{all}{string.Concat(items)}</select></label>""";
    }

    private static string DateValue(DateTime value) => value == default ? "" : value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static DateTime ParseDate(string value) => DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : default;
    private static decimal ParseNumber(string value) => decimal.TryParse(value.Replace(',', '.'),
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number) ? number : 0m;
    private static string Token(string token) => $"""<input type="hidden" name="token" value="{token}">""";
    private static IResult Html(string html, int status = 200) => Results.Content(html, "text/html; charset=utf-8", statusCode: status);
    private static IResult Missing() => Html(Page("Карточка не найдена", Back), 404);
    private static IResult Download(string html, string name) => Results.File(System.Text.Encoding.UTF8.GetBytes(html), "text/html; charset=utf-8", name);
}
