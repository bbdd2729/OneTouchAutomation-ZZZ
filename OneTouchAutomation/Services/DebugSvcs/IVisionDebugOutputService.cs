using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Debug;

public interface IVisionDebugOutputService
{
    Task<string> SaveAsync
    (
        VisionDebugOutputRequest request,
        CancellationToken cancellationToken = default);
}