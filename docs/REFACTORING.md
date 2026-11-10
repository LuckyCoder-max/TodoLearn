# Refactoring progress

## Stage 1: task creation and editing

Previously, `TaskListView` read individual controls in another view, validated
their contents, constructed an update entity and copied saved values back into
the displayed task. This made editing behavior depend on MAUI controls and
required maintaining many control aliases in the list view.

The editor now follows MVVM:

- `TaskEditorViewModel` owns form state, validation, isolated step drafts,
  normalization, saving state and the create/edit workflow.
- `ITaskWriter` exposes only the two persistence operations the editor uses.
  The existing `TaskService` implements it and continues to own EF operations.
- `TaskComposerView` binds to the ViewModel. Its code handles focus, scrolling,
  animations and forwarding UI gestures.
- `TaskListView` opens the editor, requests saving, displays storage errors and
  updates the visible list. It no longer accesses the editor's input controls.
- DI creates the editor and its ViewModel. `ITaskWriter` resolves to the same
  singleton `TaskService` used elsewhere, preserving the existing account cache.

This applies single responsibility and dependency inversion to the editor.
The form state acts as a controller for the editing use case; persistence
details stay behind a small interface. No repository or generic unit-of-work
layer was added on top of EF Core.

The form has compiled XAML bindings. It is still constructed lazily and reused;
opening it does not introduce database reads. Subtask synchronization now uses
a dictionary and a set instead of repeatedly scanning both collections: the
matching work is linear in the number of steps. No timing benchmark has been
performed, so this is not a measured application-wide speedup.

The schema and external behavior are preserved. A cancelled edit does not
mutate the original, a failed save retains the draft, and repeated save actions
are ignored while a write is pending. Persisted values are applied to the same
displayed task instance after a successful edit, preserving reference identity.

## Verification

The NUnit suite includes pure ViewModel tests with an `ITaskWriter` test double
and integration tests using SQLite in memory. They check validation, defaults,
normalization, pending steps, draft isolation, failed saves, retry, duplicate
save prevention, real persistence, reminder reset and ordered subtask updates.

Run `./tests/Test.ps1` from the repository root for tests and coverage. Build the
Windows target separately as described by the MAUI project configuration.

## Remaining stages

This is the first completed stage, not an assertion that the entire application
already satisfies every design principle.

1. Extract database initialization and legacy schema upgrades from
   `MauiProgram` into a dedicated, tested initializer.
2. Separate reminder scheduling/state decisions from native notification,
   audio and modal presentation in `TaskReminderService`.
3. Apply the same ViewModel approach to login, account restoration and the
   remaining list actions; keep platform UI behind small, specific boundaries.
4. Review `TaskItem` persistence data, presentation properties and collection
   subscriptions, then simplify overlapping models where callers allow it.
5. Profile startup, tab changes and long lists before further optimization.

Each stage should build independently and retain the existing regression suite.
Design patterns should be introduced to solve a demonstrated coupling or
duplication problem rather than to increase the number of abstractions.
