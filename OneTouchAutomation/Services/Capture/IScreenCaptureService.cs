using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Capture;

public interface IScreenCaptureService
{
    Task<CapturedFrame> CaptureScreenAsync(CancellationToken cancellationToken = default);

    Task<CapturedFrame> CaptureWindowAsync
    (
        string windowTitleKeyword,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CaptureWindowInfo>> ListWindowsAsync
    (
        CancellationToken cancellationToken = default);

    Task<CapturedFrame> CaptureWindowAsync
    (
        IntPtr windowHandle,
        CancellationToken cancellationToken = default);
}