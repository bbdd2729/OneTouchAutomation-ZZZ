using System.Text.Json;

namespace OneTouchAutomation.Services.Automation.State;

public sealed class JsonSceneConfigurationStore : ISceneConfigurationStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly string _filePath;

    public JsonSceneConfigurationStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneTouchAutomation",
            "config",
            "scenes.json");
    }

    public async Task<IReadOnlyList<AutomationSceneDefinition>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<List<AutomationSceneDefinition>>(
                   stream,
                   SerializerOptions,
                   cancellationToken)
               ?? [];
    }

    public async Task SaveAsync(
        IReadOnlyCollection<AutomationSceneDefinition> scenes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenes);
        Validate(scenes);

        var directory = Path.GetDirectoryName(_filePath)
                        ?? throw new InvalidOperationException("Scene configuration path has no directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = $"{_filePath}.tmp";

        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, scenes, SerializerOptions, cancellationToken);
        }

        File.Move(temporaryPath, _filePath, true);
    }

    private static void Validate(IReadOnlyCollection<AutomationSceneDefinition> scenes)
    {
        var sceneIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var scene in scenes)
        {
            if (string.IsNullOrWhiteSpace(scene.Id))
            {
                throw new InvalidOperationException("Scene ID is required.");
            }

            if (!sceneIds.Add(scene.Id))
            {
                throw new InvalidOperationException($"Duplicate scene ID: {scene.Id}.");
            }

            if (string.IsNullOrWhiteSpace(scene.Name) || string.IsNullOrWhiteSpace(scene.WorkflowId))
            {
                throw new InvalidOperationException($"Scene name and workflow ID are required: {scene.Id}.");
            }

            if (scene.Cooldown < TimeSpan.Zero)
            {
                throw new InvalidOperationException($"Scene cooldown cannot be negative: {scene.Id}.");
            }

            foreach (var condition in scene.Conditions.Conditions)
            {
                if (string.IsNullOrWhiteSpace(condition.StateId)
                    || condition.MinimumConfidence is < 0 or > 1
                    || condition.MaximumAge < TimeSpan.Zero)
                {
                    throw new InvalidOperationException($"Invalid condition in scene: {scene.Id}.");
                }
            }
        }
    }
}
