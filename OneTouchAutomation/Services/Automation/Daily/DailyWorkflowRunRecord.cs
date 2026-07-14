namespace OneTouchAutomation.Services.Automation.Daily;

public sealed class DailyWorkflowRunRecord
{
    public required string WorkflowId { get; init; }

    public required DateOnly GameDay { get; init; }

    public DailyTaskRunStatus Status { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public string? Message { get; init; }

    public string? EvidenceDirectory { get; init; }
}
