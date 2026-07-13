using System.Collections.Generic;
using System.Threading;

namespace OneTouchAutomation.Services.Capture;

public interface IContinuousCaptureService
{
    IAsyncEnumerable<CapturedFrame> CaptureFramesAsync(
        ContinuousCaptureRequest request,
        CancellationToken cancellationToken = default);
}
