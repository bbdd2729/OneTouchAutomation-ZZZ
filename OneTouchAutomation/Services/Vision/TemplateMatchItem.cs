namespace OneTouchAutomation.Services.Vision;

using Rect = OpenCvSharp.Rect;

public sealed class TemplateMatchItem
{
    public double MatchScore  { get; init; }
    public Rect   MatchBounds { get; init; }
}