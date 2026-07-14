using OneTouchAutomation.Services.Automation.Behavior;
using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Vision;
using OpenCvSharp;

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
            },
            TestContext.Current.CancellationToken);

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

    [Fact]
    public async Task ExecuteAsync_ClicksMatchedTemplateCenterAndReportsScreenCoordinates()
    {
        var templatePath = await CreateTemplateFileAsync();

        try
        {
            var frame = new CapturedFrame
            {
                PngBytes = [10, 20, 30],
                Width = 800,
                Height = 600,
                CapturedAt = DateTimeOffset.UtcNow,
                SourceX = 100,
                SourceY = 200
            };
            var capture = new StubScreenCaptureService { Frame = frame };
            var vision = new StubVisionDebugService
            {
                Result = new TemplateMatchResult
                {
                    IsMatch = true,
                    MatchScore = 0.92,
                    MatchBounds = new Rect(10, 20, 30, 40),
                    Message = "Matched."
                }
            };
            var input = new RecordingInputService();
            var logs = new List<string>();
            var behavior = new ClickTemplateBehavior(capture, vision, input);

            var result = await behavior.ExecuteAsync(
                new BehaviorExecutionContext
                {
                    WindowHandle = (IntPtr)123,
                    Log = logs.Add
                },
                new ClickTemplateBehaviorParameters
                {
                    TemplatePath = templatePath,
                    Threshold = 0.9,
                    UseRegion = true,
                    RegionX = 5,
                    RegionY = 6,
                    RegionWidth = 200,
                    RegionHeight = 100,
                    ClickOffsetX = 7,
                    ClickOffsetY = -3,
                    ClickMode = OneTouchAutomation.Services.Input.MouseClickMode.Repeat,
                    ClickRepeatCount = 3,
                    ClickIntervalMilliseconds = 120,
                    ClickHoldDurationMilliseconds = 80
                },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(132, result.ScreenX);
            Assert.Equal(237, result.ScreenY);
            Assert.Equal(1, capture.ClientCaptureCount);
            Assert.Single(vision.Requests);
            Assert.Equal(0.9, vision.Requests[0].Threshold);
            Assert.True(vision.Requests[0].UseRegion);
            Assert.Same(frame, input.ClickFrame);
            Assert.Same(vision.Result, input.ClickResult);
            Assert.NotNull(input.ClickOptions);
            Assert.Equal(OneTouchAutomation.Services.Input.MouseClickMode.Repeat, input.ClickOptions.Mode);
            Assert.Equal(3, input.ClickOptions.RepeatCount);
            Assert.Equal(7, input.ClickOptions.OffsetX);
            Assert.Equal(-3, input.ClickOptions.OffsetY);
            Assert.Contains(logs, message => message.StartsWith("Matched:"));
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

    private static async Task<string> CreateTemplateFileAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(path, [1, 2, 3], TestContext.Current.CancellationToken);
        return path;
    }
}
