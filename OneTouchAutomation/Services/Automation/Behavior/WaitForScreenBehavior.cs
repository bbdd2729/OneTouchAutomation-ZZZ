using System;
using System.Threading;
using System.Threading.Tasks;
using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Vision;

namespace OneTouchAutomation.Services.Automation.Behavior;

public sealed class WaitForScreenBehavior : IAutomationBehavior<WaitForScreenBehaviorParameters>
{
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly IScreenRecognitionService _screenRecognitionService;

    public WaitForScreenBehavior(IScreenCaptureService screenCaptureService, IScreenRecognitionService screenRecognitionService)
    {
        _screenCaptureService = screenCaptureService;
        _screenRecognitionService = screenRecognitionService;
    }

    public string Id => "wait-for-screen";

    public string Name => "Wait For Screen";

    public string Description => "Wait until a named screen matches its required and excluded visual elements.";

    public Type ParameterType => typeof(WaitForScreenBehaviorParameters);

    public Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        object parameters,
        CancellationToken cancellationToken = default)
    {
        return parameters is WaitForScreenBehaviorParameters typedParameters
            ? ExecuteAsync(context, typedParameters, cancellationToken)
            : Task.FromResult(new BehaviorExecutionResult { IsSuccess = false, Message = $"Invalid parameters for behavior: {Name}." });
    }

    public async Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        WaitForScreenBehaviorParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if(parameters.TimeoutSeconds <= 0 || parameters.PollIntervalMilliseconds <= 0)
        {
            return new BehaviorExecutionResult { IsSuccess = false, Message = "Timeout and poll interval must be greater than zero." };
        }

        var deadline = DateTimeOffset.UtcNow.AddSeconds(parameters.TimeoutSeconds);
        ScreenRecognitionResult? latest = null;
        while(DateTimeOffset.UtcNow <= deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var frame = await _screenCaptureService.CaptureWindowClientAsync(context.WindowHandle, cancellationToken);
            latest = await _screenRecognitionService.RecognizeAsync(frame.PngBytes, parameters.Screen, cancellationToken);
            if(latest.IsMatch)
            {
                context.Log(latest.Message);
                return new BehaviorExecutionResult { IsSuccess = true, StatusCode = latest.StatusCode, Message = latest.Message };
            }

            var remaining = deadline - DateTimeOffset.UtcNow;
            if(remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining < TimeSpan.FromMilliseconds(parameters.PollIntervalMilliseconds)
                    ? remaining
                    : TimeSpan.FromMilliseconds(parameters.PollIntervalMilliseconds), cancellationToken);
            }
        }

        var statusCode = latest?.StatusCode ?? $"not-{parameters.Screen.Id}";
        return new BehaviorExecutionResult
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Message = $"Timed out waiting for screen: {parameters.Screen.Name}."
        };
    }
}
