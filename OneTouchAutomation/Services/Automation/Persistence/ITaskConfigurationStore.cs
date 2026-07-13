using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Automation.Persistence;

public interface ITaskConfigurationStore
{
    Task<IReadOnlyList<AutomationTaskConfiguration>> LoadAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        IReadOnlyCollection<AutomationTaskConfiguration> tasks,
        CancellationToken cancellationToken = default);
}
