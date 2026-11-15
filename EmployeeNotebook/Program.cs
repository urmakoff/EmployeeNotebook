using System.Text;
using EmployeeNotebook;

Console.OutputEncoding = Encoding.UTF8;
if (args is ["--demo", "4"])
    StageDemos.Storage();
else if (args is ["--demo", "5"])
    StageDemos.Seed();
else if (args is ["--demo", "6-pay"])
    StageDemos.Pay();
else if (args is ["--demo", "6-validation"])
    StageDemos.Validation();
else
    Console.WriteLine("АРМ «Записная книжка сотрудников» — этап 6. Проверки: --demo 4, 5, 6-pay, 6-validation");
