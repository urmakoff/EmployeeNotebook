using System.Globalization;
using EmployeeNotebook.Core;
using Microsoft.Data.Sqlite;

namespace EmployeeNotebook.Infrastructure;

public sealed class SqliteEmployeeRepository : IEmployeeRepository
{
    private readonly string connectionString;

    public SqliteEmployeeRepository(string databasePath)
    {
        var path = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Pooling = false
        }.ToString();
        using var connection = Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Employees (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TabNumber TEXT NOT NULL UNIQUE,
                Snils TEXT NOT NULL,
                BirthDate TEXT NOT NULL,
                EmploymentDate TEXT NOT NULL,
                FullName TEXT NOT NULL,
                Department TEXT NOT NULL,
                Position TEXT NOT NULL,
                Contacts TEXT,
                Skills TEXT,
                ExperienceProjects TEXT,
                Status TEXT NOT NULL,
                Salary TEXT NOT NULL,
                Rate TEXT NOT NULL,
                Education TEXT NOT NULL DEFAULT ''
            );
            """;
        command.ExecuteNonQuery();
        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Employees') WHERE name = 'Education' COLLATE NOCASE;";
        if ((long)command.ExecuteScalar()! == 0)
        {
            command.CommandText = "ALTER TABLE Employees ADD COLUMN Education TEXT NOT NULL DEFAULT '';";
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public int Add(Employee employee)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Employees (TabNumber, Snils, BirthDate, EmploymentDate, FullName,
                Department, Position, Contacts, Skills, ExperienceProjects, Status, Salary, Rate, Education)
            VALUES ($tab, $snils, $birth, $employment, $name,
                $department, $position, $contacts, $skills, $projects, $status, $salary, $rate, $education);
            SELECT last_insert_rowid();
            """;
        BindEmployee(command, employee);
        return checked((int)(long)command.ExecuteScalar()!);
    }

    public IReadOnlyList<Employee> GetAll()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM Employees ORDER BY FullName, Id";
        using var reader = command.ExecuteReader();
        var employees = new List<Employee>();
        while (reader.Read())
            employees.Add(ReadEmployee(reader));
        return employees;
    }

    public Employee? GetById(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM Employees WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadEmployee(reader) : null;
    }

    public bool Update(Employee employee)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Employees SET TabNumber = $tab, Snils = $snils, BirthDate = $birth,
                EmploymentDate = $employment, FullName = $name, Department = $department,
                Position = $position, Contacts = $contacts, Skills = $skills,
                ExperienceProjects = $projects, Status = $status, Salary = $salary, Rate = $rate,
                Education = $education
            WHERE Id = $id;
            """;
        BindEmployee(command, employee);
        command.Parameters.AddWithValue("$id", employee.Id);
        return command.ExecuteNonQuery() == 1;
    }

    public bool Delete(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Employees WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() == 1;
    }

    private const string Columns = "Id, TabNumber, Snils, BirthDate, EmploymentDate, FullName, "
        + "Department, Position, Contacts, Skills, ExperienceProjects, Status, Salary, Rate, Education";

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        try
        {
            connection.Open();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static void BindEmployee(SqliteCommand command, Employee employee)
    {
        command.Parameters.AddWithValue("$tab", employee.TabNumber);
        command.Parameters.AddWithValue("$snils", employee.Snils);
        command.Parameters.AddWithValue("$birth", employee.BirthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$employment", employee.EmploymentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$name", employee.FullName);
        command.Parameters.AddWithValue("$department", employee.Department);
        command.Parameters.AddWithValue("$position", employee.Position);
        command.Parameters.AddWithValue("$contacts", employee.Contacts);
        command.Parameters.AddWithValue("$skills", employee.Skills);
        command.Parameters.AddWithValue("$projects", employee.ExperienceProjects);
        command.Parameters.AddWithValue("$education", employee.Education);
        command.Parameters.AddWithValue("$status", employee.Status);
        // Храним десятичную запись в TEXT, чтобы SQLite не преобразовал её в REAL.
        command.Parameters.AddWithValue("$salary", employee.Salary.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$rate", employee.Rate.ToString(CultureInfo.InvariantCulture));
    }

    private static Employee ReadEmployee(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        TabNumber = reader.GetString(1),
        Snils = reader.GetString(2),
        BirthDate = DateTime.ParseExact(reader.GetString(3), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        EmploymentDate = DateTime.ParseExact(reader.GetString(4), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        FullName = reader.GetString(5),
        Department = reader.GetString(6),
        Position = reader.GetString(7),
        Contacts = reader.IsDBNull(8) ? "" : reader.GetString(8),
        Skills = reader.IsDBNull(9) ? "" : reader.GetString(9),
        ExperienceProjects = reader.IsDBNull(10) ? "" : reader.GetString(10),
        Status = reader.GetString(11),
        Salary = decimal.Parse(reader.GetString(12), CultureInfo.InvariantCulture),
        Rate = decimal.Parse(reader.GetString(13), CultureInfo.InvariantCulture),
        Education = reader.GetString(14)
    };
}
