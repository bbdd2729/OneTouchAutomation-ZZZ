using System.Collections.Generic;
using System.Linq;
using OneTouchAutomation.Services.Automation.Behavior;

namespace OneTouchAutomation.Services.Automation.Workflows;

public sealed class WorkflowValidator : IWorkflowValidator
{
    private readonly IBehaviorRegistry _behaviorRegistry;

    public WorkflowValidator(IBehaviorRegistry behaviorRegistry)
    {
        _behaviorRegistry = behaviorRegistry;
    }

    public WorkflowValidationResult Validate(WorkflowDefinition workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        var errors = new List<string>();

        if(string.IsNullOrWhiteSpace(workflow.Id))
        {
            errors.Add("Workflow ID is required.");
        }

        if(string.IsNullOrWhiteSpace(workflow.Name))
        {
            errors.Add("Workflow name is required.");
        }

        if(workflow.Nodes.Count > 0)
        {
            ValidateGraph(workflow, errors);
        }
        else
        {
            ValidateLinearSteps(workflow, errors);
        }

        return new WorkflowValidationResult { Errors = errors };
    }

    private void ValidateGraph(WorkflowDefinition workflow, ICollection<string> errors)
    {
        if(workflow.MaxNodeExecutions <= 0)
        {
            errors.Add("MaxNodeExecutions must be greater than zero.");
        }

        var nodeIds = workflow.Nodes.Select(node => node.Id).ToArray();

        foreach(var duplicateId in nodeIds
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .GroupBy(id => id)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key))
        {
            errors.Add($"Duplicate workflow node ID: {duplicateId}.");
        }

        var nodeIdSet = nodeIds.ToHashSet();
        var startNodeId = workflow.StartNodeId ?? workflow.Nodes.First().Id;

        if(!nodeIdSet.Contains(startNodeId))
        {
            errors.Add($"Workflow start node not found: {startNodeId}.");
        }

        foreach(var node in workflow.Nodes)
        {
            ValidateExecutable(node.Id, node.Name, node.BehaviorId, node.Parameters, node.MaxRetryCount, errors);
        }

        foreach(var transition in workflow.Transitions)
        {
            if(!nodeIdSet.Contains(transition.FromNodeId))
            {
                errors.Add($"Transition source node not found: {transition.FromNodeId}.");
            }

            if(transition.ToNodeId is not null && !nodeIdSet.Contains(transition.ToNodeId))
            {
                errors.Add($"Transition target node not found: {transition.ToNodeId}.");
            }
        }
    }

    private void ValidateLinearSteps(WorkflowDefinition workflow, ICollection<string> errors)
    {
        foreach(var step in workflow.Steps)
        {
            ValidateExecutable(step.Id, step.Name, step.BehaviorId, step.Parameters, step.MaxRetryCount, errors);
        }
    }

    private void ValidateExecutable(
        string id,
        string name,
        string behaviorId,
        object parameters,
        int maxRetryCount,
        ICollection<string> errors)
    {
        if(string.IsNullOrWhiteSpace(id))
        {
            errors.Add("Workflow step or node ID is required.");
        }

        if(string.IsNullOrWhiteSpace(name))
        {
            errors.Add($"Workflow step or node name is required: {id}.");
        }

        if(_behaviorRegistry.FindById(behaviorId) is null)
        {
            errors.Add($"Behavior not found: {behaviorId}.");
        }

        if(parameters is null)
        {
            errors.Add($"Workflow parameters are required: {id}.");
        }

        if(maxRetryCount < 0)
        {
            errors.Add($"MaxRetryCount cannot be negative: {id}.");
        }
    }
}
