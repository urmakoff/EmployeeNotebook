using System.Globalization;

namespace EmployeeNotebook.Presentation;

internal static class ConsoleUi
{
    public static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    public static string Read(string prompt)
    {
        Console.Write(prompt);
        return (Console.ReadLine() ?? throw new EndOfStreamException()).Trim();
    }

    public static void WriteColor(string message, ConsoleColor color)
    {
        if (Console.IsOutputRedirected)
        {
            if (Environment.GetEnvironmentVariable("EMPLOYEE_NOTEBOOK_ANSI") == "1")
            {
                var code = color switch
                {
                    ConsoleColor.Green => 92,
                    ConsoleColor.Red => 91,
                    ConsoleColor.Cyan => 96,
                    _ => 39
                };
                Console.WriteLine($"\u001b[{code}m{message}\u001b[0m");
            }
            else
                Console.WriteLine(message);
            return;
        }

        var previous = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = previous;
        }
    }

    public static string Pick(string label, IReadOnlyList<string> options, string current)
    {
        Console.WriteLine(string.Join("  ", options.Select((option, index) => $"{index + 1} — {option}")));
        while (true)
        {
            var answer = Read($"{label} [{current}]: ");
            if (answer.Length == 0)
                return current;
            if (int.TryParse(answer, out var index) && index >= 1 && index <= options.Count)
                return options[index - 1];
            var choice = options.FirstOrDefault(s => string.Equals(s, answer, StringComparison.OrdinalIgnoreCase));
            if (choice is not null)
                return choice;
            WriteColor("Выберите номер или название из списка.", ConsoleColor.Red);
        }
    }

    public static string Money(decimal value) => value.ToString("N2", Russian);
}
