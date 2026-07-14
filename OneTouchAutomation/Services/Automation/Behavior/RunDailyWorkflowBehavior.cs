using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneTouchAutomation.Services.Automation.Daily;

namespace OneTouchAutomation.Services.Automation.Behavior;

public sealed class RunDailyWorkflowBehavior : IAutomationBehavior<RunDailyWorkflowBehaviorParameters>
{
    private readonly IServiceProvider _serviceProvider;

    public RunDailyWorkflowBehavior(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Id => "run-daily-workflow";

    public string Name => "Run Daily Workflow";

    public string Description => "Run a workflow once per game day and retain evidence when it fails.";

    public Type ParameterType => typeof(RunDailyWorkflowBehaviorParameters);

    public Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        object parameters,
        CancellationToken cancellationToken = default)
    {
        return parameters is RunDailyWorkflowBehaviorParameters typedParameters
            ? ExecuteAsync(context, typedParameters, cancellationToken)
            : Task.FromResult(new BehaviorExecutionResult { IsSuccess = false, Message = $"Invalid parameters for behavior: {Name}." });
    }

    public async Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        RunDailyWorkflowBehaviorParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var runner = _serviceProvider.GetService(typeof(IDailyWorkflowRunner)) as IDailyWorkflowRunner;
        if(runner is null)
        {
            return new BehaviorExecutionResult { IsSuccess = false, Message = "Daily workflow services are not available." };
        }

        try
        {
            var result = await runner.RunAsync(
                context.WindowHandle,
                parameters.WorkflowId,
                parameters.GameRefreshHour,
                parameters.ForceRun,
                context.Log,
                cancellationToken);
            var lastStep = result.WorkflowResult?.StepResults.LastOrDefault();
            return new BehaviorExecutionResult
            {
                IsSuccess = result.WasSkipped || result.Record.Status == DailyTaskRunStatus.Succeeded,
                Message = result.WasSkipped ? "Daily workflow already completed; skipped." : result.Record.Message ?? "Daily workflow finished.",
                MatchScore = lastStep?.BehaviorResult.MatchScore ?? 0,
                ScreenX = lastStep?.BehaviorResult.ScreenX,
                ScreenY = lastStep?.BehaviorResult.ScreenY
            };
        }
        catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch(Exception exception)
        {
            return new BehaviorExecutionResult { IsSuccess = false, Message = $"Daily workflow failed: {exception.Message}" };
        }
    }
}
