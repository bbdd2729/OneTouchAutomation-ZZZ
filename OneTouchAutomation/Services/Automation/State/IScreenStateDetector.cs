using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Vision;

namespace OneTouchAutomation.Services.Automation.State;

public interface IScreenStateDetector
{
    Task<IReadOnlyList<GameStateRecord>> DetectAsync(
        CapturedFrame frame,
        IReadOnlyCollection<ScreenDefinition> screens,
        CancellationToken cancellationToken = default);
}
