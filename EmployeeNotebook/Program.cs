using System.Text;
using EmployeeNotebook;

Console.OutputEncoding = Encoding.UTF8;
if (args is ["--demo", "4"])
    StageDemos.Storage();
else
    Console.WriteLine("АРМ «Записная книжка сотрудников» — этап 4. Проверка: --demo 4");
