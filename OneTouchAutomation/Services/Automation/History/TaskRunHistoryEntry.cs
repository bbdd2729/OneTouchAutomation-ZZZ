using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OneTouchAutomation.Services.Automation.History;

public sealed class TaskRunHistoryEntry
{
    public required string Id { get; init; }

    public required DateTimeOffset StartedAt { get; init; }

    public required DateTimeOffset CompletedAt { get; init; }

    public required string TargetWindowTitle { get; init; }

    public required string Summary { get; init; }

    public bool IsSuccess { get; init; }

    public bool IsCancelled { get; init; }

    public required IReadOnlyList<TaskRunTaskHistoryEntry> Tasks { get; init; }

    [JsonIgnore]
    public string DisplayText => $"{StartedAt:MM-dd HH:mm} | {Summary}";
}
