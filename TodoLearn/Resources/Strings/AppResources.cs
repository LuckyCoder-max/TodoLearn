using System.Globalization;
using System.Resources;

namespace TodoLearn.Resources.Strings;

public static class AppResources
{
    private static readonly ResourceManager ResourceManager = new(
        "TodoLearn.Resources.Strings.AppResources",
        typeof(AppResources).Assembly);

    private static string GetString(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    public static string AppName => GetString(nameof(AppName));
    public static string LoginPageTitle => GetString(nameof(LoginPageTitle));
    public static string UsernamePlaceholder => GetString(nameof(UsernamePlaceholder));
    public static string PasswordPlaceholder => GetString(nameof(PasswordPlaceholder));
    public static string LoginButton => GetString(nameof(LoginButton));
    public static string RegisterButton => GetString(nameof(RegisterButton));
    public static string InvalidCredentials => GetString(nameof(InvalidCredentials));
    public static string LoginLocked => GetString(nameof(LoginLocked));
    public static string ApplicationWindowUnavailable => GetString(nameof(ApplicationWindowUnavailable));
    public static string LoginFailed => GetString(nameof(LoginFailed));
    public static string AccountCreated => GetString(nameof(AccountCreated));
    public static string RegistrationFailed => GetString(nameof(RegistrationFailed));
    public static string UsernameRequired => GetString(nameof(UsernameRequired));
    public static string UsernameTooLong => GetString(nameof(UsernameTooLong));
    public static string PasswordRequired => GetString(nameof(PasswordRequired));
    public static string PasswordTooShort => GetString(nameof(PasswordTooShort));
    public static string UsernameTaken => GetString(nameof(UsernameTaken));
    public static string SessionExpiredTitle => GetString(nameof(SessionExpiredTitle));
    public static string SessionExpiredMessage => GetString(nameof(SessionExpiredMessage));
    public static string OkButton => GetString(nameof(OkButton));
    public static string LoginPageRequired => GetString(nameof(LoginPageRequired));
    public static string TaskServiceRequired => GetString(nameof(TaskServiceRequired));
    public static string TaskListTitle => GetString(nameof(TaskListTitle));
    public static string MyDay => GetString(nameof(MyDay));
    public static string Important => GetString(nameof(Important));
    public static string Planned => GetString(nameof(Planned));
    public static string AllTasks => GetString(nameof(AllTasks));
    public static string AddTaskPlaceholder => GetString(nameof(AddTaskPlaceholder));
    public static string AddButton => GetString(nameof(AddButton));
    public static string DeleteButton => GetString(nameof(DeleteButton));
    public static string EditTaskPlaceholder => GetString(nameof(EditTaskPlaceholder));
    public static string DescriptionPlaceholder => GetString(nameof(DescriptionPlaceholder));
    public static string PriorityLabel => GetString(nameof(PriorityLabel));
    public static string PriorityLow => GetString(nameof(PriorityLow));
    public static string PriorityMedium => GetString(nameof(PriorityMedium));
    public static string PriorityHigh => GetString(nameof(PriorityHigh));
    public static string DueLabel => GetString(nameof(DueLabel));
    public static string SaveButton => GetString(nameof(SaveButton));
    public static string CancelButton => GetString(nameof(CancelButton));
    public static string ClearButton => GetString(nameof(ClearButton));
    public static string ShowFullDescription => GetString(nameof(ShowFullDescription));
    public static string CollapseDescription => GetString(nameof(CollapseDescription));
    public static string CreatedAtFormat => GetString(nameof(CreatedAtFormat));
    public static string DueAtFormat => GetString(nameof(DueAtFormat));
    public static string AllTasksPageTitle => GetString(nameof(AllTasksPageTitle));
    public static string AllTasksEmpty => GetString(nameof(AllTasksEmpty));
    public static string ImportantPageTitle => GetString(nameof(ImportantPageTitle));
    public static string ImportantTasksEmpty => GetString(nameof(ImportantTasksEmpty));
    public static string PlannedPageTitle => GetString(nameof(PlannedPageTitle));
    public static string PlannedTasksEmpty => GetString(nameof(PlannedTasksEmpty));
    public static string TaskAddedLog => GetString(nameof(TaskAddedLog));
    public static string TaskDeletedLog => GetString(nameof(TaskDeletedLog));
    public static string TaskEditedLog => GetString(nameof(TaskEditedLog));
}
