using OneTouchAutomation.Services.Automation.Behavior;

namespace Test.Automation;

public class ClickTemplateBehaviorTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsFailure_WhenTemplateFileDoesNotExist()
    {
        var behavior = new ClickTemplateBehavior(null!, null!, null!);

        var result = await behavior.ExecuteAsync(
            CreateContext(),
            new ClickTemplateBehaviorParameters
            {
                TemplatePath = "Z:\\missing-template.png"
            });

        Assert.False(result.IsSuccess);
        Assert.Equal("Template file not found.", result.Message);
        Assert.Equal(0, result.MatchScore);
        Assert.Null(result.ScreenX);
        Assert.Null(result.ScreenY);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsWhenCancellationIsRequested()
    {
        var behavior = new ClickTemplateBehavior(null!, null!, null!);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var action = () => behavior.ExecuteAsync(
            CreateContext(),
            new ClickTemplateBehaviorParameters
            {
                TemplatePath = "Z:\\missing-template.png"
            },
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
