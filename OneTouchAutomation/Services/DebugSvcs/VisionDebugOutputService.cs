using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Rect = OpenCvSharp.Rect;

namespace OneTouchAutomation.Services.Debug;

public class VisionDebugOutputService : IVisionDebugOutputService
{
    public async Task<string> SaveAsync
    (
        VisionDebugOutputRequest request,
        CancellationToken cancellationToken = default)
    {
        var root = Path.Combine
            (
             Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
             "OneTouchAutomation",
             "debug",
             "vision");

        var dir = Path.Combine(root, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(dir);

        await File.WriteAllBytesAsync
            (
             Path.Combine(dir, "source.png"),
             request.Frame.PngBytes,
             cancellationToken);

        await File.WriteAllBytesAsync
            (
             Path.Combine(dir, "template.png"),
             request.TemplateBytes,
             cancellationToken);

        if (request.Result.DebugImagePngBytes is not null)
        {
            await File.WriteAllBytesAsync
                (
                 Path.Combine(dir, "debug.png"),
                 request.Result.DebugImagePngBytes,
                 cancellationToken);
        }

        var json = JsonSerializer.Serialize
            (
             new
             {
                 request.Frame.Width,
                 request.Frame.Height,
                 CapturedAt = request.Frame.CapturedAt,
                 request.TemplatePath,
                 request.Threshold,
                 request.Result.IsMatch,
                 request.Result.MatchScore,
                 MatchBounds = ToDto(request.Result.MatchBounds),
                 Matches = request.Result.Matches.Select
                     (x => new
                     {
                         x.MatchScore,
                         MatchBounds = ToDto(x.MatchBounds)
                     }),
                 request.Result.Message
             },
             new JsonSerializerOptions
             {
                 WriteIndented = true
             });

        await File.WriteAllTextAsync
            (
             Path.Combine(dir, "result.json"),
             json,
             cancellationToken);

        await File.WriteAllLinesAsync
            (
             Path.Combine(dir, "log.txt"),
             request.Logs,
             cancellationToken);

        return dir;
    }

    private static object ToDto(Rect rect)
    {
        return new
        {
            rect.X,
            rect.Y,
            rect.Width,
            rect.Height
        };
    }
}