namespace OneTouchAutomation.Services.Automation.State;

public interface ISceneScheduler
{
    AutomationSceneDefinition? SelectNext(IReadOnlyCollection<AutomationSceneDefinition> scenes);

    void MarkExecuted(AutomationSceneDefinition scene);
}
