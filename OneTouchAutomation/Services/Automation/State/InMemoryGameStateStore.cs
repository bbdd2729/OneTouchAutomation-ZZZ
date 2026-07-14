using System.Collections.Concurrent;

namespace OneTouchAutomation.Services.Automation.State;

public sealed class InMemoryGameStateStore : IGameStateStore
{
    private readonly ConcurrentDictionary<string, GameStateRecord> _states = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;

    public InMemoryGameStateStore(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public void Record(GameStateRecord state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (string.IsNullOrWhiteSpace(state.StateId))
        {
            throw new ArgumentException("State ID is required.", nameof(state));
        }

        if (state.Confidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(state), "Confidence must be between 0 and 1.");
        }

        _states.AddOrUpdate(
            state.StateId,
            state,
            (_, existing) => state.ObservedAt >= existing.ObservedAt ? state : existing);
    }

    public GameStateRecord? GetLatest(string stateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateId);

        return _states.TryGetValue(stateId, out var state) ? state : null;
    }

    public bool IsActive(string stateId, double minimumConfidence, TimeSpan maximumAge)
    {
        if (minimumConfidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumConfidence));
        }

        if (maximumAge < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAge));
        }

        var state = GetLatest(stateId);

        return state is not null
               && state.Confidence >= minimumConfidence
               && _timeProvider.GetUtcNow() - state.ObservedAt <= maximumAge;
    }

    public IReadOnlyCollection<GameStateRecord> GetSnapshot()
    {
        return _states.Values.ToArray();
    }

    public void Expire(TimeSpan maximumAge)
    {
        if (maximumAge < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAge));
        }

        var cutoff = _timeProvider.GetUtcNow() - maximumAge;

        foreach (var pair in _states)
        {
            if (pair.Value.ObservedAt < cutoff)
            {
                _states.TryRemove(pair);
            }
        }
    }
}
