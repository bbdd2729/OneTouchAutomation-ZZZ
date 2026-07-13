using OneTouchAutomation.Services.Automation.Behavior;
using OneTouchAutomation.Services.Input;

namespace Test.Automation;

public class PressKeyBehaviorTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsFailure_WhenKeyIsUnsupported()
    {
        var behavior = new PressKeyBehavior(null!, null!);

        var result = await behavior.ExecuteAsync(
            new BehaviorExecutionContext
            {
                WindowHandle = IntPtr.Zero,
                Log = _ => { }
            },
            new PressKeyBehaviorParameters { Key = (AutomationKey)0 },
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("Unsupported automation key.", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ActivatesTargetWindowAndSendsRequestedKey()
    {
        var input = new RecordingInputService();
        var activation = new RecordingWindowActivationService();
        var logs = new List<string>();
        var behavior = new PressKeyBehavior(input, activation);

        var result = await behavior.ExecuteAsync(
            new BehaviorExecutionContext
            {
                WindowHandle = (IntPtr)987,
                Log = logs.Add
            },
            new PressKeyBehaviorParameters { Key = AutomationKey.Enter },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Pressed key: Enter.", result.Message);
        Assert.Equal([(IntPtr)987], activation.ActivatedWindows);
        Assert.Equal([AutomationKey.Enter], input.PressedKeys);
        Assert.Equal(["Activating target window.", "Pressing key: Enter."], logs);
    }
}
