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
else if (args is ["--demo", "7"])
    StageDemos.Search();
else
    StageDemos.Preview();
