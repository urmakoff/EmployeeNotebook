namespace EmployeeNotebook.Core;

internal static class EmployeeDates
{
    public static int Age(Employee employee, DateTime today) =>
        CompletedYears(employee.BirthDate, today);

    public static int? Tenure(Employee employee, DateTime today) =>
        employee.Status == "Уволен" ? null : CompletedYears(employee.EmploymentDate, today);

    private static int CompletedYears(DateTime from, DateTime through)
    {
        if (from.Date > through.Date)
            return 0;
        var years = through.Year - from.Year;
        return from.Date.AddYears(years) > through.Date ? years - 1 : years;
    }
}
