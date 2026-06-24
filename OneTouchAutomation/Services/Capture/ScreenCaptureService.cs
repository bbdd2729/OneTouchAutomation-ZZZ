using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
        cancellationToken.ThrowIfCancellationRequested();

        Rectangle bounds = Screen.PrimaryScreen!.Bounds;

        return Task.FromResult(CaptureBounds(bounds, "Primary Screen"));
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

        var bounds = ToRectangle(window.Value.Rect);

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

    public Task<IReadOnlyList<CaptureWindowInfo>> ListWindowsAsync
    (
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var windows = EnumerateWindows().Select(x => ToCaptureWindowInfo(x)).Where
            (x => x.Width > 0 && x.Height > 0).OrderBy(x => x.Title).ToList();

        return Task.FromResult<IReadOnlyList<CaptureWindowInfo>>(windows);
    }

    public Task<CapturedFrame> CaptureWindowAsync
    (
        IntPtr windowHandle,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var window = EnumerateWindows().FirstOrDefault(x => x.Handle == windowHandle);

        if (window.Handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Window not found.");
        }

        var bounds = ToRectangle(window.Rect);

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidOperationException($"Window has invalid bounds: {window.Title}");
        }

        return Task.FromResult(CaptureBounds(bounds, window.Title));
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
        return EnumerateWindows().FirstOrDefault(x => x.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static List<WindowSearchResult> EnumerateWindows()
    {
        var windows = new List<WindowSearchResult>();

        EnumWindows
            (
             (hWnd, _) =>
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

                 if (!GetWindowRect(hWnd, out var rect))
                 {
                     return true;
                 }

                 var width  = rect.Right - rect.Left;
                 var height = rect.Bottom - rect.Top;

                 if (width <= 0 || height <= 0)
                 {
                     return true;
                 }

                 windows.Add(new WindowSearchResult(hWnd, title, rect));
                 return true;
             },
             IntPtr.Zero);

        return windows;
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

    private static Rectangle ToRectangle(NativeRect rect)
    {
        return new Rectangle
            (
             rect.Left,
             rect.Top,
             rect.Right - rect.Left,
             rect.Bottom - rect.Top);
    }

    private static CaptureWindowInfo ToCaptureWindowInfo(WindowSearchResult window)
    {
        var bounds = ToRectangle(window.Rect);

        return new CaptureWindowInfo
        {
            Handle = window.Handle,
            Title  = window.Title,
            X      = bounds.Left,
            Y      = bounds.Top,
            Width  = bounds.Width,
            Height = bounds.Height
        };
    }

    #region P/Invoke Declarations

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

    #endregion
}