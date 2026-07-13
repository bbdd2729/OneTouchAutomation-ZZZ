using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneTouchAutomation.Services.Automation.Behavior;
using OneTouchAutomation.Services.Automation.Tasks;
using OneTouchAutomation.Services.Input;

namespace OneTouchAutomation.Services.Automation.Workflows;

public sealed class WorkflowRunner : IWorkflowRunner
{
    private readonly IBehaviorRegistry _behaviorRegistry;
    private readonly IWindowActivationService? _windowActivationService;
    private readonly IWorkflowValidator _workflowValidator;

    public WorkflowRunner(
        IBehaviorRegistry behaviorRegistry,
        IWindowActivationService? windowActivationService = null,
        IWorkflowValidator? workflowValidator = null)
    {
        _behaviorRegistry = behaviorRegistry;
        _windowActivationService = windowActivationService;
        _workflowValidator = workflowValidator ?? new WorkflowValidator(behaviorRegistry);
    }

    public async Task<WorkflowRunResult> RunAsync(
        IntPtr windowHandle,
        WorkflowDefinition workflow,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var validation = _workflowValidator.Validate(workflow);

            if(!validation.IsValid)
            {
                return FailureResult(string.Join(" ", validation.Errors), log, workflow.Name);
            }

            if(_windowActivationService is not null)
            {
                log?.Invoke($"[{workflow.Name}] Activating target window.");
                await _windowActivationService.ActivateAsync(windowHandle, cancellationToken);
            }

            return workflow.Nodes.Count > 0
                ? await RunGraphAsync(windowHandle, workflow, log, cancellationToken)
                : await RunLinearAsync(windowHandle, workflow, log, cancellationToken);
        }
        catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested)
        {
            log?.Invoke($"[{workflow.Name}] Workflow cancelled.");
            return new WorkflowRunResult
            {
                IsCancelled = true,
                FinalOutcome = WorkflowNodeOutcome.Cancelled
            };
        }
    }

    private async Task<WorkflowRunResult> RunLinearAsync(
        IntPtr windowHandle,
        WorkflowDefinition workflow,
        Action<string>? log,
        CancellationToken cancellationToken)
    {
        var results = new List<WorkflowStepExecutionResult>();
        var hasFailure = false;

        foreach(var step in workflow.Steps.Where(step => step.IsEnabled))
        {
            cancellationToken.ThrowIfCancellationRequested();
            log?.Invoke($"[{workflow.Name}] Starting step: {step.Name}.");

            var result = await ExecuteAsync(
                windowHandle,
                workflow.Name,
                step.Id,
                step.Name,
                step.BehaviorId,
                step.Parameters,
                step.FailurePolicy,
                step.MaxRetryCount,
                log,
                cancellationToken);

            results.Add(result);

            if(result.Outcome == WorkflowNodeOutcome.Success)
            {
                log?.Invoke($"[{workflow.Name}] Completed step: {step.Name}.");
                continue;
            }

            hasFailure = true;
            log?.Invoke($"[{workflow.Name}] Step failed: {step.Name}. {result.Status}");

            if(step.FailurePolicy != TaskFailurePolicy.Continue)
            {
                break;
            }
        }

        return new WorkflowRunResult
        {
            StepResults = results,
            FinalOutcome = hasFailure ? WorkflowNodeOutcome.Failure : WorkflowNodeOutcome.Success
        };
    }

    private async Task<WorkflowRunResult> RunGraphAsync(
        IntPtr windowHandle,
        WorkflowDefinition workflow,
        Action<string>? log,
        CancellationToken cancellationToken)
    {
        if(workflow.MaxNodeExecutions <= 0)
        {
            return FailureResult("MaxNodeExecutions must be greater than zero.", log, workflow.Name);
        }

        var nodes = workflow.Nodes.ToDictionary(node => node.Id);
        var currentNodeId = workflow.StartNodeId ?? workflow.Nodes.First().Id;

        if(!nodes.ContainsKey(currentNodeId))
        {
            return FailureResult($"Workflow start node not found: {currentNodeId}.", log, workflow.Name);
        }

        var results = new List<WorkflowStepExecutionResult>();

        for(var executionCount = 0; executionCount < workflow.MaxNodeExecutions; executionCount++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var node = nodes[currentNodeId];
            WorkflowStepExecutionResult result;

            if(node.IsEnabled)
            {
                log?.Invoke($"[{workflow.Name}] Starting node: {node.Name}.");
                result = await ExecuteAsync(
                    windowHandle,
                    workflow.Name,
                    node.Id,
                    node.Name,
                    node.BehaviorId,
                    node.Parameters,
                    node.FailurePolicy,
                    node.MaxRetryCount,
                    log,
                    cancellationToken);
                results.Add(result);
            }
            else
            {
                result = new WorkflowStepExecutionResult
                {
                    StepId = node.Id,
                    StepName = node.Name,
                    BehaviorId = node.BehaviorId,
                    BehaviorResult = new BehaviorExecutionResult
                    {
                        IsSuccess = true,
                        Message = "Node skipped because it is disabled."
                    },
                    Duration = TimeSpan.Zero,
                    Outcome = WorkflowNodeOutcome.Success,
                    Status = "Disabled"
                };
                results.Add(result);
            }

            var transition = FindTransition(workflow.Transitions, node.Id, result.Outcome, result.Status);

            if(transition?.ToNodeId is null)
            {
                return new WorkflowRunResult
                {
                    StepResults = results,
                    FinalOutcome = result.Outcome
                };
            }

            if(!nodes.ContainsKey(transition.ToNodeId))
            {
                log?.Invoke($"[{workflow.Name}] Transition target not found: {transition.ToNodeId}.");
                return new WorkflowRunResult
                {
                    StepResults = results,
                    FinalOutcome = WorkflowNodeOutcome.Failure
                };
            }

            currentNodeId = transition.ToNodeId;
        }

        log?.Invoke($"[{workflow.Name}] Maximum node executions reached.");
        return new WorkflowRunResult
        {
            StepResults = results,
            FinalOutcome = WorkflowNodeOutcome.Failure
        };
    }

    private async Task<WorkflowStepExecutionResult> ExecuteAsync(
        IntPtr windowHandle,
        string workflowName,
        string stepId,
        string stepName,
        string behaviorId,
        object parameters,
        TaskFailurePolicy failurePolicy,
        int maxRetryCount,
        Action<string>? log,
        CancellationToken cancellationToken)
    {
        var behavior = _behaviorRegistry.FindById(behaviorId);
        var stopwatch = Stopwatch.StartNew();
        var attempts = 0;
        BehaviorExecutionResult behaviorResult;

        do
        {
            attempts++;
            behaviorResult = await ExecuteBehaviorAsync(
                behavior,
                windowHandle,
                workflowName,
                stepName,
                behaviorId,
                parameters,
                log,
                cancellationToken);

            if(behaviorResult.IsSuccess
               || failurePolicy != TaskFailurePolicy.Retry
               || attempts > maxRetryCount)
            {
                break;
            }

            log?.Invoke($"[{workflowName}] Retrying node: {stepName}. Attempt {attempts + 1}.");
        }
        while(true);

        stopwatch.Stop();

        return new WorkflowStepExecutionResult
        {
            StepId = stepId,
            StepName = stepName,
            BehaviorId = behaviorId,
            BehaviorResult = behaviorResult,
            Duration = stopwatch.Elapsed,
            AttemptCount = attempts,
            Outcome = behaviorResult.IsSuccess ? WorkflowNodeOutcome.Success : WorkflowNodeOutcome.Failure,
            Status = behaviorResult.Message
        };
    }

    private static WorkflowTransitionDefinition? FindTransition(
        IReadOnlyList<WorkflowTransitionDefinition> transitions,
        string fromNodeId,
        WorkflowNodeOutcome outcome,
        string status)
    {
        var candidates = transitions
            .Where(transition => transition.FromNodeId == fromNodeId && transition.Outcome == outcome)
            .ToArray();

        return candidates.FirstOrDefault(transition => transition.ExpectedStatus == status)
               ?? candidates.FirstOrDefault(transition => transition.ExpectedStatus is null);
    }

    private static WorkflowRunResult FailureResult(string message, Action<string>? log, string workflowName)
    {
        log?.Invoke($"[{workflowName}] {message}");
        return new WorkflowRunResult { FinalOutcome = WorkflowNodeOutcome.Failure };
    }

    private static async Task<BehaviorExecutionResult> ExecuteBehaviorAsync(
        IAutomationBehavior? behavior,
        IntPtr windowHandle,
        string workflowName,
        string stepName,
        string behaviorId,
        object parameters,
        Action<string>? log,
        CancellationToken cancellationToken)
    {
        if(behavior is null)
        {
            return new BehaviorExecutionResult
            {
                IsSuccess = false,
                Message = $"Behavior not found: {behaviorId}."
            };
        }

        try
        {
            return await behavior.ExecuteAsync(
                new BehaviorExecutionContext
                {
                    WindowHandle = windowHandle,
                    Log = message => log?.Invoke($"[{workflowName}/{stepName}] {message}"),
                    CancellationToken = cancellationToken
                },
                parameters,
                cancellationToken);
        }
        catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch(Exception exception)
        {
            return new BehaviorExecutionResult
            {
                IsSuccess = false,
                Message = $"Unhandled behavior error: {exception.Message}"
            };
        }
    }
}
