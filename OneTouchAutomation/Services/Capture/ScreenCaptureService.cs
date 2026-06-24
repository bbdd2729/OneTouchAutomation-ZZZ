using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
        graphics.CopyFromScreen
            (bounds.Left,
             bounds.Top,
             0,
             0,
             bitmap.Size);

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

    public Task<CapturedFrame> CaptureWindowAsync
        (string windowTitleKeyword, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(windowTitleKeyword))
        {
            throw new ArgumentException("Window title keyword is required.", nameof(windowTitleKeyword));
        }

        var window = FindWindowByTitleKeyword(windowTitleKeyword);

        if (window is null)
        {
            throw new InvalidOperationException($"Window not found: {windowTitleKeyword}");
        }

        var bounds = new Rectangle
            (
             window.Value.Rect.Left,
             window.Value.Rect.Top,
             window.Value.Rect.Right - window.Value.Rect.Left,
             window.Value.Rect.Bottom - window.Value.Rect.Top);

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidOperationException($"Window has invalid bounds: {window.Value.Title}");
        }

        return Task.FromResult
            (CaptureBounds
                 (
                  bounds,
                  window.Value.Title));
    }

    private static CapturedFrame CaptureBounds(Rectangle bounds, string sourceName)
    {
        using var bitmap   = new Bitmap(bounds.Width, bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.CopyFromScreen
            (
             bounds.Left,
             bounds.Top,
             0,
             0,
             bitmap.Size);

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);

        return new CapturedFrame
        {
            PngBytes   = stream.ToArray(),
            Width      = bounds.Width,
            Height     = bounds.Height,
            CapturedAt = DateTimeOffset.Now,
            SourceName = sourceName,
            SourceX    = bounds.Left,
            SourceY    = bounds.Top
        };
    }

    private static WindowSearchResult? FindWindowByTitleKeyword(string keyword)
    {
        var windows = new List<WindowSearchResult>();

        EnumWindows
            ((hWnd, _) =>
             {
                 if (!IsWindowVisible(hWnd))
                 {
                     return true;
                 }

                 var title = GetWindowTitle(hWnd);

                 if (string.IsNullOrWhiteSpace(title))
                 {
                     return true;
                 }

                 if (!title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                 {
                     return true;
                 }

                 if (!GetWindowRect(hWnd, out var rect))
                 {
                     return true;
                 }

                 windows.Add(new WindowSearchResult(hWnd, title, rect));
                 return true;
             },
             IntPtr.Zero);

        return windows.Count > 0 ? windows[0] : null;
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        var length = GetWindowTextLength(hWnd);

        if (length <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        GetWindowText(hWnd, builder, builder.Capacity);

        return builder.ToString();
    }

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect lpRect);

    private readonly record struct WindowSearchResult(
        IntPtr Handle,
        string Title,
        NativeRect Rect);

    private readonly struct NativeRect
    {
        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;

        public NativeRect(int left, int top, int right, int bottom)
        {
            Left   = left;
            Top    = top;
            Right  = right;
            Bottom = bottom;
        }
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
}