using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Automation.Behavior;

public interface IAutomationBehavior
{
    string Name { get; }

    string Description { get; }

    Task<BehaviorExecutionResult> ExecuteAsync
    (
            BehaviorExecutionContext context,
            CancellationToken cancellationToken = default(CancellationToken));
}