using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneTouchAutomation.Models;
using OneTouchAutomation.Services.Automation.Behavior;
using OneTouchAutomation.Services.Automation.Workflows;
using OneTouchAutomation.Services.Input;

namespace OneTouchAutomation.ViewModels;

public partial class WorkflowPageViewModel : ViewModelBase
{
    private readonly IBehaviorRegistry _behaviorRegistry;
    private readonly IWorkflowConfigurationStore _workflowStore;
    private readonly IWorkflowValidator _workflowValidator;

    [ObservableProperty] private WorkflowEditorModel? _selectedWorkflow;

    [ObservableProperty] private WorkflowNodeEditorModel? _selectedNode;

    [ObservableProperty] private WorkflowTransitionEditorModel? _selectedTransition;

    [ObservableProperty] private string _statusText = "Create or select a workflow.";

    public WorkflowPageViewModel(
        IBehaviorRegistry behaviorRegistry,
        IWorkflowConfigurationStore workflowStore,
        IWorkflowValidator workflowValidator)
    {
        _behaviorRegistry = behaviorRegistry;
        _workflowStore = workflowStore;
        _workflowValidator = workflowValidator;
        foreach(var behavior in behaviorRegistry.Behaviors.Where(behavior => behavior.Id != "run-daily-workflow"))
        {
            AvailableBehaviors.Add(behavior);
        }

        if(!Design.IsDesignMode)
        {
            _ = LoadAsync();
        }
    }

    public WorkflowPageViewModel() : this(
        new BehaviorRegistry([]),
        new JsonWorkflowConfigurationStore(new BehaviorRegistry([]), new WorkflowValidator(new BehaviorRegistry([]))),
        new WorkflowValidator(new BehaviorRegistry([]))) { }

    public ObservableCollection<WorkflowEditorModel> Workflows { get; } = new();

    public ObservableCollection<IAutomationBehavior> AvailableBehaviors { get; } = new();

    public IReadOnlyList<AutomationKey> AvailableKeys { get; } = Enum.GetValues<AutomationKey>();

    public IReadOnlyList<MouseClickMode> AvailableClickModes { get; } = Enum.GetValues<MouseClickMode>();

    public IReadOnlyList<WorkflowNodeOutcome> AvailableOutcomes { get; } = Enum.GetValues<WorkflowNodeOutcome>();

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var definitions = await _workflowStore.LoadAsync();
            Workflows.Clear();

            foreach(var definition in definitions)
            {
                Workflows.Add(WorkflowEditorModel.FromDefinition(definition));
            }

