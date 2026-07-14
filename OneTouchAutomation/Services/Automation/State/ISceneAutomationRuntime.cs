namespace OneTouchAutomation.Services.Automation.State;

public interface ISceneAutomationRuntime
{
    Task<SceneAutomationRunResult> RunAsync(
        SceneAutomationRequest request,
        Action<string>? log = null,
        CancellationToken cancellationToken = default);
}
