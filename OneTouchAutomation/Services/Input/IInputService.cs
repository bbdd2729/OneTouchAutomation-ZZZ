using System.Threading;
using System.Threading.Tasks;
using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Vision;

namespace OneTouchAutomation.Services.Input;

public interface IInputService
{
    Task MoveMouseAsync
    (
        int screenX,
        int screenY,
        CancellationToken cancellationToken = default);

    Task ClickAsync
    (
        int screenX,
        int screenY,
        CancellationToken cancellationToken = default);

    Task ClickAsync
    (
        int screenX,
        int screenY,
        MouseClickOptions options,
        CancellationToken cancellationToken = default);

    Task PressKeyAsync
    (
        AutomationKey key,
        CancellationToken cancellationToken = default);

    Task ClickMatchCenterAsync
    (
        CapturedFrame frame,
        TemplateMatchResult result,
        CancellationToken cancellationToken = default);

    Task ClickMatchAsync
    (
        CapturedFrame frame,
        TemplateMatchResult result,
        MouseClickOptions options,
        CancellationToken cancellationToken = default);
}
