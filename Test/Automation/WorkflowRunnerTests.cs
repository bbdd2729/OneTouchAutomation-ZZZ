using OneTouchAutomation.Services.Automation.Behavior;
using OneTouchAutomation.Services.Automation.Tasks;
using OneTouchAutomation.Services.Automation.Workflows;

namespace Test.Automation;

public sealed class WorkflowRunnerTests
{
    [Fact]
    public async Task RunAsync_ExecutesEnabledStepsInOrderAndActivatesWindow()
    {
        var executionOrder = new List<string>();
        var activation = new RecordingWindowActivationService();
        var runner = new WorkflowRunner(
            new BehaviorRegistry(
            [
                new TestWorkflowBehavior("first", executionOrder, true),
                new TestWorkflowBehavior("second", executionOrder, true)
            ]),
            activation);

        var result = await runner.RunAsync(
            (IntPtr)99,
            CreateWorkflow(
                CreateStep("first-step", "first"),
                new WorkflowStepDefinition
                {
                    Id = "disabled-step",
                    Name = "Disabled",
                    BehaviorId = "second",
                    Parameters = new object(),
                    IsEnabled = false
                },
                CreateStep("second-step", "second")),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(["first", "second"], executionOrder);
        Assert.Equal([(IntPtr)99], activation.ActivatedWindows);
        Assert.Equal(2, result.StepResults.Count);
    }

    [Fact]
    public async Task RunAsync_ContinuesAfterFailedStepWhenConfigured()
    {
        var executionOrder = new List<string>();
        var runner = new WorkflowRunner(new BehaviorRegistry(
        [
            new TestWorkflowBehavior("fails", executionOrder, false),
            new TestWorkflowBehavior("later", executionOrder, true)
        ]));

        var result = await runner.RunAsync(
            IntPtr.Zero,
            CreateWorkflow(
                new WorkflowStepDefinition
                {
                    Id = "failed-step",
                    Name = "Fail and continue",
                    BehaviorId = "fails",
                    Parameters = new object(),
                    FailurePolicy = TaskFailurePolicy.Continue
                },
                CreateStep("later-step", "later")),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(["fails", "later"], executionOrder);
        Assert.Equal(2, result.StepResults.Count);
    }

    [Fact]
    public async Task RunAsync_ReturnsCancelledResultWhenCancellationIsRequested()
    {
        var runner = new WorkflowRunner(new BehaviorRegistry([]));
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var result = await runner.RunAsync(
            IntPtr.Zero,
            CreateWorkflow(),
            cancellationToken: cancellationSource.Token);

        Assert.True(result.IsCancelled);
        Assert.Empty(result.StepResults);
    }

    [Fact]
    public async Task RunAsync_FollowsFailureTransitionAndCompletesRecoveryNode()
    {
        var executionOrder = new List<string>();
        var runner = new WorkflowRunner(new BehaviorRegistry(
        [
            new TestWorkflowBehavior("fails", executionOrder, false),
            new TestWorkflowBehavior("recover", executionOrder, true)
        ]));

        var result = await runner.RunAsync(
            IntPtr.Zero,
            new WorkflowDefinition
            {
                Id = "recovery-workflow",
                Name = "Recovery Workflow",
                StartNodeId = "check",
                Nodes =
                [
                    CreateNode("check", "fails"),
                    CreateNode("recover", "recover")
                ],
                Transitions =
                [
                    new WorkflowTransitionDefinition
                    {
                        FromNodeId = "check",
                        ToNodeId = "recover",
                        Outcome = WorkflowNodeOutcome.Failure
                    }
                ]
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(WorkflowNodeOutcome.Success, result.FinalOutcome);
        Assert.Equal(["fails", "recover"], executionOrder);
        Assert.Equal(2, result.StepResults.Count);
    }

    [Fact]
    public async Task RunAsync_StopsGraphWhenMaximumNodeExecutionsIsReached()
    {
        var executionOrder = new List<string>();
        var runner = new WorkflowRunner(new BehaviorRegistry(
        [new TestWorkflowBehavior("loop", executionOrder, true)]));

        var result = await runner.RunAsync(
            IntPtr.Zero,
            new WorkflowDefinition
            {
                Id = "loop-workflow",
                Name = "Loop Workflow",
                StartNodeId = "loop",
                MaxNodeExecutions = 2,
                Nodes = [CreateNode("loop", "loop")],
                Transitions =
                [
                    new WorkflowTransitionDefinition
                    {
                        FromNodeId = "loop",
                        ToNodeId = "loop",
                        Outcome = WorkflowNodeOutcome.Success
                    }
                ]
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(WorkflowNodeOutcome.Failure, result.FinalOutcome);
        Assert.Equal(["loop", "loop"], executionOrder);
    }

    [Fact]
    public async Task RunAsync_ReturnsFailureWithoutExecutionWhenGraphIsInvalid()
    {
        var executionOrder = new List<string>();
        var runner = new WorkflowRunner(new BehaviorRegistry(
        [new TestWorkflowBehavior("known", executionOrder, true)]));

        var result = await runner.RunAsync(
            IntPtr.Zero,
            new WorkflowDefinition
            {
                Id = "invalid-workflow",
                Name = "Invalid Workflow",
                StartNodeId = "missing",
                Nodes = [CreateNode("known-node", "known")]
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(WorkflowNodeOutcome.Failure, result.FinalOutcome);
        Assert.Empty(result.StepResults);
        Assert.Empty(executionOrder);
    }

    private static WorkflowDefinition CreateWorkflow(params WorkflowStepDefinition[] steps)
    {
        return new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = steps
        };
    }

    private static WorkflowStepDefinition CreateStep(string id, string behaviorId)
    {
        return new WorkflowStepDefinition
        {
            Id = id,
            Name = id,
            BehaviorId = behaviorId,
            Parameters = new object()
        };
    }

    private static WorkflowNodeDefinition CreateNode(string id, string behaviorId)
    {
        return new WorkflowNodeDefinition
        {
            Id = id,
            Name = id,
            BehaviorId = behaviorId,
            Parameters = new object()
        };
    }

    private sealed class TestWorkflowBehavior : IAutomationBehavior
    {
        private readonly ICollection<string> _executionOrder;
        private readonly bool _shouldSucceed;

        public TestWorkflowBehavior(string id, ICollection<string> executionOrder, bool shouldSucceed)
        {
            Id = id;
            _executionOrder = executionOrder;
            _shouldSucceed = shouldSucceed;
        }

        public string Id { get; }

        public string Name => Id;

        public string Description => "Test workflow behavior.";

        public Type ParameterType => typeof(object);

        public Task<BehaviorExecutionResult> ExecuteAsync(
            BehaviorExecutionContext context,
            object parameters,
            CancellationToken cancellationToken = default)
        {
            _executionOrder.Add(Id);

            return Task.FromResult(new BehaviorExecutionResult
            {
                IsSuccess = _shouldSucceed,
                Message = _shouldSucceed ? "Completed." : "Failed."
            });
        }
    }
}
