using OneTouchAutomation.Services.Automation.Workflows;

namespace OneTouchAutomation.Services.Automation.State;

public sealed class SceneOrchestrator : ISceneOrchestrator
{
    private readonly ISceneScheduler _sceneScheduler;
    private readonly IWorkflowConfigurationStore _workflowStore;
    private readonly IWorkflowRunner _workflowRunner;

    public SceneOrchestrator(
        ISceneScheduler sceneScheduler,
        IWorkflowConfigurationStore workflowStore,
        IWorkflowRunner workflowRunner)
    {
        _sceneScheduler = sceneScheduler;
        _workflowStore = workflowStore;
        _workflowRunner = workflowRunner;
    }

    public async Task<SceneRunResult> RunNextAsync(
        IntPtr windowHandle,
        IReadOnlyCollection<AutomationSceneDefinition> scenes,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenes);
        cancellationToken.ThrowIfCancellationRequested();

        var scene = _sceneScheduler.SelectNext(scenes);

        if (scene is null)
        {
            return new SceneRunResult { Message = "No eligible scene." };
        }

        var workflows = await _workflowStore.LoadAsync(cancellationToken);
        var workflow = workflows.FirstOrDefault(candidate => candidate.Id == scene.WorkflowId);

        if (workflow is null)
        {
            var message = $"Workflow not found for scene '{scene.Name}': {scene.WorkflowId}.";
            log?.Invoke(message);
            return new SceneRunResult { Scene = scene, Message = message };
        }

        _sceneScheduler.MarkExecuted(scene);
        log?.Invoke($"[{scene.Name}] Starting workflow: {workflow.Name}.");

        var result = await _workflowRunner.RunAsync(windowHandle, workflow, log, cancellationToken);

        return new SceneRunResult
        {
            Scene = scene,
            WorkflowResult = result,
            Message = result.IsSuccess ? "Scene completed." : "Scene workflow did not complete successfully."
        };
    }
}
