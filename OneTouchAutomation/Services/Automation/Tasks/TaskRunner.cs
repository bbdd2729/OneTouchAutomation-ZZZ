using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneTouchAutomation.Services.Automation.Behavior;

namespace OneTouchAutomation.Services.Automation.Tasks;

public sealed class TaskRunner : ITaskRunner
{
    private readonly IBehaviorRegistry _behaviorRegistry;

    public TaskRunner(IBehaviorRegistry behaviorRegistry)
    {
        _behaviorRegistry = behaviorRegistry;
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
            foreach(var task in tasks.Where(task => task.IsEnabled))
            {
                cancellationToken.ThrowIfCancellationRequested();

                log?.Invoke($"Starting task: {task.Name}.");

                var behavior = _behaviorRegistry.FindById(task.BehaviorId);
                var stopwatch = Stopwatch.StartNew();
                BehaviorExecutionResult behaviorResult;

                if(behavior is null)
                {
                    behaviorResult = new BehaviorExecutionResult
                    {
                        IsSuccess = false,
                        Message = $"Behavior not found: {task.BehaviorId}."
                    };
                }
                else
                {
                    behaviorResult = await behavior.ExecuteAsync
                        (
                         new BehaviorExecutionContext
                         {
                                 WindowHandle       = windowHandle,
                                 Log                = message => log?.Invoke($"[{task.Name}] {message}"),
                                 CancellationToken  = cancellationToken
                         },
                         task.Parameters,
                         cancellationToken);
                }

                stopwatch.Stop();

                results.Add
                    (new AutomationTaskExecutionResult
                    {
                            TaskId         = task.Id,
                            TaskName       = task.Name,
                            BehaviorId     = task.BehaviorId,
                            BehaviorResult = behaviorResult,
                            Duration       = stopwatch.Elapsed
                    });

                if(!behaviorResult.IsSuccess)
                {
                    log?.Invoke($"Task failed: {task.Name}. {behaviorResult.Message}");
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
}
