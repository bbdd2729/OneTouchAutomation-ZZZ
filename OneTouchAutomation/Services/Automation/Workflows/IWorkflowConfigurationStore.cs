using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Automation.Workflows;

public interface IWorkflowConfigurationStore
{
    Task<IReadOnlyList<WorkflowDefinition>> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(IReadOnlyCollection<WorkflowDefinition> workflows, CancellationToken cancellationToken = default);
}
