using System.Runtime.CompilerServices;
using OneTouchAutomation.Services.Automation.State;
using OneTouchAutomation.Services.Automation.Workflows;
using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Vision;

namespace Test.Automation;

public sealed class SceneAutomationRuntimeTests
{
    [Fact]
    public async Task RunAsync_ProcessesFramesAndReturnsCountersWhenCancelled()
    {
        using var cancellationSource = new CancellationTokenSource();
        var stateStore = new InMemoryGameStateStore();
        var runtime = new SceneAutomationRuntime(
            new OneFrameCaptureService(cancellationSource),
            new RecordingDetector(),
            stateStore,
            new RecordingOrchestrator());

        var result = await runtime.RunAsync(
            new SceneAutomationRequest
            {
                WindowHandle = (IntPtr)7,
                CaptureRequest = new ContinuousCaptureRequest { FramesPerSecond = 1 },
                Screens = [new ScreenDefinition { Id = "Battle", Name = "Battle" }],
                Scenes = [new AutomationSceneDefinition { Id = "battle", Name = "Battle", WorkflowId = "workflow" }]
            },
            cancellationToken: cancellationSource.Token);

        Assert.True(result.IsCancelled);
        Assert.Equal(1, result.ProcessedFrameCount);
        Assert.Equal(1, result.DetectedStateCount);
        Assert.Equal(1, result.ExecutedSceneCount);
    }

    private sealed class OneFrameCaptureService(CancellationTokenSource cancellationSource) : IContinuousCaptureService
    {
        public async IAsyncEnumerable<CapturedFrame> CaptureFramesAsync(
            ContinuousCaptureRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new CapturedFrame
            {
                PngBytes = [1],
                Width = 1,
                Height = 1,
                CapturedAt = DateTimeOffset.UtcNow
            };

            cancellationSource.Cancel();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }

    private sealed class RecordingDetector : IScreenStateDetector
    {
        public Task<IReadOnlyList<GameStateRecord>> DetectAsync(CapturedFrame frame, IReadOnlyCollection<ScreenDefinition> screens, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GameStateRecord>>(
            [
                new GameStateRecord
                {
                    StateId = "Battle",
                    Confidence = 0.9,
                    ObservedAt = frame.CapturedAt
                }
            ]);
    }

    private sealed class RecordingOrchestrator : ISceneOrchestrator
    {
        public Task<SceneRunResult> RunNextAsync(IntPtr windowHandle, IReadOnlyCollection<AutomationSceneDefinition> scenes, Action<string>? log = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SceneRunResult
            {
                Scene = scenes.Single(),
                WorkflowResult = new WorkflowRunResult()
            });
    }
}
