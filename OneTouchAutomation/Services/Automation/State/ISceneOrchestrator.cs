using OneTouchAutomation.Services.Automation.Workflows;

namespace OneTouchAutomation.Services.Automation.State;

public interface ISceneOrchestrator
{
    Task<SceneRunResult> RunNextAsync(
        IntPtr windowHandle,
        IReadOnlyCollection<AutomationSceneDefinition> scenes,
        Action<string>? log = null,
        CancellationToken cancellationToken = default);
}

public sealed class SceneRunResult
{
    public AutomationSceneDefinition? Scene { get; init; }

    public WorkflowRunResult? WorkflowResult { get; init; }

    public string Message { get; init; } = string.Empty;

    public bool WasScheduled => Scene is not null;
}
