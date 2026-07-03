using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Automation.Behavior;

public interface IAutomationBehavior<TParameters>
{
    string Name { get; }

    string Description { get; }

    Task<BehaviorExecutionResult> ExecuteAsync
    (
            BehaviorExecutionContext context,
            TParameters parameters,
            CancellationToken cancellationToken = default(CancellationToken));
}