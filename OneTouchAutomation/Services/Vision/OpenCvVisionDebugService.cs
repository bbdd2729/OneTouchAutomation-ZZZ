using System.Collections.Generic;
using System.Linq;
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
        using var  debug    = source.Clone();

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

        var searchRegion = new Rect
            (0,
             0,
             source.Width,
             source.Height);
        var offsetX = 0;
        var offsetY = 0;

        if (request.UseRegion)
        {
            searchRegion = ClampRegion
                (new Rect
                     (
                      request.RegionX,
                      request.RegionY,
                      request.RegionWidth,
                      request.RegionHeight),
                 source.Width,
                 source.Height);

            offsetX = searchRegion.X;
            offsetY = searchRegion.Y;

            Cv2.Rectangle
                (debug,
                 searchRegion,
                 Scalar.DeepSkyBlue,
                 2);
        }

        using var searchSource = new Mat(source, searchRegion);

        using var sourceGray   = new Mat();
        using var templateGray = new Mat();

        Cv2.CvtColor(searchSource, sourceGray, ColorConversionCodes.BGR2GRAY);
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

        var matches = new List<TemplateMatchItem>();

        for (int y = 0; y < result.Rows; y++)
        {
            for (int x = 0; x < result.Cols; x++)
            {
                var matchScore = result.At<float>(y, x);

                if (matchScore < request.Threshold)
                {
                    continue;
                }

                var bounds = new Rect
                    (
                     x + offsetX,
                     y + offsetY,
                     template.Width,
                     template.Height);

                if (matches.Any(m => IsOverlapping(m.MatchBounds, bounds)))
                {
                    continue;
                }

                matches.Add
                    (new TemplateMatchItem
                    {
                        MatchScore  = matchScore,
                        MatchBounds = bounds
                    });
            }
        }

        foreach (var match in matches)
        {
            Cv2.Rectangle
                (
                 debug,
                 match.MatchBounds,
                 Scalar.LimeGreen,
                 2);

            Cv2.PutText
                (
                 debug,
                 $"{match.MatchScore:0.000}",
                 new Point(match.MatchBounds.X, Math.Max(20, match.MatchBounds.Y - 8)),
                 HersheyFonts.HersheySimplex,
                 0.6,
                 Scalar.LimeGreen,
                 2);
        }

        Cv2.ImEncode(".png", debug, out var debugBytes);

        var best = matches.OrderByDescending(m => m.MatchScore).FirstOrDefault();

        return Task.FromResult
            (new TemplateMatchResult
            {
                IsMatch            = matches.Count > 0,
                MatchScore         = best?.MatchScore ?? 0,
                MatchBounds        = best?.MatchBounds ?? default,
                Matches            = matches,
                DebugImagePngBytes = debugBytes,
                Message = matches.Count > 0
                    ? $"Matched {matches.Count} item(s)."
                    : "No match above threshold.",
            });
    }

    private static bool IsOverlapping(Rect a, Rect b)
    {
        var intersection     = a & b;
        var intersectionArea = intersection.Width * intersection.Height;

        if (intersectionArea <= 0)
        {
            return false;
        }

        var minArea = Math.Min(a.Width * a.Height, b.Width * b.Height);
        return intersectionArea > minArea * 0.5;
    }

    private static Rect ClampRegion(Rect region, int sourceWidth, int sourceHeight)
    {
        var x = Math.Clamp(region.X, 0, sourceWidth - 1);
        var y = Math.Clamp(region.Y, 0, sourceHeight - 1);

        var width  = Math.Clamp(region.Width, 1, sourceWidth - x);
        var height = Math.Clamp(region.Height, 1, sourceHeight - y);

        return new Rect
            (x,
             y,
             width,
             height);
    }
}