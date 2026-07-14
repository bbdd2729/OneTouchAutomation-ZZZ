using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Vision;

namespace OneTouchAutomation.Services.Automation.Behavior;

public sealed class WaitForTemplateDisappearBehavior : IAutomationBehavior<WaitForTemplateDisappearBehaviorParameters>
{
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly IVisionDebugService _visionService;

    public WaitForTemplateDisappearBehavior(IScreenCaptureService screenCaptureService, IVisionDebugService visionService)
    {
        _screenCaptureService = screenCaptureService;
        _visionService = visionService;
    }

    public string Id => "wait-for-template-disappear";

    public string Name => "Wait For Template Disappear";

    public string Description => "Wait until a template is no longer visible in the selected window.";

    public Type ParameterType => typeof(WaitForTemplateDisappearBehaviorParameters);

    public Task<BehaviorExecutionResult> ExecuteAsync(BehaviorExecutionContext context, object parameters, CancellationToken cancellationToken = default)
    {
        return parameters is WaitForTemplateDisappearBehaviorParameters typedParameters
            ? ExecuteAsync(context, typedParameters, cancellationToken)
            : Task.FromResult(new BehaviorExecutionResult { IsSuccess = false, Message = $"Invalid parameters for behavior: {Name}." });
    }

    public async Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        WaitForTemplateDisappearBehaviorParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(parameters.TemplatePath) || !File.Exists(parameters.TemplatePath))
        {
            return new BehaviorExecutionResult { IsSuccess = false, Message = "Template file not found." };
        }

        if(parameters.TimeoutSeconds <= 0 || parameters.PollIntervalMilliseconds <= 0)
        {
            return new BehaviorExecutionResult { IsSuccess = false, Message = "Timeout and poll interval must be greater than zero." };
        }

        var templateBytes = await File.ReadAllBytesAsync(parameters.TemplatePath, cancellationToken);
        var deadline = DateTimeOffset.UtcNow.AddSeconds(parameters.TimeoutSeconds);
        while(DateTimeOffset.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var frame = await _screenCaptureService.CaptureWindowClientAsync(context.WindowHandle, cancellationToken);
            var match = await _visionService.MatchTemplateAsync(new TemplateMatchRequest
            {
                SourceBytes = frame.PngBytes,
                TemplateBytes = templateBytes,
                Threshold = parameters.Threshold,
                UseRegion = parameters.UseRegion,
                RegionX = parameters.RegionX,
                RegionY = parameters.RegionY,
                RegionWidth = parameters.RegionWidth,
                RegionHeight = parameters.RegionHeight
            }, cancellationToken);

            if(!match.IsMatch)
            {
                return new BehaviorExecutionResult { IsSuccess = true, StatusCode = "disappeared", Message = "Template disappeared." };
            }

            var remaining = deadline - DateTimeOffset.UtcNow;
            if(remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining < TimeSpan.FromMilliseconds(parameters.PollIntervalMilliseconds)
                    ? remaining
                    : TimeSpan.FromMilliseconds(parameters.PollIntervalMilliseconds), cancellationToken);
            }
        }

        return new BehaviorExecutionResult { IsSuccess = false, StatusCode = "still-visible", Message = "Template is still visible after timeout." };
    }
}
