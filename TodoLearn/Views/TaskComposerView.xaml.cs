namespace TodoLearn.Views;

public partial class TaskComposerView : ContentView
{
    private CancellationTokenSource? _motion;
    public void FocusTitle() => NewTaskEntry.Focus();





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
        Unloaded += (_, _) =>
        {
            CancelMotion();
            Helpers.UiMotion.ResetPresentation(Scrim, Dialog);
        };
    }

    public void PrepareToShow()
    {
        CancelMotion();
        _motion = new CancellationTokenSource();
        Helpers.UiMotion.ResetPresentation(Scrim, Dialog);
        Scrim.Opacity = Dialog.Opacity = Helpers.UiMotion.IsEnabled ? 0 : 1;
    }

    public async Task ShowAsync()
    {
        var token = _motion!.Token;
        try
        {
            await WaitForLayoutAsync(token);
            await FieldsScroll.ScrollToAsync(0, 0, false);
            token.ThrowIfCancellationRequested();
            await Helpers.UiMotion.ShowPresentationAsync(Scrim, Dialog, token);
            token.ThrowIfCancellationRequested();
            NewTaskEntry.Focus();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }

    public async Task HideAsync()
    {
        CancelMotion();
        _motion = new CancellationTokenSource();
        var token = _motion.Token;
        try
        {
            await Helpers.UiMotion.HidePresentationAsync(Scrim, Dialog, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }

    private async Task WaitForLayoutAsync(CancellationToken token)
    {
        if (Dialog.IsLoaded && Dialog.Width > 0 && Dialog.Height > 0)
            return;
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void CheckLayout(object? sender, EventArgs args)
        {
            if (Dialog.IsLoaded && Dialog.Width > 0 && Dialog.Height > 0)
                ready.TrySetResult();
        }
        Dialog.Loaded += CheckLayout;
        Dialog.SizeChanged += CheckLayout;
        using var registration = token.Register(() => ready.TrySetCanceled(token));
        try
        {
            CheckLayout(this, EventArgs.Empty);
            await ready.Task;
        }
        finally
        {
            Dialog.Loaded -= CheckLayout;
            Dialog.SizeChanged -= CheckLayout;
        }
    }

    private void CancelMotion()
    {
        _motion?.Cancel();
        _motion?.Dispose();
        _motion = null;
    }

    private void OnTitleCompleted(object? sender, EventArgs e) => NewTaskDescriptionEditor.Focus();

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

    private void OnTitleChanged(object? sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.NewTextValue)) ValidationLabel.IsVisible = false;
    }
}
