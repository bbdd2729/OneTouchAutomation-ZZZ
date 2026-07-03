using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Input;
using OneTouchAutomation.Services.Vision;

namespace OneTouchAutomation.Services.Automation.Behavior;

public class ClickTemplateBehavior : IAutomationBehavior
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

    public string? TemplatePath { get; set; }

    public double Threshold { get; set; } = 0.85;

    public bool UseRegion { get; set; }

    public int RegionX { get; set; }

    public int RegionY { get; set; }

    public int RegionWidth { get; set; } = 400;

    public int RegionHeight { get; set; } = 300;

    #region IAutomationBehavior Members

    public string Name => "Click Template";

    public string Description
        => "Capture selected window client area, match template, then click the best match center.";


    public async Task<BehaviorExecutionResult> ExecuteAsync
    (
            BehaviorExecutionContext context,
            CancellationToken cancellationToken = default(CancellationToken))
    {
        cancellationToken.ThrowIfCancellationRequested();

        if(string.IsNullOrWhiteSpace(TemplatePath) || !File.Exists(TemplatePath))
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

        var templateBytes = await File.ReadAllBytesAsync(TemplatePath, cancellationToken);

        context.Log("Running template match.");

        var matchResult = await _visionDebugService.MatchTemplateAsync
                (
                 new TemplateMatchRequest
                 {
                         SourceBytes   = frame.PngBytes,
                         TemplateBytes = templateBytes,
                         Threshold     = Threshold,
                         UseRegion     = UseRegion,
                         RegionX       = RegionX,
                         RegionY       = RegionY,
                         RegionWidth   = RegionWidth,
                         RegionHeight  = RegionHeight
                 },
                 cancellationToken);

        if(!matchResult.IsMatch)
        {
            return new BehaviorExecutionResult
            {
                    IsSuccess  = false,
                    Message    = matchResult.Message,
                    MatchScore = matchResult.MatchScore
            };
        }

        var bounds = matchResult.MatchBounds;

        var screenX = frame.SourceX + bounds.X + bounds.Width / 2;
        var screenY = frame.SourceY + bounds.Y + bounds.Height / 2;

        context.Log($"Matched: score={matchResult.MatchScore:0.000}, screen=({screenX},{screenY}).");
        context.Log("Clicking match center.");

        await _inputService.ClickMatchCenterAsync
                (
                 frame,
                 matchResult,
                 cancellationToken);

        return new BehaviorExecutionResult
        {
                IsSuccess  = true,
                Message    = "Clicked template match center.",
                MatchScore = matchResult.MatchScore,
                ScreenX    = screenX,
                ScreenY    = screenY
        };
    }

    #endregion
}