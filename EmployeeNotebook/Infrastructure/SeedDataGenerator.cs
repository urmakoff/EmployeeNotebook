using EmployeeNotebook.Core;

namespace EmployeeNotebook.Infrastructure;

public static class SeedDataGenerator
{
    public static void Initialize(IEmployeeRepository repository)
    {
        if (repository.GetAll().Count != 0)
            return;

        // Все карточки вымышлены. Адреса .invalid не предназначены для доставки.
        Employee[] employees =
        [
            Demo(1, "Иванов Алексей Петрович", "ИТ", "Разработчик", "Работает", 85_000, 1m, "C#, SQL", "Картотека"),
            Demo(2, "Иванова Мария Олеговна", "ИТ", "Тестировщик", "Работает", 73_000, 0.75m, "Тестирование", "Проверка форм"),
            Demo(3, "Кузнецов Артём Ильич", "ИТ", "Администратор", "В отпуске", 115_000, 1m, "Linux, резервирование", "Инвентаризация"),
            Demo(4, "Соколова Вера Андреевна", "Бухгалтерия", "Бухгалтер", "Работает", 85_000, 0.75m, "Сверка, учёт", "Сверка остатков"),
            Demo(5, "Попов Илья Денисович", "Бухгалтерия", "Экономист", "На больничном", 78_000, 0.5m, "Бюджетирование", "План затрат"),
            Demo(6, "Смирнова Ольга Юрьевна", "Отдел кадров", "Инспектор", "Работает", 64_000, 1m, "Кадровый учёт", "Архив карточек"),
            Demo(7, "Волкова Елена Игоревна", "Отдел кадров", "Рекрутер", "В отпуске", 67_000, 0.5m, "Подбор персонала", "Стажировки"),
            Demo(8, "Фёдоров Андрей Сергеевич", "Отдел продаж", "Менеджер", "Работает", 72_000, 1.5m, "Переговоры", "Каталог услуг"),
            Demo(9, "Павлова Анна Борисовна", "Отдел продаж", "Аналитик", "Уволен", 90_000, 1m, "Анализ продаж", "План спроса"),
            Demo(10, "Алексеев Максим Львович", "Производство", "Инженер", "Работает", 160_000, 1m, "Проектирование", "Ремонт линии"),
            Demo(11, "Орлова Ирина Валерьевна", "Производство", "Технолог", "Работает", 104_000, 1m, "Контроль качества", "Контроль партии"),
            Demo(12, "Григорьев Олег Юрьевич", "Производство", "Оператор", "Работает", 40_000, 0.75m, "Настройка линии", "Смена оснастки")
        ];

        foreach (var employee in employees)
            repository.Add(employee);
    }

    private static Employee Demo(
        int number, string name, string department, string position, string status,
        decimal salary, decimal rate, string skills, string projects) => new()
    {
        TabNumber = $"ТН-{number:0000}",
        Snils = $"000-000-{number:000} 00",
        BirthDate = new(1985 + number, (number - 1) % 12 + 1, number + 6),
        EmploymentDate = new(2020 + number % 4, 9, number),
        FullName = name,
        Department = department,
        Position = position,
        Status = status,
        Salary = salary,
        Rate = rate,
        Contacts = $"employee{number:00}@example.invalid",
        Education = number % 3 == 0 ? "Среднее профессиональное" : "Высшее",
        Skills = skills,
        ExperienceProjects = projects
    };
}
