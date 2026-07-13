using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Automation.History;

public sealed class JsonTaskRunHistoryStore : ITaskRunHistoryStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _directoryPath;

    public JsonTaskRunHistoryStore(string? directoryPath = null)
    {
        _directoryPath = directoryPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneTouchAutomation",
            "history");
    }

    public async Task SaveAsync(TaskRunHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_directoryPath);

        var filePath = Path.Combine(_directoryPath, $"{entry.StartedAt:yyyyMMdd-HHmmss}-{entry.Id}.json");
        var temporaryPath = $"{filePath}.tmp";

        await using(var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, entry, SerializerOptions, cancellationToken);
        }

        File.Move(temporaryPath, filePath, true);
    }

    public async Task<IReadOnlyList<TaskRunHistoryEntry>> LoadRecentAsync(
        int maximumCount = 20,
        CancellationToken cancellationToken = default)
    {
        if(maximumCount <= 0 || !Directory.Exists(_directoryPath))
        {
            return [];
        }

        var entries = new List<TaskRunHistoryEntry>();
        var filePaths = Directory.EnumerateFiles(_directoryPath, "*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Take(maximumCount);

        foreach(var filePath in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var stream = File.OpenRead(filePath);
                var entry = await JsonSerializer.DeserializeAsync<TaskRunHistoryEntry>(
                    stream,
                    SerializerOptions,
                    cancellationToken);

                if(entry is not null)
                {
                    entries.Add(entry);
                }
            }
            catch(JsonException)
            {
                // One corrupt report must not hide the remaining execution history.
            }
        }

        return entries;
    }
}
