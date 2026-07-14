using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneTouchAutomation.Services.Automation.Workflows;

namespace OneTouchAutomation.Services.Automation.Daily;

public sealed class DailyWorkflowRunner : IDailyWorkflowRunner
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();
    private readonly IWorkflowConfigurationStore _workflowStore;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IDailyWorkflowRunRecordStore _recordStore;
    private readonly IAutomationEvidenceStore _evidenceStore;

    public DailyWorkflowRunner(
        IWorkflowConfigurationStore workflowStore,
        IWorkflowRunner workflowRunner,
        IDailyWorkflowRunRecordStore recordStore,
        IAutomationEvidenceStore evidenceStore)
    {
        _workflowStore = workflowStore;
        _workflowRunner = workflowRunner;
        _recordStore = recordStore;
        _evidenceStore = evidenceStore;
    }

    public async Task<DailyWorkflowRunResult> RunAsync(
        IntPtr windowHandle,
        string workflowId,
        int gameRefreshHour,
        bool forceRun = false,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(workflowId))
        {
            throw new ArgumentException("Workflow ID is required.", nameof(workflowId));
        }

        if(gameRefreshHour is < 0 or > 23)
        {
            throw new ArgumentOutOfRangeException(nameof(gameRefreshHour), "Game refresh hour must be between 0 and 23.");
        }

        var gameDay = GetGameDay(DateTimeOffset.Now, gameRefreshHour);
        var runLock = Locks.GetOrAdd($"{workflowId}:{gameDay:yyyyMMdd}", _ => new SemaphoreSlim(1, 1));
        await runLock.WaitAsync(cancellationToken);

        try
        {
            var existing = await _recordStore.GetAsync(workflowId, gameDay, cancellationToken);
            if(!forceRun && existing?.Status == DailyTaskRunStatus.Succeeded)
            {
                log?.Invoke($"Daily workflow already completed for {gameDay:yyyy-MM-dd}; skipping.");
                return new DailyWorkflowRunResult { WasSkipped = true, Record = existing };
            }

            var workflow = (await _workflowStore.LoadAsync(cancellationToken)).FirstOrDefault(item => item.Id == workflowId)
                ?? throw new InvalidOperationException($"Workflow not found: {workflowId}.");
            var startedAt = DateTimeOffset.UtcNow;
            await _recordStore.SaveAsync(new DailyWorkflowRunRecord
            {
                WorkflowId = workflowId,
                GameDay = gameDay,
                Status = DailyTaskRunStatus.Running,
                StartedAt = startedAt
            }, cancellationToken);

            log?.Invoke($"Running daily workflow '{workflow.Name}' for {gameDay:yyyy-MM-dd}.");
            var result = await _workflowRunner.RunAsync(windowHandle, workflow, log, cancellationToken);
            var status = result.IsSuccess
                ? DailyTaskRunStatus.Succeeded
                : result.IsCancelled ? DailyTaskRunStatus.Cancelled : DailyTaskRunStatus.Failed;
            var message = result.IsSuccess ? "Workflow completed." : result.IsCancelled ? "Workflow cancelled." : "Workflow failed.";
            var evidenceDirectory = result.IsSuccess
                ? null
                : await _evidenceStore.SaveWindowCaptureAsync(windowHandle, workflowId, gameDay, message, CancellationToken.None);
            var record = new DailyWorkflowRunRecord
            {
                WorkflowId = workflowId,
                GameDay = gameDay,
                Status = status,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                Message = message,
                EvidenceDirectory = evidenceDirectory
            };
            await _recordStore.SaveAsync(record, CancellationToken.None);
            return new DailyWorkflowRunResult { Record = record, WorkflowResult = result };
        }
        finally
        {
            runLock.Release();
        }
    }

    internal static DateOnly GetGameDay(DateTimeOffset now, int gameRefreshHour)
    {
        return DateOnly.FromDateTime((now.Hour < gameRefreshHour ? now.AddDays(-1) : now).Date);
    }
}
