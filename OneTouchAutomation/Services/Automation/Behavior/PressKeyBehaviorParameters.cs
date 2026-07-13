using OneTouchAutomation.Services.Input;

namespace OneTouchAutomation.Services.Automation.Behavior;

public sealed class PressKeyBehaviorParameters
{
    public AutomationKey Key { get; init; } = AutomationKey.Enter;
}
