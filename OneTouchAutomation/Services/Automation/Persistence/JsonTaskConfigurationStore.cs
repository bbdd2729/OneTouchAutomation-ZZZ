using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Automation.Persistence;

public sealed class JsonTaskConfigurationStore : ITaskConfigurationStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _filePath;

    public JsonTaskConfigurationStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneTouchAutomation",
            "config",
            "tasks.json");
    }

    public async Task<IReadOnlyList<AutomationTaskConfiguration>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if(!File.Exists(_filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_filePath);

        return await JsonSerializer.DeserializeAsync<List<AutomationTaskConfiguration>>(
                   stream,
                   SerializerOptions,
                   cancellationToken)
               ?? [];
    }

    public async Task SaveAsync(
        IReadOnlyCollection<AutomationTaskConfiguration> tasks,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_filePath)
                        ?? throw new InvalidOperationException("Task configuration path has no directory.");

        Directory.CreateDirectory(directory);

        var temporaryPath = $"{_filePath}.tmp";

        await using(var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                tasks,
                SerializerOptions,
                cancellationToken);
        }

        File.Move(temporaryPath, _filePath, true);
    }
}
