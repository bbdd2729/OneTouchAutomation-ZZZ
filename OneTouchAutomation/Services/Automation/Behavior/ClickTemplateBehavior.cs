using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Input;
using OneTouchAutomation.Services.Vision;

namespace OneTouchAutomation.Services.Automation.Behavior;

public class ClickTemplateBehavior : IAutomationBehavior<ClickTemplateBehaviorParameters>
{
    private readonly IInputService         _inputService;
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly IVisionDebugService   _visionDebugService;

    public ClickTemplateBehavior
    (
            IScreenCaptureService screenCaptureService,
            IVisionDebugService visionDebugService,
            IInputService inputService)
    {
        _screenCaptureService = screenCaptureService;
        _visionDebugService   = visionDebugService;
        _inputService         = inputService;
    }


    #region IAutomationBehavior Members

    public string Id => "click-template";

    public string Name => "Click Template";

    public string Description
        => "Capture selected window client area, match template, then click the best match center.";

    public Type ParameterType => typeof(ClickTemplateBehaviorParameters);

    public Task<BehaviorExecutionResult> ExecuteAsync
    (
            BehaviorExecutionContext context,
            object parameters,
            CancellationToken cancellationToken = default)
    {
        if(parameters is not ClickTemplateBehaviorParameters typedParameters)
        {
            return Task.FromResult
                (new BehaviorExecutionResult
                {
                        IsSuccess = false,
                        Message = $"Invalid parameters for behavior: {Name}."
                });
        }

        return ExecuteAsync(context, typedParameters, cancellationToken);
    }


    public async Task<BehaviorExecutionResult> ExecuteAsync
    (
            BehaviorExecutionContext context,
            ClickTemplateBehaviorParameters parameters,
            CancellationToken cancellationToken = default(CancellationToken))
    {
        cancellationToken.ThrowIfCancellationRequested();

        if(string.IsNullOrWhiteSpace(parameters.TemplatePath) || !File.Exists(parameters.TemplatePath))
        {
            return new BehaviorExecutionResult
            {
                    IsSuccess = false,
                    Message   = "Template file not found."
            };
        }

        context.Log("Capturing selected window client area.");

        var frame = await _screenCaptureService.CaptureWindowClientAsync
                (
                 context.WindowHandle,
                 cancellationToken);

        context.Log($"Captured: {frame.Width}x{frame.Height}, source=({frame.SourceX},{frame.SourceY}).");

        var templateBytes = await File.ReadAllBytesAsync(parameters.TemplatePath, cancellationToken);

        context.Log("Running template match.");

        var matchResult = await _visionDebugService.MatchTemplateAsync
                (
                 new TemplateMatchRequest
                 {
                         SourceBytes   = frame.PngBytes,
                         TemplateBytes = templateBytes,
                         Threshold     = parameters.Threshold,
                         UseRegion     = parameters.UseRegion,
                         RegionX       = parameters.RegionX,
                         RegionY       = parameters.RegionY,
                         RegionWidth   = parameters.RegionWidth,
                         RegionHeight  = parameters.RegionHeight
                 },
                 cancellationToken);

        if(!matchResult.IsMatch)
        {
            return new BehaviorExecutionResult
            {
                    IsSuccess  = !parameters.FailWhenNotFound,
                    StatusCode = "not-found",
                    Message    = matchResult.Message,
                    MatchScore = matchResult.MatchScore
            };
        }

        var bounds = matchResult.MatchBounds;

        var clickOptions = new MouseClickOptions
        {
            OffsetX = parameters.ClickOffsetX,
            OffsetY = parameters.ClickOffsetY,
            Mode = parameters.ClickMode,
            RepeatCount = parameters.ClickRepeatCount,
            IntervalMilliseconds = parameters.ClickIntervalMilliseconds,
            HoldDurationMilliseconds = parameters.ClickHoldDurationMilliseconds
        };

        var screenX = frame.SourceX + bounds.X + bounds.Width / 2 + clickOptions.OffsetX;
        var screenY = frame.SourceY + bounds.Y + bounds.Height / 2 + clickOptions.OffsetY;

        context.Log($"Matched: score={matchResult.MatchScore:0.000}, screen=({screenX},{screenY}).");
        context.Log($"Clicking match with {clickOptions.Mode} mode.");

        await _inputService.ClickMatchAsync
                (
                 frame,
                 matchResult,
                 clickOptions,
                 cancellationToken);

        return new BehaviorExecutionResult
        {
                IsSuccess  = true,
                StatusCode = "clicked",
                Message    = "Clicked template match.",
                MatchScore = matchResult.MatchScore,
                ScreenX    = screenX,
                ScreenY    = screenY
        };
    }

    #endregion
}
