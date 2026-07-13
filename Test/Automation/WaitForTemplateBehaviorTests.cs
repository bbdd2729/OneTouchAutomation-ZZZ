using OneTouchAutomation.Services.Automation.Behavior;
using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Vision;
using OpenCvSharp;

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
            },
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("Template file not found.", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailure_WhenTimeoutOrPollIntervalIsInvalid()
    {
        var directory = Path.Combine(Path.GetTempPath(), "OneTouchAutomationTests", Guid.NewGuid().ToString("N"));
        var templatePath = Path.Combine(directory, "template.png");
        Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(templatePath, [], TestContext.Current.CancellationToken);

        try
        {
            var behavior = new WaitForTemplateBehavior(null!, null!);
            var result = await behavior.ExecuteAsync(
                CreateContext(),
                new WaitForTemplateBehaviorParameters
                {
                    TemplatePath = templatePath,
                    TimeoutSeconds = 0
                },
                TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccess);
            Assert.Equal("Timeout and poll interval must be greater than zero.", result.Message);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccessWhenTemplateIsDetected()
    {
        var templatePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(templatePath, [1, 2, 3], TestContext.Current.CancellationToken);

        try
        {
            var frame = new CapturedFrame
            {
                PngBytes = [1, 2, 3],
                Width = 500,
                Height = 400,
                CapturedAt = DateTimeOffset.UtcNow,
                SourceX = 50,
                SourceY = 60
            };
            var capture = new StubScreenCaptureService { Frame = frame };
            var vision = new StubVisionDebugService
            {
                Result = new TemplateMatchResult
                {
                    IsMatch = true,
                    MatchScore = 0.88,
                    MatchBounds = new Rect(30, 40, 20, 10)
                }
            };
            var logs = new List<string>();
            var behavior = new WaitForTemplateBehavior(capture, vision);

            var result = await behavior.ExecuteAsync(
                new BehaviorExecutionContext
                {
                    WindowHandle = (IntPtr)456,
                    Log = logs.Add
                },
                new WaitForTemplateBehaviorParameters
                {
                    TemplatePath = templatePath,
                    TimeoutSeconds = 2,
                    PollIntervalMilliseconds = 10
                },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal("Template detected.", result.Message);
            Assert.Equal(90, result.ScreenX);
            Assert.Equal(105, result.ScreenY);
            Assert.Equal(1, capture.ClientCaptureCount);
            Assert.Single(vision.Requests);
            Assert.Contains(logs, message => message.StartsWith("Template detected after 1"));
        }
        finally
        {
            File.Delete(templatePath);
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
