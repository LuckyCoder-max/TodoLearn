namespace TodoLearn.Helpers;

internal static class UiMotion
{
    public static void ResetPresentation(VisualElement scrim, VisualElement panel)
    {
        foreach (var element in new[] { scrim, panel })
        {
            element.CancelAnimations();
            element.Opacity = 1;
            element.Scale = 1;
#if WINDOWS
            if (element.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement native)
            {
                var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(native);
                visual.StopAnimation("Opacity");
                visual.StopAnimation("Scale");
                visual.Opacity = 1;
                visual.Scale = System.Numerics.Vector3.One;
            }
#endif
        }
    }

    public static async Task ShowPresentationAsync(VisualElement scrim, VisualElement panel, CancellationToken token)
    {
        if (!IsEnabled)
        {
            ResetPresentation(scrim, panel);
            return;
        }
#if WINDOWS
        var frame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnRendering(object? sender, object args) => frame.TrySetResult();
        Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += OnRendering;
        using var registration = token.Register(() => frame.TrySetCanceled(token));
        try { await frame.Task; }
        finally { Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= OnRendering; }
        token.ThrowIfCancellationRequested();
        scrim.Opacity = panel.Opacity = 1;
        await AnimatePresentationAsync(scrim, panel, true, token);
#else
        panel.Scale = 0.97;
        await Task.WhenAll(scrim.FadeToAsync(1, 220, Easing.CubicOut),
            panel.FadeToAsync(1, 260, Easing.CubicOut), panel.ScaleToAsync(1, 260, Easing.CubicOut));
#endif
    }

    public static async Task HidePresentationAsync(VisualElement scrim, VisualElement panel, CancellationToken token)
    {
        if (!IsEnabled)
            return;
#if WINDOWS
        await AnimatePresentationAsync(scrim, panel, false, token);
#else
        scrim.CancelAnimations();
        panel.CancelAnimations();
        await Task.WhenAll(scrim.FadeToAsync(0, 160, Easing.CubicIn),
            panel.FadeToAsync(0, 160, Easing.CubicIn), panel.ScaleToAsync(0.98, 160, Easing.CubicIn));
#endif
    }

#if WINDOWS
    private static async Task AnimatePresentationAsync(VisualElement scrim, VisualElement panel, bool opening, CancellationToken token)
    {
        if (scrim.Handler?.PlatformView is not Microsoft.UI.Xaml.UIElement nativeScrim
            || panel.Handler?.PlatformView is not Microsoft.UI.Xaml.UIElement nativePanel)
            return;

        var background = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(nativeScrim);
        var card = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(nativePanel);
        var compositor = card.Compositor;
        using var batch = compositor.CreateScopedBatch(Microsoft.UI.Composition.CompositionBatchTypes.Animation);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnCompleted(object sender, Microsoft.UI.Composition.CompositionBatchCompletedEventArgs args) => finished.TrySetResult();
        batch.Completed += OnCompleted;
        using var registration = token.Register(() => finished.TrySetCanceled(token));
        try
        {
            var duration = TimeSpan.FromMilliseconds(opening ? 260 : 160);
            var easing = compositor.CreateCubicBezierEasingFunction(new(0.2f, 0), new(0, 1));
            card.CenterPoint = new((float)panel.Width / 2, (float)panel.Height / 2, 0);
            if (opening)
            {
                background.Opacity = card.Opacity = 0;
                card.Scale = new(0.97f, 0.97f, 1);
            }
            foreach (var visual in new[] { background, card })
            {
                var fade = compositor.CreateScalarKeyFrameAnimation();
                fade.Duration = duration;
                fade.InsertExpressionKeyFrame(0, "this.StartingValue");
                fade.InsertKeyFrame(1, opening ? 1 : 0, easing);
                visual.StartAnimation("Opacity", fade);
                visual.Opacity = opening ? 1 : 0;
            }
            var scale = compositor.CreateVector3KeyFrameAnimation();
            scale.Duration = duration;
            scale.InsertExpressionKeyFrame(0, "this.StartingValue");
            scale.InsertKeyFrame(1, opening ? System.Numerics.Vector3.One : new(0.98f, 0.98f, 1), easing);
            card.StartAnimation("Scale", scale);
            card.Scale = opening ? System.Numerics.Vector3.One : new(0.98f, 0.98f, 1);
            batch.End();
            await finished.Task;
        }
        finally
        {
            batch.Completed -= OnCompleted;
        }
    }
#endif

    public static bool IsEnabled
    {
        get
        {
#if WINDOWS
            return new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
#else
            return true;
#endif
        }
    }

    public static async Task SetCompletedAsync(CheckBox checkbox, VisualElement? summary, bool completed)
    {
        if (!IsEnabled)
            return;

        checkbox.CancelAnimations();
        summary?.CancelAnimations();
        if (summary is not null)
            summary.Opacity = completed ? 1 : 0.55;
        var fade = summary?.FadeToAsync(completed ? 0.55 : 1, 160, Easing.CubicOut) ?? Task.FromResult(false);
        if (completed)
        {
            await checkbox.ScaleToAsync(1.12, 70, Easing.CubicOut);
            await checkbox.ScaleToAsync(1, 90, Easing.CubicOut);
        }
        await fade;
        checkbox.Scale = 1;
    }
}
