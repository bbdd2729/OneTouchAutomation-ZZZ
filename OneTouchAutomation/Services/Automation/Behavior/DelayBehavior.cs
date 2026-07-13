using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Automation.Behavior;

public sealed class DelayBehavior : IAutomationBehavior<DelayBehaviorParameters>
{
    public string Id => "delay";

    public string Name => "Delay";

    public string Description => "Wait for a configured duration before continuing to the next task.";

    public Type ParameterType => typeof(DelayBehaviorParameters);

    public Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        object parameters,
        CancellationToken cancellationToken = default)
    {
        if(parameters is not DelayBehaviorParameters typedParameters)
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
        DelayBehaviorParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if(parameters.DurationMilliseconds <= 0)
        {
            return new BehaviorExecutionResult
            {
                IsSuccess = false,
                Message = "Delay duration must be greater than zero."
            };
        }

        context.Log($"Waiting for {parameters.DurationMilliseconds} ms.");
        await Task.Delay(parameters.DurationMilliseconds, cancellationToken);

        return new BehaviorExecutionResult
        {
            IsSuccess = true,
            Message = $"Waited for {parameters.DurationMilliseconds} ms."
        };
    }
}
