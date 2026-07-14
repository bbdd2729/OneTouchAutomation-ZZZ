using OneTouchAutomation.Services.Capture;

namespace OneTouchAutomation.Services.Automation.State;

public sealed class SceneAutomationRuntime : ISceneAutomationRuntime
{
    private readonly IContinuousCaptureService _continuousCaptureService;
    private readonly IScreenStateDetector _screenStateDetector;
    private readonly IGameStateStore _stateStore;
    private readonly ISceneOrchestrator _sceneOrchestrator;

    public SceneAutomationRuntime(
        IContinuousCaptureService continuousCaptureService,
        IScreenStateDetector screenStateDetector,
        IGameStateStore stateStore,
        ISceneOrchestrator sceneOrchestrator)
    {
        _continuousCaptureService = continuousCaptureService;
        _screenStateDetector = screenStateDetector;
        _stateStore = stateStore;
        _sceneOrchestrator = sceneOrchestrator;
    }

    public async Task<SceneAutomationRunResult> RunAsync(
        SceneAutomationRequest request,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.StateMaximumAge < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(request.StateMaximumAge));
        }

        var processedFrameCount = 0;
        var detectedStateCount = 0;
        var executedSceneCount = 0;

        try
        {
            await foreach (var frame in _continuousCaptureService
                               .CaptureFramesAsync(request.CaptureRequest, cancellationToken)
                               .WithCancellation(cancellationToken))
            {
                processedFrameCount++;
                var detectedStates = await _screenStateDetector.DetectAsync(frame, request.Screens, cancellationToken);
                detectedStateCount += detectedStates.Count;
                _stateStore.Expire(request.StateMaximumAge);

                var sceneResult = await _sceneOrchestrator.RunNextAsync(
                    request.WindowHandle,
                    request.Scenes,
                    log,
                    cancellationToken);

                if (sceneResult.WorkflowResult is not null)
                {
                    executedSceneCount++;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            log?.Invoke("Scene automation cancelled.");
            return new SceneAutomationRunResult
            {
                ProcessedFrameCount = processedFrameCount,
                DetectedStateCount = detectedStateCount,
                ExecutedSceneCount = executedSceneCount,
                IsCancelled = true,
            };
        }

        return new SceneAutomationRunResult
        {
            ProcessedFrameCount = processedFrameCount,
            DetectedStateCount = detectedStateCount,
            ExecutedSceneCount = executedSceneCount,
        };
    }
}
