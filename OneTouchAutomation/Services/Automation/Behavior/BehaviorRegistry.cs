using System.Collections.Generic;
using System.Linq;

namespace OneTouchAutomation.Services.Automation.Behavior;

public sealed class BehaviorRegistry : IBehaviorRegistry
{
    public BehaviorRegistry(IEnumerable<IAutomationBehavior> behaviors)
    {
        Behaviors = behaviors
            .OrderBy(behavior => behavior.Name)
            .ToArray();
    }

    public IReadOnlyList<IAutomationBehavior> Behaviors { get; }

    public IAutomationBehavior? FindById(string id)
    {
        return Behaviors.FirstOrDefault(behavior => behavior.Id == id);
    }
}
