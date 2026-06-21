using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OneTouchAutomation.Services.Capture;

public class ScreenCaptureService : IScreenCaptureService
{
    public Task<CapturedFrame> CaptureScreenAsync(CancellationToken cancellationToken = default)
    {
        Rectangle bounds = Screen.PrimaryScreen!.Bounds;

        using var bitmap   = new Bitmap(bounds.Width, bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bitmap.Size);

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);

        return Task.FromResult
            (new CapturedFrame
            {
                PngBytes   = stream.ToArray(),
                Width      = bounds.Width,
                Height     = bounds.Height,
                CapturedAt = DateTimeOffset.Now
            });
    }
}