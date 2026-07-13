using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Vision;

namespace OneTouchAutomation.Services.Automation.Behavior;

public sealed class WaitForTemplateBehavior : IAutomationBehavior<WaitForTemplateBehaviorParameters>
{
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly IVisionDebugService _visionDebugService;

    public WaitForTemplateBehavior(
        IScreenCaptureService screenCaptureService,
        IVisionDebugService visionDebugService)
    {
        _screenCaptureService = screenCaptureService;
        _visionDebugService = visionDebugService;
    }

    public string Id => "wait-for-template";

    public string Name => "Wait For Template";

    public string Description => "Wait until a template appears in the selected window client area.";

    public Type ParameterType => typeof(WaitForTemplateBehaviorParameters);

    public Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        object parameters,
        CancellationToken cancellationToken = default)
    {
        if(parameters is not WaitForTemplateBehaviorParameters typedParameters)
        {
            return Task.FromResult(new BehaviorExecutionResult
            {
                IsSuccess = false,
                Message = $"Invalid parameters for behavior: {Name}."
            });
        }

        return ExecuteAsync(context, typedParameters, cancellationToken);
    }

    public async Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        WaitForTemplateBehaviorParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(parameters.TemplatePath) || !File.Exists(parameters.TemplatePath))
        {
            return new BehaviorExecutionResult
            {
                IsSuccess = false,
                Message = "Template file not found."
            };
        }

        if(parameters.TimeoutSeconds <= 0 || parameters.PollIntervalMilliseconds <= 0)
        {
            return new BehaviorExecutionResult
            {
                IsSuccess = false,
                Message = "Timeout and poll interval must be greater than zero."
            };
        }

        var templateBytes = await File.ReadAllBytesAsync(parameters.TemplatePath, cancellationToken);
        var deadline = DateTimeOffset.UtcNow.AddSeconds(parameters.TimeoutSeconds);
        var attempts = 0;

        context.Log($"Waiting up to {parameters.TimeoutSeconds} second(s) for template.");

        while(DateTimeOffset.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempts++;

            var frame = await _screenCaptureService.CaptureWindowClientAsync(
                context.WindowHandle,
                cancellationToken);

            var matchResult = await _visionDebugService.MatchTemplateAsync(
                new TemplateMatchRequest
                {
                    SourceBytes = frame.PngBytes,
                    TemplateBytes = templateBytes,
                    Threshold = parameters.Threshold,
                    UseRegion = parameters.UseRegion,
                    RegionX = parameters.RegionX,
                    RegionY = parameters.RegionY,
                    RegionWidth = parameters.RegionWidth,
                    RegionHeight = parameters.RegionHeight
                },
                cancellationToken);

            if(matchResult.IsMatch)
            {
                var bounds = matchResult.MatchBounds;
                var screenX = frame.SourceX + bounds.X + bounds.Width / 2;
                var screenY = frame.SourceY + bounds.Y + bounds.Height / 2;

                context.Log($"Template detected after {attempts} attempt(s). score={matchResult.MatchScore:0.000}.");

                return new BehaviorExecutionResult
                {
                    IsSuccess = true,
                    Message = "Template detected.",
                    MatchScore = matchResult.MatchScore,
                    ScreenX = screenX,
                    ScreenY = screenY
                };
            }

            var remaining = deadline - DateTimeOffset.UtcNow;

            if(remaining <= TimeSpan.Zero)
            {
                break;
            }

            await Task.Delay(
                remaining < TimeSpan.FromMilliseconds(parameters.PollIntervalMilliseconds)
                    ? remaining
                    : TimeSpan.FromMilliseconds(parameters.PollIntervalMilliseconds),
                cancellationToken);
        }

        return new BehaviorExecutionResult
        {
            IsSuccess = false,
            Message = $"Template was not detected within {parameters.TimeoutSeconds} second(s)."
        };
    }
}
