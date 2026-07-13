using OneTouchAutomation.Services.Automation.Behavior;

namespace Test.Automation;

public class DelayBehaviorTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsFailure_WhenDurationIsNotPositive()
    {
        var behavior = new DelayBehavior();

        var result = await behavior.ExecuteAsync(
            CreateContext(),
            new DelayBehaviorParameters { DurationMilliseconds = 0 },
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("Delay duration must be greater than zero.", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsWhenDelayIsCancelled()
    {
        var behavior = new DelayBehavior();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var action = () => behavior.ExecuteAsync(
            CreateContext(),
            new DelayBehaviorParameters { DurationMilliseconds = 100 },
            cancellationSource.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
    }

    private static BehaviorExecutionContext CreateContext()
    {
        return new BehaviorExecutionContext
        {
            WindowHandle = IntPtr.Zero,
            Log = _ => { }
        };
    }
}
