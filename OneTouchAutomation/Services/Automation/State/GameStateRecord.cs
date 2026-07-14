namespace OneTouchAutomation.Services.Automation.State;

public sealed record GameStateRecord
{
    public required string StateId { get; init; }

    public required double Confidence { get; init; }

    public required DateTimeOffset ObservedAt { get; init; }

    public string? SourceFrameId { get; init; }
}
