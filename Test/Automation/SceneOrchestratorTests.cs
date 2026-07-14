using OneTouchAutomation.Services.Automation.State;
using OneTouchAutomation.Services.Automation.Workflows;

namespace Test.Automation;

public sealed class SceneOrchestratorTests
{
    [Fact]
    public async Task RunNextAsync_ExecutesTheWorkflowForTheSelectedScene()
    {
        var scene = CreateScene("daily", "daily-workflow");
        var workflow = CreateWorkflow("daily-workflow");
        var runner = new RecordingWorkflowRunner();
        var orchestrator = new SceneOrchestrator(
            new FixedSceneScheduler(scene),
            new FixedWorkflowStore([workflow]),
            runner);

        var result = await orchestrator.RunNextAsync((IntPtr)42, [scene], cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.WasScheduled);
        Assert.Equal(scene, result.Scene);
        Assert.Equal(workflow, runner.Workflow);
        Assert.Equal((IntPtr)42, runner.WindowHandle);
    }

    [Fact]
    public async Task RunNextAsync_DoesNotExecuteWhenNoSceneIsEligible()
    {
        var runner = new RecordingWorkflowRunner();
        var orchestrator = new SceneOrchestrator(
            new FixedSceneScheduler(null),
            new FixedWorkflowStore([]),
            runner);

        var result = await orchestrator.RunNextAsync(IntPtr.Zero, [], cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.WasScheduled);
        Assert.Null(runner.Workflow);
    }

    [Fact]
    public async Task RunNextAsync_ReportsMissingWorkflowWithoutExecuting()
    {
        var scene = CreateScene("daily", "missing-workflow");
        var runner = new RecordingWorkflowRunner();
        var orchestrator = new SceneOrchestrator(
            new FixedSceneScheduler(scene),
            new FixedWorkflowStore([]),
            runner);

        var result = await orchestrator.RunNextAsync(IntPtr.Zero, [scene], cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.WasScheduled);
        Assert.Null(result.WorkflowResult);
        Assert.Null(runner.Workflow);
    }

    private static AutomationSceneDefinition CreateScene(string id, string workflowId) => new()
    {
        Id = id,
        Name = id,
        WorkflowId = workflowId,
    };

    private static WorkflowDefinition CreateWorkflow(string id) => new()
    {
        Id = id,
        Name = id,
    };

    private sealed class FixedSceneScheduler(AutomationSceneDefinition? scene) : ISceneScheduler
    {
        public AutomationSceneDefinition? SelectNext(IReadOnlyCollection<AutomationSceneDefinition> scenes) => scene;

        public void MarkExecuted(AutomationSceneDefinition scene) { }
    }

    private sealed class FixedWorkflowStore(IReadOnlyList<WorkflowDefinition> workflows) : IWorkflowConfigurationStore
    {
        public Task<IReadOnlyList<WorkflowDefinition>> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(workflows);

        public Task SaveAsync(IReadOnlyCollection<WorkflowDefinition> workflows, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingWorkflowRunner : IWorkflowRunner
    {
        public IntPtr WindowHandle { get; private set; }

        public WorkflowDefinition? Workflow { get; private set; }

        public Task<WorkflowRunResult> RunAsync(IntPtr windowHandle, WorkflowDefinition workflow, Action<string>? log = null, CancellationToken cancellationToken = default)
        {
            WindowHandle = windowHandle;
            Workflow = workflow;
            return Task.FromResult(new WorkflowRunResult());
        }
    }
}
