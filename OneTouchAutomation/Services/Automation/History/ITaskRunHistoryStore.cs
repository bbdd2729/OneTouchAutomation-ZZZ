using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Automation.History;

public interface ITaskRunHistoryStore
{
    Task SaveAsync(TaskRunHistoryEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskRunHistoryEntry>> LoadRecentAsync(
        int maximumCount = 20,
        CancellationToken cancellationToken = default);
}
