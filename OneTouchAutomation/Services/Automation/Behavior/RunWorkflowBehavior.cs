using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneTouchAutomation.Services.Automation.Workflows;

namespace OneTouchAutomation.Services.Automation.Behavior;

public sealed class RunWorkflowBehavior : IAutomationBehavior<RunWorkflowBehaviorParameters>
{
    private readonly IServiceProvider _serviceProvider;

    public RunWorkflowBehavior(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Id => "run-workflow";

    public string Name => "Run Workflow";

    public string Description => "Load a saved workflow definition and execute its graph against the selected window.";

    public Type ParameterType => typeof(RunWorkflowBehaviorParameters);

    public Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        object parameters,
        CancellationToken cancellationToken = default)
    {
        if(parameters is not RunWorkflowBehaviorParameters typedParameters)
        {
            return Task.FromResult(new BehaviorExecutionResult
            {
                IsSuccess = false,
                Message = $"Invalid parameters for behavior: {Name}."
            });
        }

        return ExecuteAsync(context, typedParameters, cancellationToken);
    }

    public async Task<BehaviorExecutionResult> ExecuteAsync(
        BehaviorExecutionContext context,
        RunWorkflowBehaviorParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(parameters.WorkflowId))
        {
            return new BehaviorExecutionResult
            {
                IsSuccess = false,
                Message = "Workflow ID is required."
            };
        }

        var store = _serviceProvider.GetService(typeof(IWorkflowConfigurationStore)) as IWorkflowConfigurationStore;
        var runner = _serviceProvider.GetService(typeof(IWorkflowRunner)) as IWorkflowRunner;

        if(store is null || runner is null)
        {
            return new BehaviorExecutionResult
            {
                IsSuccess = false,
                Message = "Workflow services are not available."
            };
        }

        var workflow = (await store.LoadAsync(cancellationToken))
            .FirstOrDefault(item => item.Id == parameters.WorkflowId);

        if(workflow is null)
        {
            return new BehaviorExecutionResult
            {
                IsSuccess = false,
                Message = $"Workflow not found: {parameters.WorkflowId}."
            };
        }

        context.Log($"Running workflow: {workflow.Name}.");

        var result = await runner.RunAsync(context.WindowHandle, workflow, context.Log, cancellationToken);
        var lastStep = result.StepResults.LastOrDefault();

        return new BehaviorExecutionResult
        {
            IsSuccess = result.IsSuccess,
            Message = result.IsCancelled
                ? "Workflow cancelled."
                : result.IsSuccess
                    ? $"Workflow completed: {workflow.Name}."
                    : $"Workflow failed: {workflow.Name}.",
            MatchScore = lastStep?.BehaviorResult.MatchScore ?? 0,
            ScreenX = lastStep?.BehaviorResult.ScreenX,
            ScreenY = lastStep?.BehaviorResult.ScreenY
        };
    }
}
