using System.Threading;
using System.Threading.Tasks;
using OneTouchAutomation.Services.Input;

namespace OneTouchAutomation.Services.Automation.Behavior;

public sealed class PressKeyBehavior : IAutomationBehavior<PressKeyBehaviorParameters>
{
    private readonly IInputService _inputService;
    private readonly IWindowActivationService _windowActivationService;

    public PressKeyBehavior(
        IInputService inputService,
        IWindowActivationService windowActivationService)
    {
        _inputService = inputService;
        _windowActivationService = windowActivationService;
    }

    public string Id => "press-key";

    public string Name => "Press Key";

    public string Description => "Activate the selected window and send a keyboard key.";

    public Type ParameterType => typeof(PressKeyBehaviorParameters);

    public Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        object parameters,
        CancellationToken cancellationToken = default)
    {
        if(parameters is not PressKeyBehaviorParameters typedParameters)
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
        PressKeyBehaviorParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if(!Enum.IsDefined(parameters.Key))
        {
            return new BehaviorExecutionResult
            {
                IsSuccess = false,
                Message = "Unsupported automation key."
            };
        }

        context.Log("Activating target window.");
        await _windowActivationService.ActivateAsync(context.WindowHandle, cancellationToken);

        context.Log($"Pressing key: {parameters.Key}.");
        await _inputService.PressKeyAsync(parameters.Key, cancellationToken);

        return new BehaviorExecutionResult
        {
            IsSuccess = true,
            Message = $"Pressed key: {parameters.Key}."
        };
    }
}
