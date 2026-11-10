namespace TodoLearn.Views;

public partial class TaskComposerView : ContentView
{
    public event EventHandler? SaveRequested;
    public event EventHandler? CancelRequested;
    public TaskComposerView()
    {
        InitializeComponent();
        SizeChanged += (_, _) =>
        {
            Dialog.WidthRequest = Math.Min(480, Math.Max(0, Width - 48));
            Dialog.HeightRequest = Math.Min(680, Math.Max(0, Height - 48));
        };
    }
    private void OnTitleCompleted(object? sender, EventArgs e) => NewTaskDescriptionEditor.Focus();
    private void OnTitleChanged(object? sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.NewTextValue)) ValidationLabel.IsVisible = false;
    }
    private void OnSaveClicked(object? sender, EventArgs e) => SaveRequested?.Invoke(this, e);
    private void OnCancelClicked(object? sender, EventArgs e) => CancelRequested?.Invoke(this, e);

    public System.Collections.ObjectModel.ObservableCollection<Models.TaskStep> DraftSteps { get; } = new();
    public void SetSteps(IEnumerable<Models.TaskStep> steps)
    {
        DraftSteps.Clear();
        foreach (var step in steps) DraftSteps.Add(step.Copy());
        StepEntry.Text = string.Empty;
    }
    private void OnAddStepClicked(object? sender, EventArgs e)
    {
        var text = StepEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;
        DraftSteps.Add(new Models.TaskStep { Text = text });
        StepEntry.Text = string.Empty;
        StepEntry.Focus();
    }
    private void OnRemoveStepClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: Models.TaskStep step }) DraftSteps.Remove(step);
    }
    public void IncludePendingStep() => OnAddStepClicked(this, EventArgs.Empty);
}