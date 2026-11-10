# TodoLearn tests

The NUnit suite checks the main application rules without starting MAUI or
accessing the application's database, Google account, or secure storage.
It is included in `TodoLearn.sln` under the `tests` solution folder.

## Run in Visual Studio

1. Open `TodoLearn.sln` and let NuGet restore the packages.
2. Open **Test > Test Explorer**.
3. Select **Run All Tests**. Group by **Traits** to separate `Unit` and
   `Integration` tests.

NUnit3TestAdapter is installed in the test project; no IDE extension is needed.

## Run from the repository root

Requires the .NET 10 SDK. The test project does not require MAUI workloads.

```powershell
dotnet test tests/TodoLearn.Tests/TodoLearn.Tests.csproj
dotnet test tests/TodoLearn.Tests/TodoLearn.Tests.csproj --filter TestCategory=Unit
dotnet test tests/TodoLearn.Tests/TodoLearn.Tests.csproj --filter TestCategory=Integration
```

Run tests with coverage and an 80% minimum line-coverage check:

```powershell
./tests/Test.ps1
```

Or collect coverage directly:

```powershell
dotnet test tests/TodoLearn.Tests/TodoLearn.Tests.csproj --settings tests/coverage.runsettings --collect "XPlat Code Coverage" --results-directory TestResults
```

TRX test results and Cobertura coverage reports are written to `TestResults/`,
which Git ignores. The script fails if a test fails, coverage is missing, or
line coverage falls below the configured minimum.

## Scope

| Area | Checks |
| --- | --- |
| Credentials and hashing | Required fields, length boundaries, password whitespace, unique salts, invalid stored hashes |
| Local authentication | Registration, duplicate names, password verification, account lookup, failed-attempt lockout, expiry and reset |
| External account mapping | Reuse provider/subject identity, email changes, collision with local usernames, invalid input |
| Tasks and storage | Creation, editing, deletion, cached reads, account changes, rejection of cross-account operations |
| Categories and filters | My Day boundaries, Important, Planned, All Tasks, every category, uncategorised tasks, sorting |
| Subtasks | Ordered persistence, draft isolation, progress, completion, removal, cascade deletion, rejection of foreign steps |
| Deadlines | Nearest unfinished deadline, completed tasks, overdue counts, exact-time boundaries |
| Reminders | Ten-minute window, duplicate suppression, snooze choices, dismissal, deadline changes, completion and account isolation |

`Unit/` contains pure in-memory tests. `Integration/` exercises the real EF Core
SQLite provider using a new in-memory database for every test. These are
integration tests, not mocked database unit tests. Storage assertions reload
entities through a fresh context rather than relying on EF's change tracker.

`TestClock` supplies deterministic time for lockout and snooze checks. Tests do
not sleep. `TestSession` supplies only the current account ID through
`IAccountSession`; the actual authentication and task services are not mocked.

## How production code is tested

The MAUI executable depends on platform UI and cannot run as a plain `net10.0`
test assembly. The test project links the existing platform-independent `.cs`
files and resources directly from `TodoLearn/`; there are no copied service
implementations. This keeps the change small without moving the application
into new assemblies. The app and tests compile the same source files.

Coverage measures those linked services, helpers and models. Test fixtures,
test doubles and the resource accessor are excluded. This percentage describes
the selected application logic, not the entire MAUI executable or a percentage
of product features. New platform-independent application files must be added
to the test project's source includes when they enter this scope.

The initial 118-test run passed with 94.29% line coverage and 84.59% branch
coverage in this scope. These numbers are a baseline; rerun the script for
current results.

Not covered here: XAML layout, pointer/keyboard interactions, native Windows
notification delivery and sound, browser OAuth/token verification, secure
session storage, themes, and startup migration of an existing user database.
Those need platform integration or UI tests and manual verification.
External account mapping tests do not prove that Google OAuth itself works.

`SubtaskChecks/` is the older console smoke-check harness. Its scenarios are
also covered by the NUnit suite; use `TodoLearn.Tests` for ongoing development.

The editor refactoring adds pure `TaskEditorViewModel` tests and SQLite editor
integration tests. See `docs/REFACTORING.md` for the current architectural scope.