            SelectedWorkflow = Workflows.FirstOrDefault();
            StatusText = $"Loaded {Workflows.Count} workflow(s).";
        }
        catch(Exception exception)
        {
            StatusText = $"Failed to load workflows: {exception.Message}";
        }
    }

    [RelayCommand]
    private void NewWorkflow()
    {
        var workflow = new WorkflowEditorModel { Name = $"Workflow {Workflows.Count + 1}" };
        Workflows.Add(workflow);
        SelectedWorkflow = workflow;
        StatusText = "New workflow created.";
    }

    [RelayCommand]
    private void DeleteWorkflow()
    {
        if(SelectedWorkflow is null)
        {
            return;
        }

        var index = Workflows.IndexOf(SelectedWorkflow);
        Workflows.Remove(SelectedWorkflow);
        SelectedWorkflow = Workflows.ElementAtOrDefault(index) ?? Workflows.LastOrDefault();
        SelectedNode = null;
        SelectedTransition = null;
        StatusText = "Workflow removed. Save to persist the change.";
    }

    [RelayCommand]
    private void AddNode()
    {
        if(SelectedWorkflow is null)
        {
            return;
        }

        var node = new WorkflowNodeEditorModel
        {
            Name = $"Delay {SelectedWorkflow.Nodes.Count + 1}",
            BehaviorId = "delay"
        };
        var previousNode = SelectedWorkflow.Nodes.LastOrDefault();
        SelectedWorkflow.Nodes.Add(node);
        if(previousNode is not null)
        {
            SelectedWorkflow.Transitions.Add(new WorkflowTransitionEditorModel
            {
                FromNodeId = previousNode.Id,
                ToNodeId = node.Id,
                Outcome = WorkflowNodeOutcome.Success
            });
        }
        SelectedNode = node;
    }

    [RelayCommand]
    private void RemoveNode()
    {
        if(SelectedWorkflow is null || SelectedNode is null)
        {
            return;
        }

        var nodeId = SelectedNode.Id;
        var index = SelectedWorkflow.Nodes.IndexOf(SelectedNode);
        SelectedWorkflow.Nodes.Remove(SelectedNode);
        foreach(var transition in SelectedWorkflow.Transitions
                    .Where(transition => transition.FromNodeId == nodeId || transition.ToNodeId == nodeId)
                    .ToArray())
        {
            SelectedWorkflow.Transitions.Remove(transition);
        }
        SelectedNode = SelectedWorkflow.Nodes.ElementAtOrDefault(index) ?? SelectedWorkflow.Nodes.LastOrDefault();
    }

    [RelayCommand]
    private void AddTransition()
    {
        if(SelectedWorkflow is null || SelectedNode is null)
        {
            StatusText = "Select a source node before adding a transition.";
            return;
        }

        var nodeIndex = SelectedWorkflow.Nodes.IndexOf(SelectedNode);
        var nextNode = SelectedWorkflow.Nodes.ElementAtOrDefault(nodeIndex + 1);
        var transition = new WorkflowTransitionEditorModel
        {
            FromNodeId = SelectedNode.Id,
            ToNodeId = nextNode?.Id,
            IsTerminal = nextNode is null,
            Outcome = WorkflowNodeOutcome.Success
        };
        SelectedWorkflow.Transitions.Add(transition);
        SelectedTransition = transition;
    }

    [RelayCommand]
    private void RemoveTransition()
    {
        if(SelectedWorkflow is null || SelectedTransition is null)
        {
            return;
        }

        SelectedWorkflow.Transitions.Remove(SelectedTransition);
        SelectedTransition = SelectedWorkflow.Transitions.LastOrDefault();
    }

    [RelayCommand]
    private void MoveNodeUp()
    {
        if(SelectedWorkflow is null || SelectedNode is null)
        {
            return;
        }

        var index = SelectedWorkflow.Nodes.IndexOf(SelectedNode);

        if(index > 0)
        {
            SelectedWorkflow.Nodes.Move(index, index - 1);
        }
    }

    [RelayCommand]
    private void MoveNodeDown()
    {
        if(SelectedWorkflow is null || SelectedNode is null)
        {
            return;
        }

        var index = SelectedWorkflow.Nodes.IndexOf(SelectedNode);

        if(index >= 0 && index < SelectedWorkflow.Nodes.Count - 1)
        {
            SelectedWorkflow.Nodes.Move(index, index + 1);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var definitions = Workflows.Select(ToDefinition).ToArray();
            await _workflowStore.SaveAsync(definitions);
            StatusText = $"Saved {definitions.Length} workflow(s).";
        }
        catch(Exception exception)
        {
            StatusText = $"Failed to save workflows: {exception.Message}";
        }
    }

    [RelayCommand]
    private void ValidateWorkflow()
    {
        if(SelectedWorkflow is null)
        {
            StatusText = "Select a workflow first.";
            return;
        }

        try
        {
            var validation = _workflowValidator.Validate(ToDefinition(SelectedWorkflow));
            StatusText = validation.IsValid
                ? "Workflow validation passed."
                : $"Workflow validation failed: {string.Join(" ", validation.Errors)}";
        }
        catch(Exception exception)
        {
            StatusText = $"Workflow validation failed: {exception.Message}";
        }
    }

    private static WorkflowDefinition ToDefinition(WorkflowEditorModel model)
    {
        var nodes = model.Nodes.Select(node => new WorkflowNodeDefinition
        {
            Id = node.Id,
            Name = node.Name,
            BehaviorId = node.BehaviorId,
            Parameters = node.ToParameters()
        }).ToArray();

        var transitions = model.Transitions
            .Select(transition => new WorkflowTransitionDefinition
            {
                FromNodeId = transition.FromNodeId,
                ToNodeId = transition.IsTerminal ? null : transition.ToNodeId,
                Outcome = transition.Outcome,
                ExpectedStatus = string.IsNullOrWhiteSpace(transition.ExpectedStatus) ? null : transition.ExpectedStatus
            })
            .ToArray();

        return new WorkflowDefinition
        {
            Id = model.Id,
            Name = model.Name,
            StartNodeId = nodes.FirstOrDefault()?.Id,
            Nodes = nodes,
            Transitions = transitions
        };
    }
}
