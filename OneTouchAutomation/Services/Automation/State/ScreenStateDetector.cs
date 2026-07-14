using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Vision;

namespace OneTouchAutomation.Services.Automation.State;

public sealed class ScreenStateDetector : IScreenStateDetector
{
    private readonly IScreenRecognitionService _screenRecognitionService;
    private readonly IGameStateStore _stateStore;

    public ScreenStateDetector(
        IScreenRecognitionService screenRecognitionService,
        IGameStateStore stateStore)
    {
        _screenRecognitionService = screenRecognitionService;
        _stateStore = stateStore;
    }

    public async Task<IReadOnlyList<GameStateRecord>> DetectAsync(
        CapturedFrame frame,
        IReadOnlyCollection<ScreenDefinition> screens,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(screens);

        var records = new List<GameStateRecord>();

        foreach (var screen in screens)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _screenRecognitionService.RecognizeAsync(frame.PngBytes, screen, cancellationToken);

            if (!result.IsMatch)
            {
                continue;
            }

            var record = new GameStateRecord
            {
                StateId = result.ScreenId,
                Confidence = result.RequiredMatches.Count == 0
                    ? 1
                    : result.RequiredMatches.Min(match => match.MatchScore),
                ObservedAt = frame.CapturedAt,
                SourceFrameId = frame.SourceName,
            };

            _stateStore.Record(record);
            records.Add(record);
        }

        return records;
    }
}
