using System.Collections.Concurrent;

namespace OneTouchAutomation.Services.Automation.State;

public sealed class SceneScheduler : ISceneScheduler
{
    private readonly IGameStateConditionEvaluator _conditionEvaluator;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastExecutedAt = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;

    public SceneScheduler(
        IGameStateConditionEvaluator conditionEvaluator,
        TimeProvider? timeProvider = null)
    {
        _conditionEvaluator = conditionEvaluator;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public AutomationSceneDefinition? SelectNext(IReadOnlyCollection<AutomationSceneDefinition> scenes)
    {
        ArgumentNullException.ThrowIfNull(scenes);

        var now = _timeProvider.GetUtcNow();

        return scenes
            .Where(scene => scene.IsEnabled)
            .Where(scene => IsReady(scene, now))
            .Where(scene => _conditionEvaluator.Evaluate(scene.Conditions))
            .OrderByDescending(scene => scene.Priority)
            .ThenBy(scene => scene.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    public void MarkExecuted(AutomationSceneDefinition scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        _lastExecutedAt[scene.Id] = _timeProvider.GetUtcNow();
    }

    private bool IsReady(AutomationSceneDefinition scene, DateTimeOffset now)
    {
        if (scene.Cooldown < TimeSpan.Zero)
        {
            return false;
        }

        return !_lastExecutedAt.TryGetValue(scene.Id, out var lastExecutedAt)
               || now - lastExecutedAt >= scene.Cooldown;
    }
}
