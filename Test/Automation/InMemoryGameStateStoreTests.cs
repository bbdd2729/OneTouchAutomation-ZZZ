using OneTouchAutomation.Services.Automation.State;

namespace Test.Automation;

public sealed class InMemoryGameStateStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Record_KeepsTheMostRecentObservationForEachState()
    {
        var store = new InMemoryGameStateStore(new FixedTimeProvider(Now));

        store.Record(CreateState("Battle", 0.80, Now));
        store.Record(CreateState("Battle", 0.95, Now.AddSeconds(1)));
        store.Record(CreateState("Battle", 0.20, Now.AddSeconds(-1)));

        var state = store.GetLatest("Battle");

        Assert.NotNull(state);
        Assert.Equal(0.95, state.Confidence);
        Assert.Equal(Now.AddSeconds(1), state.ObservedAt);
    }

    [Fact]
    public void IsActive_RequiresConfidenceAndFreshObservation()
    {
        var store = new InMemoryGameStateStore(new FixedTimeProvider(Now));
        store.Record(CreateState("ConfirmDialog", 0.88, Now.AddSeconds(-2)));

        Assert.True(store.IsActive("ConfirmDialog", 0.80, TimeSpan.FromSeconds(3)));
        Assert.False(store.IsActive("ConfirmDialog", 0.90, TimeSpan.FromSeconds(3)));
        Assert.False(store.IsActive("ConfirmDialog", 0.80, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void Expire_RemovesOnlyStaleStates()
    {
        var store = new InMemoryGameStateStore(new FixedTimeProvider(Now));
        store.Record(CreateState("Old", 0.9, Now.AddSeconds(-10)));
        store.Record(CreateState("Fresh", 0.9, Now.AddSeconds(-2)));

        store.Expire(TimeSpan.FromSeconds(5));

        Assert.Null(store.GetLatest("Old"));
        Assert.NotNull(store.GetLatest("Fresh"));
    }

    private static GameStateRecord CreateState(string id, double confidence, DateTimeOffset observedAt) => new()
    {
        StateId = id,
        Confidence = confidence,
        ObservedAt = observedAt,
    };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
