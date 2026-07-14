using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Capture;

public sealed class ContinuousCaptureService : IContinuousCaptureService
{
    private readonly IScreenCaptureService _screenCaptureService;

    public ContinuousCaptureService(IScreenCaptureService screenCaptureService)
    {
        _screenCaptureService = screenCaptureService;
    }

    public async IAsyncEnumerable<CapturedFrame> CaptureFramesAsync(
        ContinuousCaptureRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if(request.FramesPerSecond is < 1 or > 60)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.FramesPerSecond),
                "Capture frame rate must be between 1 and 60 FPS.");
        }

        var frameInterval = TimeSpan.FromSeconds(1d / request.FramesPerSecond);

        while(true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var startedAt = Stopwatch.GetTimestamp();

            yield return await CaptureFrameAsync(request, cancellationToken);

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var remaining = frameInterval - elapsed;

            if(remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining, cancellationToken);
            }
        }
    }

    private Task<CapturedFrame> CaptureFrameAsync(
        ContinuousCaptureRequest request,
        CancellationToken cancellationToken)
    {
        return request.Source switch
        {
            ContinuousCaptureSource.Screen => _screenCaptureService.CaptureScreenAsync(cancellationToken),
            ContinuousCaptureSource.Window => request.WindowHandle != IntPtr.Zero
                ? _screenCaptureService.CaptureWindowAsync(request.WindowHandle, cancellationToken)
                : _screenCaptureService.CaptureWindowAsync(
                    request.WindowTitleKeyword ?? string.Empty,
                    cancellationToken),
            ContinuousCaptureSource.WindowClient when request.WindowHandle != IntPtr.Zero =>
                _screenCaptureService.CaptureWindowClientAsync(request.WindowHandle, cancellationToken),
            ContinuousCaptureSource.WindowClient => throw new InvalidOperationException(
                "Select a window before capturing its client area."),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Source))
        };
    }
}
