using System;
using System.Threading;
using System.Threading.Tasks;
using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Vision;

namespace OneTouchAutomation.Services.Automation.Behavior;

public sealed class CheckScreenBehavior : IAutomationBehavior<CheckScreenBehaviorParameters>
{
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly IScreenRecognitionService _screenRecognitionService;

    public CheckScreenBehavior(IScreenCaptureService screenCaptureService, IScreenRecognitionService screenRecognitionService)
    {
        _screenCaptureService = screenCaptureService;
        _screenRecognitionService = screenRecognitionService;
    }

    public string Id => "check-screen";

    public string Name => "Check Screen";

    public string Description => "Recognize a named game screen from required and excluded visual elements.";

    public Type ParameterType => typeof(CheckScreenBehaviorParameters);

    public Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        object parameters,
        CancellationToken cancellationToken = default)
    {
        return parameters is CheckScreenBehaviorParameters typedParameters
            ? ExecuteAsync(context, typedParameters, cancellationToken)
            : Task.FromResult(new BehaviorExecutionResult { IsSuccess = false, Message = $"Invalid parameters for behavior: {Name}." });
    }

    public async Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        CheckScreenBehaviorParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var frame = await _screenCaptureService.CaptureWindowClientAsync(context.WindowHandle, cancellationToken);
        var recognition = await _screenRecognitionService.RecognizeAsync(frame.PngBytes, parameters.Screen, cancellationToken);
        context.Log(recognition.Message);

        return new BehaviorExecutionResult
        {
            IsSuccess = recognition.IsMatch,
            StatusCode = recognition.StatusCode,
            Message = recognition.Message
        };
    }
}
