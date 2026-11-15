using System.Text;
using EmployeeNotebook;

Console.OutputEncoding = Encoding.UTF8;
if (args is ["--demo", "4"])
    StageDemos.Storage();
else if (args is ["--demo", "5"])
    StageDemos.Seed();
else
    Console.WriteLine("АРМ «Записная книжка сотрудников» — этап 5. Проверки: --demo 4, --demo 5");
