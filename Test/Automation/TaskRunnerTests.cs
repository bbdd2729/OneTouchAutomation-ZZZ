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
            ]);

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
            ]);

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
            [CreateTask("task-throws", "throws")]);

        Assert.False(result.IsSuccess);
        var taskResult = Assert.Single(result.TaskResults);
        Assert.Equal("Unhandled behavior error: Expected test exception.", taskResult.BehaviorResult.Message);
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
}
