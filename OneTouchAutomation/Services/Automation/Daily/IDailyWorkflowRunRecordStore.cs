using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Automation.Daily;

public interface IDailyWorkflowRunRecordStore
{
    Task<DailyWorkflowRunRecord?> GetAsync(
        string workflowId,
        DateOnly gameDay,
        CancellationToken cancellationToken = default);

    Task SaveAsync(DailyWorkflowRunRecord record, CancellationToken cancellationToken = default);
}
