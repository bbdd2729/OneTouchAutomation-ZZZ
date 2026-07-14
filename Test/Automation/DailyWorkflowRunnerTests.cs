using OneTouchAutomation.Services.Automation.Daily;
using OneTouchAutomation.Services.Automation.Workflows;

namespace Test.Automation;

public sealed class DailyWorkflowRunnerTests
{
    [Fact]
    public async Task RunAsync_SkipsWorkflowThatAlreadySucceededForCurrentGameDay()
    {
        var store = new InMemoryRecordStore();
        var workflowId = "daily-reward";
        var gameDay = DateOnly.FromDateTime(DateTime.Now.Hour < 4 ? DateTime.Today.AddDays(-1) : DateTime.Today);
        await store.SaveAsync(new DailyWorkflowRunRecord
        {
            WorkflowId = workflowId,
            GameDay = gameDay,
            Status = DailyTaskRunStatus.Succeeded,
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CompletedAt = DateTimeOffset.UtcNow
        }, TestContext.Current.CancellationToken);
        var workflowRunner = new RecordingWorkflowRunner();
        var runner = new DailyWorkflowRunner(
            new FixedWorkflowStore(workflowId),
            workflowRunner,
            store,
            new NoopEvidenceStore());

        var result = await runner.RunAsync(IntPtr.Zero, workflowId, 4, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.WasSkipped);
        Assert.Equal(DailyTaskRunStatus.Succeeded, result.Record.Status);
        Assert.Equal(0, workflowRunner.RunCount);
    }

    [Fact]
    public async Task RunAsync_PersistsFailureAndEvidenceWhenWorkflowFails()
    {
        var workflowId = "daily-battle";
        var recordStore = new InMemoryRecordStore();
        var evidenceStore = new RecordingEvidenceStore();
        var runner = new DailyWorkflowRunner(
            new FixedWorkflowStore(workflowId),
            new RecordingWorkflowRunner(shouldSucceed: false),
            recordStore,
            evidenceStore);

        var result = await runner.RunAsync(IntPtr.Zero, workflowId, 4, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.WasSkipped);
        Assert.Equal(DailyTaskRunStatus.Failed, result.Record.Status);
        Assert.Equal("evidence", result.Record.EvidenceDirectory);
        Assert.Equal(1, evidenceStore.SaveCount);
    }

    private sealed class FixedWorkflowStore : IWorkflowConfigurationStore
    {
        private readonly WorkflowDefinition _workflow;

        public FixedWorkflowStore(string workflowId)
        {
            _workflow = new WorkflowDefinition { Id = workflowId, Name = workflowId };
        }

        public Task<IReadOnlyList<WorkflowDefinition>> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<WorkflowDefinition>>([_workflow]);
        }

        public Task SaveAsync(IReadOnlyCollection<WorkflowDefinition> workflows, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryRecordStore : IDailyWorkflowRunRecordStore
    {
        private readonly Dictionary<string, DailyWorkflowRunRecord> _records = [];

        public Task<DailyWorkflowRunRecord?> GetAsync(string workflowId, DateOnly gameDay, CancellationToken cancellationToken = default)
        {
            _records.TryGetValue($"{workflowId}:{gameDay:yyyyMMdd}", out var record);
            return Task.FromResult(record);
        }

        public Task SaveAsync(DailyWorkflowRunRecord record, CancellationToken cancellationToken = default)
        {
            _records[$"{record.WorkflowId}:{record.GameDay:yyyyMMdd}"] = record;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingWorkflowRunner : IWorkflowRunner
    {
        private readonly bool _shouldSucceed;

        public RecordingWorkflowRunner(bool shouldSucceed = true)
        {
            _shouldSucceed = shouldSucceed;
        }

        public int RunCount { get; private set; }

        public Task<WorkflowRunResult> RunAsync(IntPtr windowHandle, WorkflowDefinition workflow, Action<string>? log = null, CancellationToken cancellationToken = default)
        {
            RunCount++;
            return Task.FromResult(new WorkflowRunResult
            {
                FinalOutcome = _shouldSucceed ? WorkflowNodeOutcome.Success : WorkflowNodeOutcome.Failure
            });
        }
    }

    private sealed class NoopEvidenceStore : IAutomationEvidenceStore
    {
        public Task<string?> SaveWindowCaptureAsync(IntPtr windowHandle, string workflowId, DateOnly gameDay, string reason, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>(null);
        }
    }

    private sealed class RecordingEvidenceStore : IAutomationEvidenceStore
    {
        public int SaveCount { get; private set; }

        public Task<string?> SaveWindowCaptureAsync(IntPtr windowHandle, string workflowId, DateOnly gameDay, string reason, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult<string?>("evidence");
        }
    }
}
