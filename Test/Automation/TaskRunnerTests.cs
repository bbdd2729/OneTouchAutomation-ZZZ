using OneTouchAutomation.Services.Automation.Behavior;
using OneTouchAutomation.Services.Automation.Tasks;

namespace Test.Automation;

public class TaskRunnerTests
{
    [Fact]
    public async Task RunAsync_RunsEnabledTasksInOrderAndSkipsDisabledTasks()
    {
        var executionOrder = new List<string>();
        var registry = new BehaviorRegistry(
        [
            new TestBehavior("first", executionOrder, true),
            new TestBehavior("second", executionOrder, true)
        ]);
        var runner = new TaskRunner(registry);

        var result = await runner.RunAsync(
            IntPtr.Zero,
            [
                CreateTask("task-1", "first"),
                new AutomationTaskDefinition
                {
                    Id = "task-disabled",
                    Name = "Disabled task",
                    BehaviorId = "second",
                    Parameters = new object(),
                    IsEnabled = false
                },
                CreateTask("task-2", "second")
            ],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsCancelled);
        Assert.Equal(["first", "second"], executionOrder);
        Assert.Equal(2, result.TaskResults.Count);
    }

    [Fact]
    public async Task RunAsync_StopsAfterFirstFailedTask()
    {
        var executionOrder = new List<string>();
        var registry = new BehaviorRegistry(
        [
            new TestBehavior("fails", executionOrder, false),
            new TestBehavior("later", executionOrder, true)
        ]);
        var runner = new TaskRunner(registry);

        var result = await runner.RunAsync(
            IntPtr.Zero,
            [
                CreateTask("task-fails", "fails"),
                CreateTask("task-later", "later")
            ],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(["fails"], executionOrder);
        Assert.Single(result.TaskResults);
        Assert.False(result.TaskResults[0].BehaviorResult.IsSuccess);
    }

    [Fact]
    public async Task RunAsync_ConvertsBehaviorExceptionToFailedTaskResult()
    {
        var registry = new BehaviorRegistry([new ThrowingBehavior()]);
        var runner = new TaskRunner(registry);

        var result = await runner.RunAsync(
            IntPtr.Zero,
            [CreateTask("task-throws", "throws")],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        var taskResult = Assert.Single(result.TaskResults);
        Assert.Equal("Unhandled behavior error: Expected test exception.", taskResult.BehaviorResult.Message);
    }

    [Fact]
    public async Task RunAsync_RetriesFailedTaskUntilItSucceeds()
    {
        var behavior = new RetryBehavior();
        var runner = new TaskRunner(new BehaviorRegistry([behavior]));

        var result = await runner.RunAsync(
            IntPtr.Zero,
            [new AutomationTaskDefinition
            {
                Id = "task-retry",
                Name = "Retry task",
                BehaviorId = behavior.Id,
                Parameters = new object(),
                FailurePolicy = TaskFailurePolicy.Retry,
                MaxRetryCount = 1
            }],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, behavior.CallCount);
        Assert.Equal(2, Assert.Single(result.TaskResults).AttemptCount);
    }

    [Fact]
    public async Task RunAsync_ContinuesAfterFailedTask_WhenPolicyIsContinue()
    {
        var executionOrder = new List<string>();
        var runner = new TaskRunner(new BehaviorRegistry(
        [
            new TestBehavior("fails", executionOrder, false),
            new TestBehavior("later", executionOrder, true)
        ]));

        var result = await runner.RunAsync(
            IntPtr.Zero,
            [
                new AutomationTaskDefinition
                {
                    Id = "task-fails",
                    Name = "Fail and continue",
                    BehaviorId = "fails",
                    Parameters = new object(),
                    FailurePolicy = TaskFailurePolicy.Continue
                },
                CreateTask("task-later", "later")
            ],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(["fails", "later"], executionOrder);
        Assert.Equal(2, result.TaskResults.Count);
    }

    [Fact]
    public async Task RunAsync_StopsRetryingAfterConfiguredRetryLimit()
    {
        var behavior = new AlwaysFailBehavior();
        var runner = new TaskRunner(new BehaviorRegistry([behavior]));

        var result = await runner.RunAsync(
            IntPtr.Zero,
            [new AutomationTaskDefinition
            {
                Id = "task-retry-limit",
                Name = "Retry limit",
                BehaviorId = behavior.Id,
                Parameters = new object(),
                FailurePolicy = TaskFailurePolicy.Retry,
                MaxRetryCount = 2
            }],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(3, behavior.CallCount);
        Assert.Equal(3, Assert.Single(result.TaskResults).AttemptCount);
    }

    [Fact]
    public async Task RunAsync_ActivatesTargetWindowBeforeExecutingTasks()
    {
        var executionOrder = new List<string>();
        var activation = new RecordingWindowActivationService();
        var runner = new TaskRunner(
            new BehaviorRegistry([new TestBehavior("first", executionOrder, true)]),
            activation);

        var result = await runner.RunAsync(
            (IntPtr)2468,
            [CreateTask("task-1", "first")],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal([(IntPtr)2468], activation.ActivatedWindows);
        Assert.Equal(["first"], executionOrder);
    }

    [Fact]
    public async Task RunAsync_ReturnsFailureResult_WhenBehaviorIsNotRegistered()
    {
        var runner = new TaskRunner(new BehaviorRegistry([]));

        var result = await runner.RunAsync(
            IntPtr.Zero,
            [CreateTask("task-missing", "missing")],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        var taskResult = Assert.Single(result.TaskResults);
        Assert.Equal("Behavior not found: missing.", taskResult.BehaviorResult.Message);
        Assert.Equal(1, taskResult.AttemptCount);
    }

    [Fact]
    public async Task RunAsync_ReturnsCancelledResult_WhenCancellationIsAlreadyRequested()
    {
        var executionOrder = new List<string>();
        var runner = new TaskRunner(new BehaviorRegistry([new TestBehavior("first", executionOrder, true)]));
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var result = await runner.RunAsync(
            IntPtr.Zero,
            [CreateTask("task-1", "first")],
            cancellationToken: cancellationSource.Token);

        Assert.True(result.IsCancelled);
        Assert.Empty(result.TaskResults);
        Assert.Empty(executionOrder);
    }

    private static AutomationTaskDefinition CreateTask(string id, string behaviorId)
    {
        return new AutomationTaskDefinition
        {
            Id = id,
            Name = id,
            BehaviorId = behaviorId,
            Parameters = new object()
        };
    }

    private sealed class TestBehavior : IAutomationBehavior
    {
        private readonly ICollection<string> _executionOrder;
        private readonly bool _shouldSucceed;

        public TestBehavior(string id, ICollection<string> executionOrder, bool shouldSucceed)
        {
            Id = id;
            _executionOrder = executionOrder;
            _shouldSucceed = shouldSucceed;
        }

        public string Id { get; }

        public string Name => Id;

        public string Description => "Test behavior";

        public Type ParameterType => typeof(object);

        public Task<BehaviorExecutionResult> ExecuteAsync(
            BehaviorExecutionContext context,
            object parameters,
            CancellationToken cancellationToken = default)
        {
            _executionOrder.Add(Id);

            return Task.FromResult
                (new BehaviorExecutionResult
                {
                        IsSuccess = _shouldSucceed,
                        Message = _shouldSucceed ? "Completed." : "Failed."
                });
        }
    }

    private sealed class ThrowingBehavior : IAutomationBehavior
    {
        public string Id => "throws";

        public string Name => "Throws";

        public string Description => "Throws for testing.";

        public Type ParameterType => typeof(object);

        public Task<BehaviorExecutionResult> ExecuteAsync(
            BehaviorExecutionContext context,
            object parameters,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Expected test exception.");
        }
    }

    private sealed class RetryBehavior : IAutomationBehavior
    {
        public int CallCount { get; private set; }

        public string Id => "retry";

        public string Name => "Retry";

        public string Description => "Fails once, then succeeds.";

        public Type ParameterType => typeof(object);

        public Task<BehaviorExecutionResult> ExecuteAsync(
            BehaviorExecutionContext context,
            object parameters,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(new BehaviorExecutionResult
            {
                IsSuccess = CallCount > 1,
                Message = CallCount > 1 ? "Completed." : "Temporary failure."
            });
        }
    }

    private sealed class AlwaysFailBehavior : IAutomationBehavior
    {
        public int CallCount { get; private set; }

        public string Id => "always-fails";

        public string Name => "Always fails";

        public string Description => "Always fails for retry testing.";

        public Type ParameterType => typeof(object);

        public Task<BehaviorExecutionResult> ExecuteAsync(
            BehaviorExecutionContext context,
            object parameters,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new BehaviorExecutionResult
            {
                IsSuccess = false,
                Message = "Failed."
            });
        }
    }
}
