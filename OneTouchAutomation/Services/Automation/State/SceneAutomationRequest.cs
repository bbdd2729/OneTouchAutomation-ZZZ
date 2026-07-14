using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Vision;

namespace OneTouchAutomation.Services.Automation.State;

public sealed class SceneAutomationRequest
{
    public required IntPtr WindowHandle { get; init; }

    public required ContinuousCaptureRequest CaptureRequest { get; init; }

    public IReadOnlyCollection<ScreenDefinition> Screens { get; init; } = [];

    public IReadOnlyCollection<AutomationSceneDefinition> Scenes { get; init; } = [];

    public TimeSpan StateMaximumAge { get; init; } = TimeSpan.FromSeconds(2);
}
