using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Automation.Behavior;

public interface IAutomationBehavior
{
    string Id { get; }

    string Name { get; }

    string Description { get; }

    Type ParameterType { get; }

    Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        object parameters,
        CancellationToken cancellationToken = default);
}

public interface IAutomationBehavior<TParameters> : IAutomationBehavior
{

    Task<BehaviorExecutionResult> ExecuteAsync
    (
            BehaviorExecutionContext context,
            TParameters parameters,
            CancellationToken cancellationToken = default(CancellationToken));
}
