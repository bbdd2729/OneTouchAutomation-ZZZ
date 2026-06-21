using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;

namespace OneTouchAutomation.Services.Vision;

public class OpenCvVisionDebugService : IVisionDebugService
{
    public Task<TemplateMatchResult> MatchTemplateAsync
    (
        TemplateMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using Mat  source   = Cv2.ImDecode(request.SourceBytes, ImreadModes.Color);
        using Mat? template = Cv2.ImDecode(request.TemplateBytes, ImreadModes.Color);

        if (source is null)
        {
            return Task.FromResult
                (new TemplateMatchResult
                {
                    IsMatch = false,
                    Message = "Failed to decode screen image."
                });
        }

        if (template is null)
        {
            return Task.FromResult
                (new TemplateMatchResult
                {
                    IsMatch = false,
                    Message = "Failed to decode template image."
                });
        }

        using var sourceGray   = new Mat();
        using var templateGray = new Mat();

        Cv2.CvtColor(source, sourceGray, ColorConversionCodes.BGR2GRAY);
        Cv2.CvtColor(template, templateGray, ColorConversionCodes.BGR2GRAY);

        using var result = new Mat();

        Cv2.MatchTemplate
            (sourceGray,
             templateGray,
             result,
             TemplateMatchModes.CCoeffNormed);

        Cv2.MinMaxLoc
            (result,
             out _,
             out double maxVal,
             out _,
             out Point maxLoc);

        Rect bounds = new Rect
            (
             maxLoc.X,
             maxLoc.Y,
             template.Width,
             template.Height);

        var isMatch = maxVal >= request.Threshold;

        using var debug = source.Clone();

        Cv2.Rectangle
            (debug,
             bounds,
             isMatch ? Scalar.Green : Scalar.Red,
             2);

        Cv2.PutText
            (
             debug,
             $"{maxVal:0.000}",
             new Point(bounds.X, Math.Max(20, bounds.Y - 8)),
             HersheyFonts.HersheySimplex,
             0.7,
             isMatch ? Scalar.LimeGreen : Scalar.Red,
             2);

        Cv2.ImEncode(".png", debug, out var debugBytes);

        return Task.FromResult
            (new TemplateMatchResult
            {
                IsMatch            = isMatch,
                MatchScore         = maxVal,
                MatchBounds        = bounds,
                MatchedRegionBytes = debugBytes,
                Message            = isMatch ? "Matched." : "No match above threshold.",
            });
    }
}