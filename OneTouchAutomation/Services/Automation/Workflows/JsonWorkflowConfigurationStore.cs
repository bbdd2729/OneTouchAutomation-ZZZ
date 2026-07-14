using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OneTouchAutomation.Services.Automation.Behavior;
using OneTouchAutomation.Services.Automation.Tasks;

namespace OneTouchAutomation.Services.Automation.Workflows;

public sealed class JsonWorkflowConfigurationStore : IWorkflowConfigurationStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly IBehaviorRegistry _behaviorRegistry;
    private readonly IWorkflowValidator _workflowValidator;
    private readonly string _filePath;

    public JsonWorkflowConfigurationStore(
        IBehaviorRegistry behaviorRegistry,
        IWorkflowValidator workflowValidator,
        string? filePath = null)
    {
        _behaviorRegistry = behaviorRegistry;
        _workflowValidator = workflowValidator;
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneTouchAutomation",
            "config",
            "workflows.json");
    }

    public async Task<IReadOnlyList<WorkflowDefinition>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if(!File.Exists(_filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_filePath);
        var configurations = await JsonSerializer.DeserializeAsync<List<WorkflowConfiguration>>(
            stream,
            SerializerOptions,
            cancellationToken) ?? [];

        return configurations.Select(ToDefinition).ToArray();
    }

    public async Task SaveAsync(
        IReadOnlyCollection<WorkflowDefinition> workflows,
        CancellationToken cancellationToken = default)
    {
        foreach(var workflow in workflows)
        {
            var validation = _workflowValidator.Validate(workflow);

            if(!validation.IsValid)
            {
                throw new InvalidOperationException(string.Join(" ", validation.Errors));
            }
        }

        var directory = Path.GetDirectoryName(_filePath)
                        ?? throw new InvalidOperationException("Workflow configuration path has no directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = $"{_filePath}.tmp";
        var configurations = workflows.Select(ToConfiguration).ToArray();

        await using(var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, configurations, SerializerOptions, cancellationToken);
        }

        File.Move(temporaryPath, _filePath, true);
    }

    private WorkflowDefinition ToDefinition(WorkflowConfiguration configuration)
    {
        var definition = new WorkflowDefinition
        {
            Id = configuration.Id,
            Name = configuration.Name,
            StartNodeId = configuration.StartNodeId,
            MaxNodeExecutions = configuration.MaxNodeExecutions,
            Steps = configuration.Steps.Select(ToStepDefinition).ToArray(),
            Nodes = configuration.Nodes.Select(ToNodeDefinition).ToArray(),
            Transitions = configuration.Transitions.Select(transition => new WorkflowTransitionDefinition
            {
                FromNodeId = transition.FromNodeId,
                ToNodeId = transition.ToNodeId,
                Outcome = transition.Outcome,
                ExpectedStatus = transition.ExpectedStatus
            }).ToArray()
        };

        var validation = _workflowValidator.Validate(definition);

        if(!validation.IsValid)
        {
            throw new InvalidDataException(string.Join(" ", validation.Errors));
        }

        return definition;
    }

    private WorkflowStepDefinition ToStepDefinition(WorkflowStepConfiguration configuration)
    {
        return new WorkflowStepDefinition
        {
            Id = configuration.Id,
            Name = configuration.Name,
            BehaviorId = configuration.BehaviorId,
            Parameters = DeserializeParameters(configuration.BehaviorId, configuration.Parameters),
            IsEnabled = configuration.IsEnabled,
            FailurePolicy = configuration.FailurePolicy,
            MaxRetryCount = configuration.MaxRetryCount
        };
    }

    private WorkflowNodeDefinition ToNodeDefinition(WorkflowNodeConfiguration configuration)
    {
        return new WorkflowNodeDefinition
        {
            Id = configuration.Id,
            Name = configuration.Name,
            BehaviorId = configuration.BehaviorId,
            Parameters = DeserializeParameters(configuration.BehaviorId, configuration.Parameters),
            IsEnabled = configuration.IsEnabled,
            FailurePolicy = configuration.FailurePolicy,
            MaxRetryCount = configuration.MaxRetryCount
        };
    }

    private object DeserializeParameters(string behaviorId, JsonElement parameters)
    {
        var behavior = _behaviorRegistry.FindById(behaviorId)
                       ?? throw new InvalidDataException($"Behavior not found: {behaviorId}.");

        return parameters.Deserialize(behavior.ParameterType, SerializerOptions)
               ?? throw new InvalidDataException($"Failed to deserialize workflow parameters: {behaviorId}.");
    }

    private static WorkflowConfiguration ToConfiguration(WorkflowDefinition definition)
    {
        return new WorkflowConfiguration
        {
            Id = definition.Id,
            Name = definition.Name,
            StartNodeId = definition.StartNodeId,
            MaxNodeExecutions = definition.MaxNodeExecutions,
            Steps = definition.Steps.Select(step => new WorkflowStepConfiguration
            {
                Id = step.Id,
                Name = step.Name,
                BehaviorId = step.BehaviorId,
                Parameters = JsonSerializer.SerializeToElement(step.Parameters, step.Parameters.GetType(), SerializerOptions),
                IsEnabled = step.IsEnabled,
                FailurePolicy = step.FailurePolicy,
                MaxRetryCount = step.MaxRetryCount
            }).ToArray(),
            Nodes = definition.Nodes.Select(node => new WorkflowNodeConfiguration
            {
                Id = node.Id,
                Name = node.Name,
                BehaviorId = node.BehaviorId,
                Parameters = JsonSerializer.SerializeToElement(node.Parameters, node.Parameters.GetType(), SerializerOptions),
                IsEnabled = node.IsEnabled,
                FailurePolicy = node.FailurePolicy,
                MaxRetryCount = node.MaxRetryCount
            }).ToArray(),
            Transitions = definition.Transitions.Select(transition => new WorkflowTransitionConfiguration
            {
                FromNodeId = transition.FromNodeId,
                ToNodeId = transition.ToNodeId,
                Outcome = transition.Outcome,
                ExpectedStatus = transition.ExpectedStatus
            }).ToArray()
        };
    }
}

public sealed class WorkflowConfiguration
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string? StartNodeId { get; init; }

    public int MaxNodeExecutions { get; init; } = 100;

    public IReadOnlyList<WorkflowStepConfiguration> Steps { get; init; } = [];

    public IReadOnlyList<WorkflowNodeConfiguration> Nodes { get; init; } = [];

    public IReadOnlyList<WorkflowTransitionConfiguration> Transitions { get; init; } = [];
}

public sealed class WorkflowStepConfiguration
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string BehaviorId { get; init; }

    public JsonElement Parameters { get; init; }

    public bool IsEnabled { get; init; } = true;

    public TaskFailurePolicy FailurePolicy { get; init; } = TaskFailurePolicy.Stop;

    public int MaxRetryCount { get; init; }
}

public sealed class WorkflowNodeConfiguration
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string BehaviorId { get; init; }

    public JsonElement Parameters { get; init; }

    public bool IsEnabled { get; init; } = true;

    public TaskFailurePolicy FailurePolicy { get; init; } = TaskFailurePolicy.Stop;

    public int MaxRetryCount { get; init; }
}

public sealed class WorkflowTransitionConfiguration
{
    public required string FromNodeId { get; init; }

    public string? ToNodeId { get; init; }

    public WorkflowNodeOutcome Outcome { get; init; }

    public string? ExpectedStatus { get; init; }
}
