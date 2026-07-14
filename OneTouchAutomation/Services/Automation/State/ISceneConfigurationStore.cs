namespace OneTouchAutomation.Services.Automation.State;

public interface ISceneConfigurationStore
{
    Task<IReadOnlyList<AutomationSceneDefinition>> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(
        IReadOnlyCollection<AutomationSceneDefinition> scenes,
        CancellationToken cancellationToken = default);
}
