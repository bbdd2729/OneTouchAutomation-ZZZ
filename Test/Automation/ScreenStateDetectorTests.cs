using OneTouchAutomation.Services.Automation.State;
using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Vision;

namespace Test.Automation;

public sealed class ScreenStateDetectorTests
{
    [Fact]
    public async Task DetectAsync_RecordsOnlyMatchedScreensWithTheLowestRequiredScore()
    {
        var store = new InMemoryGameStateStore();
        var detector = new ScreenStateDetector(new TestRecognitionService(), store);
        var matched = CreateScreen("Battle");
        var unmatched = CreateScreen("Loading");

        var records = await detector.DetectAsync(CreateFrame(), [matched, unmatched], TestContext.Current.CancellationToken);

        var record = Assert.Single(records);
        Assert.Equal("Battle", record.StateId);
        Assert.Equal(0.82, record.Confidence);
        Assert.Equal(record, store.GetLatest("Battle"));
        Assert.Null(store.GetLatest("Loading"));
    }

    private static CapturedFrame CreateFrame() => new()
    {
        PngBytes = [1, 2, 3],
        Width = 10,
        Height = 10,
        CapturedAt = DateTimeOffset.UtcNow,
        SourceName = "test-frame",
    };

    private static ScreenDefinition CreateScreen(string id) => new() { Id = id, Name = id };

    private sealed class TestRecognitionService : IScreenRecognitionService
    {
        public Task<ScreenRecognitionResult> RecognizeAsync(byte[] sourceBytes, ScreenDefinition screen, CancellationToken cancellationToken = default)
        {
            var isMatch = screen.Id == "Battle";
            return Task.FromResult(new ScreenRecognitionResult
            {
                ScreenId = screen.Id,
                IsMatch = isMatch,
                StatusCode = screen.Id,
                Message = screen.Id,
                RequiredMatches = isMatch
                    ? [new TemplateMatchResult { IsMatch = true, MatchScore = 0.91 }, new TemplateMatchResult { IsMatch = true, MatchScore = 0.82 }]
                    : []
            });
        }
    }
}
