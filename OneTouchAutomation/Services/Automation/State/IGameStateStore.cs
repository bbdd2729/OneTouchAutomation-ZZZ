namespace OneTouchAutomation.Services.Automation.State;

public interface IGameStateStore
{
    void Record(GameStateRecord state);

    GameStateRecord? GetLatest(string stateId);

    bool IsActive(string stateId, double minimumConfidence, TimeSpan maximumAge);

    IReadOnlyCollection<GameStateRecord> GetSnapshot();

    void Expire(TimeSpan maximumAge);
}
