using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Capture;

public interface IScreenCaptureService
{
    Task<CapturedFrame> CaptureScreenAsync(CancellationToken cancellationToken = default);
}