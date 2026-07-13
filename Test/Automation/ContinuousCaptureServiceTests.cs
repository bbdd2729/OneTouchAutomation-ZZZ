using OneTouchAutomation.Services.Capture;

namespace Test.Automation;

public sealed class ContinuousCaptureServiceTests
{
    [Fact]
    public async Task CaptureFramesAsync_CapturesRequestedSourceUntilCancelled()
    {
        var captureService = new StubScreenCaptureService();
        var service = new ContinuousCaptureService(captureService);
        using var cancellationSource = new CancellationTokenSource();

        await using var enumerator = service.CaptureFramesAsync(
                new ContinuousCaptureRequest
                {
                    Source = ContinuousCaptureSource.Screen,
                    FramesPerSecond = 20
                },
                cancellationSource.Token)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        var hasFrame = await enumerator.MoveNextAsync();

        Assert.True(hasFrame);
        Assert.Same(captureService.Frame, enumerator.Current);
        Assert.Equal(1, captureService.ScreenCaptureCount);

        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await enumerator.MoveNextAsync().AsTask());
    }
}
