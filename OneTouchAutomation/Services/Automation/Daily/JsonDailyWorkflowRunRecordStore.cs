using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Automation.Daily;

public sealed class JsonDailyWorkflowRunRecordStore : IDailyWorkflowRunRecordStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string _directoryPath;

    public JsonDailyWorkflowRunRecordStore(string? directoryPath = null)
    {
        _directoryPath = directoryPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneTouchAutomation",
            "daily-runs");
    }

    public async Task<DailyWorkflowRunRecord?> GetAsync(
        string workflowId,
        DateOnly gameDay,
        CancellationToken cancellationToken = default)
    {
        var path = GetPath(workflowId, gameDay);

        if(!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<DailyWorkflowRunRecord>(stream, SerializerOptions, cancellationToken);
    }

    public async Task SaveAsync(DailyWorkflowRunRecord record, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_directoryPath);
        var path = GetPath(record.WorkflowId, record.GameDay);
        var temporaryPath = $"{path}.tmp";

        await using(var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, record, SerializerOptions, cancellationToken);
        }

        File.Move(temporaryPath, path, true);
    }

    private string GetPath(string workflowId, DateOnly gameDay)
    {
        var safeWorkflowId = string.Concat(workflowId.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
        return Path.Combine(_directoryPath, $"{gameDay:yyyyMMdd}-{safeWorkflowId}.json");
    }
}
