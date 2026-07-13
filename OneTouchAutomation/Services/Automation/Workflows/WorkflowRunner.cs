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

    public WorkflowRunner(
        IBehaviorRegistry behaviorRegistry,
        IWindowActivationService? windowActivationService = null)
    {
        _behaviorRegistry = behaviorRegistry;
        _windowActivationService = windowActivationService;
    }

    public async Task<WorkflowRunResult> RunAsync(
        IntPtr windowHandle,
        WorkflowDefinition workflow,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        var results = new List<WorkflowStepExecutionResult>();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if(_windowActivationService is not null)
            {
                log?.Invoke($"[{workflow.Name}] Activating target window.");
                await _windowActivationService.ActivateAsync(windowHandle, cancellationToken);
            }

            foreach(var step in workflow.Steps.Where(step => step.IsEnabled))
            {
                cancellationToken.ThrowIfCancellationRequested();
                log?.Invoke($"[{workflow.Name}] Starting step: {step.Name}.");

                var behavior = _behaviorRegistry.FindById(step.BehaviorId);
                var stopwatch = Stopwatch.StartNew();
                var attempts = 0;
                BehaviorExecutionResult result;

                do
                {
                    attempts++;
                    result = await ExecuteStepAsync(
                        behavior,
                        windowHandle,
                        workflow.Name,
                        step,
                        log,
                        cancellationToken);

                    if(result.IsSuccess
                       || step.FailurePolicy != TaskFailurePolicy.Retry
                       || attempts > step.MaxRetryCount)
                    {
                        break;
                    }

                    log?.Invoke($"[{workflow.Name}] Retrying step: {step.Name}. Attempt {attempts + 1}.");
                }
                while(true);

                stopwatch.Stop();
                results.Add(new WorkflowStepExecutionResult
                {
                    StepId = step.Id,
                    StepName = step.Name,
                    BehaviorId = step.BehaviorId,
                    BehaviorResult = result,
                    Duration = stopwatch.Elapsed,
                    AttemptCount = attempts
                });

                if(result.IsSuccess)
                {
                    log?.Invoke($"[{workflow.Name}] Completed step: {step.Name}.");
                    continue;
                }

                log?.Invoke($"[{workflow.Name}] Step failed: {step.Name}. {result.Message}");

                if(step.FailurePolicy == TaskFailurePolicy.Continue)
                {
                    continue;
                }

                break;
            }

            return new WorkflowRunResult { StepResults = results };
        }
        catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested)
        {
            log?.Invoke($"[{workflow.Name}] Workflow cancelled.");
            return new WorkflowRunResult { StepResults = results, IsCancelled = true };
        }
    }

    private static async Task<BehaviorExecutionResult> ExecuteStepAsync(
        IAutomationBehavior? behavior,
        IntPtr windowHandle,
        string workflowName,
        WorkflowStepDefinition step,
        Action<string>? log,
        CancellationToken cancellationToken)
    {
        if(behavior is null)
        {
            return new BehaviorExecutionResult
            {
                IsSuccess = false,
                Message = $"Behavior not found: {step.BehaviorId}."
            };
        }

        try
        {
            return await behavior.ExecuteAsync(
                new BehaviorExecutionContext
                {
                    WindowHandle = windowHandle,
                    Log = message => log?.Invoke($"[{workflowName}/{step.Name}] {message}"),
                    CancellationToken = cancellationToken
                },
                step.Parameters,
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
