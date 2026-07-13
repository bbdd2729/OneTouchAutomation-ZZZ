using System.Collections.Generic;

namespace OneTouchAutomation.Services.Automation.Behavior;

public interface IBehaviorRegistry
{
    IReadOnlyList<IAutomationBehavior> Behaviors { get; }

    IAutomationBehavior? FindById(string id);
}
