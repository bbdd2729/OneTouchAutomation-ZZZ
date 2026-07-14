namespace OneTouchAutomation.Services.Automation.State;

public sealed class SceneAutomationRunResult
{
    public int ProcessedFrameCount { get; init; }

    public int DetectedStateCount { get; init; }

    public int ExecutedSceneCount { get; init; }

    public bool IsCancelled { get; init; }
}
