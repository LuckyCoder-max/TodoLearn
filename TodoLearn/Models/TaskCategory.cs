using TodoLearn.Resources.Strings;

namespace TodoLearn.Models;

public enum TaskCategory
{
    Work,
    Home,
    Shopping,
    Study,
    Personal
}

public static class TaskCategories
{
    public static IReadOnlyList<string> Names => new[]
    {
        AppResources.CategoryNone,
        AppResources.CategoryWork,
        AppResources.CategoryHome,
        AppResources.CategoryShopping,
        AppResources.CategoryStudy,
        AppResources.CategoryPersonal
    };
}
