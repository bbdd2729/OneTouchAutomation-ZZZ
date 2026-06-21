using System.Collections.Generic;

namespace OneTouchAutomation.Services.Vision;

using Rect = OpenCvSharp.Rect;

public sealed class TemplateMatchResult
{
    public bool                             IsMatch            { get; init; }
    public double                           MatchScore         { get; init; }
    public Rect                             MatchBounds        { get; init; }
    public IReadOnlyList<TemplateMatchItem> Matches            { get; init; } = [];
    public byte[]?                          MatchedRegionBytes { get; init; }
    public string                           Message            { get; init; } = string.Empty;
}