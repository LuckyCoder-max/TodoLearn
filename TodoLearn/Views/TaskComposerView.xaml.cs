namespace TodoLearn.Views;

public partial class TaskComposerView : ContentView
{
    public event EventHandler? SaveRequested;
    public event EventHandler? CancelRequested;
    public TaskComposerView() => InitializeComponent();
    private void OnSaveClicked(object? sender, EventArgs e) => SaveRequested?.Invoke(this, e);
    private void OnCancelClicked(object? sender, EventArgs e) => CancelRequested?.Invoke(this, e);
}
