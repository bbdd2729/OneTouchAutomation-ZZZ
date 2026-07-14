using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OneTouchAutomation.Services.Capture;

namespace OneTouchAutomation.Services.Automation.Daily;

public sealed class FileAutomationEvidenceStore : IAutomationEvidenceStore
{
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly string _rootDirectory;

    public FileAutomationEvidenceStore(IScreenCaptureService screenCaptureService, string? rootDirectory = null)
    {
        _screenCaptureService = screenCaptureService;
        _rootDirectory = rootDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneTouchAutomation",
            "evidence");
    }

    public async Task<string?> SaveWindowCaptureAsync(
        IntPtr windowHandle,
        string workflowId,
        DateOnly gameDay,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.Combine(_rootDirectory, gameDay.ToString("yyyyMMdd"), workflowId, DateTimeOffset.UtcNow.ToString("HHmmssfff"));
            Directory.CreateDirectory(directory);
            var frame = await _screenCaptureService.CaptureWindowClientAsync(windowHandle, cancellationToken);

            await File.WriteAllBytesAsync(Path.Combine(directory, "screen.png"), frame.PngBytes, cancellationToken);
            var metadata = new { reason, frame.Width, frame.Height, frame.CapturedAt, frame.SourceName, frame.SourceX, frame.SourceY };
            await File.WriteAllTextAsync(
                Path.Combine(directory, "metadata.json"),
                JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }),
                cancellationToken);

            return directory;
        }
        catch
        {
            // Failure evidence must never hide the original workflow failure.
            return null;
        }
    }
}
