using EmployeeNotebook.Application;
using EmployeeNotebook.Infrastructure;
using EmployeeNotebook.Presentation;

namespace EmployeeNotebook;

internal static partial class StageDemos
{
    public static void Preview()
    {
        WithTemporaryDatabase((repository, _) =>
        {
            SeedDataGenerator.Initialize(repository);
            new MainView(repository, new EmployeeService(), new LinqSearchService()).Run();
        });
    }
}
