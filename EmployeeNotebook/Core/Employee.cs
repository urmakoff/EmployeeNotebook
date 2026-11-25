namespace EmployeeNotebook.Core;

public sealed class Employee
{
    public int Id { get; set; } // Внутренний ключ записи.
    public string TabNumber { get; set; } = "";
    public string Snils { get; set; } = "";
    public DateTime BirthDate { get; set; }
    public DateTime EmploymentDate { get; set; }
    public string FullName { get; set; } = "";
    public string Department { get; set; } = "";
    public string Position { get; set; } = "";
    public string Contacts { get; set; } = "";
    public string Skills { get; set; } = "";
    public string ExperienceProjects { get; set; } = "";
    public string Education { get; set; } = "";
    public string Status { get; set; } = "Работает";
    public decimal Salary { get; set; }
    public decimal Rate { get; set; } = 1m;
}
