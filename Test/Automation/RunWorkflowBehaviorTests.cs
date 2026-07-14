using OneTouchAutomation.Services.Automation.Behavior;
using OneTouchAutomation.Services.Automation.Workflows;

namespace Test.Automation;

public sealed class RunWorkflowBehaviorTests
{
    [Fact]
    public async Task ExecuteAsync_LoadsAndRunsSavedWorkflow()
    {
        var executed = false;
        var registry = new BehaviorRegistry([new TestBehavior(() => executed = true)]);
        var workflow = new WorkflowDefinition
        {
            Id = "daily-task",
            Name = "Daily task",
            StartNodeId = "action",
            Nodes =
            [
                new WorkflowNodeDefinition
                {
                    Id = "action",
                    Name = "Action",
                    BehaviorId = "test",
                    Parameters = new object()
                }
            ]
        };
        var provider = new TestServiceProvider(
            new TestWorkflowStore([workflow]),
            new WorkflowRunner(registry));
        var behavior = new RunWorkflowBehavior(provider);

        var result = await behavior.ExecuteAsync(
            new BehaviorExecutionContext
            {
                WindowHandle = IntPtr.Zero,
                Log = _ => { }
            },
            new RunWorkflowBehaviorParameters { WorkflowId = "daily-task" },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(executed);
        Assert.Equal("Workflow completed: Daily task.", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailureWhenWorkflowDoesNotExist()
    {
        var provider = new TestServiceProvider(
            new TestWorkflowStore([]),
            new WorkflowRunner(new BehaviorRegistry([])));
        var behavior = new RunWorkflowBehavior(provider);

        var result = await behavior.ExecuteAsync(
            new BehaviorExecutionContext
            {
                WindowHandle = IntPtr.Zero,
                Log = _ => { }
            },
            new RunWorkflowBehaviorParameters { WorkflowId = "missing" },
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("Workflow not found: missing.", result.Message);
    }

    private sealed class TestWorkflowStore : IWorkflowConfigurationStore
    {
        private readonly IReadOnlyList<WorkflowDefinition> _workflows;

        public TestWorkflowStore(IReadOnlyList<WorkflowDefinition> workflows)
        {
            _workflows = workflows;
        }

        public Task<IReadOnlyList<WorkflowDefinition>> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_workflows);

        public Task SaveAsync(
            IReadOnlyCollection<WorkflowDefinition> workflows,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        private readonly IWorkflowConfigurationStore _store;
        private readonly IWorkflowRunner _runner;

        public TestServiceProvider(IWorkflowConfigurationStore store, IWorkflowRunner runner)
        {
            _store = store;
            _runner = runner;
        }

        public object? GetService(Type serviceType)
        {
            return serviceType == typeof(IWorkflowConfigurationStore)
                ? _store
                : serviceType == typeof(IWorkflowRunner)
                    ? _runner
                    : null;
        }
    }

    private sealed class TestBehavior : IAutomationBehavior
    {
        private readonly Action _onExecute;

        public TestBehavior(Action onExecute)
        {
            _onExecute = onExecute;
        }

        public string Id => "test";

        public string Name => "Test";

        public string Description => "Test behavior.";

        public Type ParameterType => typeof(object);

        public Task<BehaviorExecutionResult> ExecuteAsync(
            BehaviorExecutionContext context,
            object parameters,
            CancellationToken cancellationToken = default)
        {
            _onExecute();
            return Task.FromResult(new BehaviorExecutionResult
            {
                IsSuccess = true,
                Message = "Completed."
            });
        }
    }
}
