using OneTouchAutomation.Services.Automation.Behavior;

namespace Test.Automation;

public class WaitForTemplateBehaviorTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsFailure_WhenTemplateFileDoesNotExist()
    {
        var behavior = new WaitForTemplateBehavior(null!, null!);

        var result = await behavior.ExecuteAsync(
            CreateContext(),
            new WaitForTemplateBehaviorParameters
            {
                TemplatePath = "Z:\\missing-template.png"
            });

        Assert.False(result.IsSuccess);
        Assert.Equal("Template file not found.", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailure_WhenTimeoutOrPollIntervalIsInvalid()
    {
        var directory = Path.Combine(Path.GetTempPath(), "OneTouchAutomationTests", Guid.NewGuid().ToString("N"));
        var templatePath = Path.Combine(directory, "template.png");
        Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(templatePath, []);

        try
        {
            var behavior = new WaitForTemplateBehavior(null!, null!);
            var result = await behavior.ExecuteAsync(
                CreateContext(),
                new WaitForTemplateBehaviorParameters
                {
                    TemplatePath = templatePath,
                    TimeoutSeconds = 0
                });

            Assert.False(result.IsSuccess);
            Assert.Equal("Timeout and poll interval must be greater than zero.", result.Message);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
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
