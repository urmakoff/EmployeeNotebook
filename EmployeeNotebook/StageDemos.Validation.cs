using System.Globalization;
using EmployeeNotebook.Application;

namespace EmployeeNotebook;

internal static partial class StageDemos
{
    public static void Pay()
    {
        var service = new EmployeeService();
        var payment = service.CalculatePay(85_000m, 0.75m);
        Require(payment == 63_750m && service.CalculatePay(100.005m, 1m) == 100.01m,
            "Точный расчёт и округление до копейки");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Оклад 85 000 руб. × ставка 0,75");
        Console.WriteLine($"Результат: {payment.ToString("F2", CultureInfo.InvariantCulture)} руб.");
        Console.ResetColor();
    }

    public static void Validation()
    {
        var service = new EmployeeService();
        var employee = DemoEmployee();
        Require(service.Validate(employee).IsSuccess, "Корректная карточка проходит проверку");

        employee.FullName = "";
        employee.Snils = "12-34";
        employee.Salary = -5000m;
        var result = service.Validate(employee);
        Require(!result.IsSuccess && result.Errors.Count == 3, "Отклонение трёх ошибок ввода");
        Console.WriteLine("Попытка сохранить: ФИО пустое, СНИЛС 12-34, оклад −5 000 руб.");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[ОШИБКА ВАЛИДАЦИИ] Карточка отклонена:");
        for (var index = 0; index < result.Errors.Count; index++)
            Console.WriteLine($"{index + 1}. {result.Errors[index]}");
        Console.ResetColor();
    }
}
