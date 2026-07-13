using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneTouchAutomation.Services.Automation.Behavior;
using OneTouchAutomation.Services.Input;

namespace OneTouchAutomation.Services.Automation.Tasks;

public sealed class TaskRunner : ITaskRunner
{
    private readonly IBehaviorRegistry _behaviorRegistry;
    private readonly IWindowActivationService? _windowActivationService;

    public TaskRunner(
        IBehaviorRegistry behaviorRegistry,
        IWindowActivationService? windowActivationService = null)
    {
        _behaviorRegistry = behaviorRegistry;
        _windowActivationService = windowActivationService;
    }

    public async Task<TaskRunResult> RunAsync(
        IntPtr windowHandle,
        IReadOnlyList<AutomationTaskDefinition> tasks,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<AutomationTaskExecutionResult>();

        try
        {
            if(_windowActivationService is not null)
            {
                log?.Invoke("Activating target window.");
                await _windowActivationService.ActivateAsync(windowHandle, cancellationToken);
            }

            foreach(var task in tasks.Where(task => task.IsEnabled))
            {
                cancellationToken.ThrowIfCancellationRequested();

                log?.Invoke($"Starting task: {task.Name}.");

                var behavior = _behaviorRegistry.FindById(task.BehaviorId);
                var stopwatch = Stopwatch.StartNew();
                var attempts = 0;
                BehaviorExecutionResult behaviorResult;

                do
                {
                    attempts++;
                    behaviorResult = await ExecuteBehaviorAsync(
                        behavior,
                        task,
                        windowHandle,
                        log,
                        cancellationToken);

                    if(behaviorResult.IsSuccess || task.FailurePolicy != TaskFailurePolicy.Retry || attempts > task.MaxRetryCount)
                    {
                        break;
                    }

                    log?.Invoke($"Retrying task: {task.Name}. Attempt {attempts + 1}.");
                }
                while(true);

                stopwatch.Stop();

                results.Add
                    (new AutomationTaskExecutionResult
                    {
                            TaskId         = task.Id,
                            TaskName       = task.Name,
                            BehaviorId     = task.BehaviorId,
                            BehaviorResult = behaviorResult,
                            Duration       = stopwatch.Elapsed,
                            AttemptCount   = attempts
                    });

                if(!behaviorResult.IsSuccess)
                {
                    log?.Invoke($"Task failed: {task.Name}. {behaviorResult.Message}");

                    if(task.FailurePolicy == TaskFailurePolicy.Continue)
                    {
                        log?.Invoke($"Continuing after failed task: {task.Name}.");
                        continue;
                    }

                    break;
                }

                log?.Invoke($"Task completed: {task.Name}.");
            }

            return new TaskRunResult
            {
                    TaskResults = results,
                    IsCancelled = false
            };
        }
        catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested)
        {
            log?.Invoke("Task run cancelled.");

            return new TaskRunResult
            {
                    TaskResults = results,
                    IsCancelled = true
            };
        }
    }

    private static async Task<BehaviorExecutionResult> ExecuteBehaviorAsync(
        IAutomationBehavior? behavior,
        AutomationTaskDefinition task,
        IntPtr windowHandle,
        Action<string>? log,
        CancellationToken cancellationToken)
    {
        if(behavior is null)
        {
            return new BehaviorExecutionResult
            {
                IsSuccess = false,
                Message = $"Behavior not found: {task.BehaviorId}."
            };
        }

        try
        {
            return await behavior.ExecuteAsync(
                new BehaviorExecutionContext
                {
                    WindowHandle = windowHandle,
                    Log = message => log?.Invoke($"[{task.Name}] {message}"),
                    CancellationToken = cancellationToken
                },
                task.Parameters,
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
