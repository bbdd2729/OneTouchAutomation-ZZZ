using System.Collections.Generic;
using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Input;
using OneTouchAutomation.Services.Vision;

namespace OneTouchAutomation.Services.Debug;

public class VisionDebugOutputRequest
{
    public required CapturedFrame Frame { get; init; }

    public required byte[] TemplateBytes { get; init; }

    public required TemplateMatchResult Result { get; init; }

    public string? TemplatePath { get; init; }

    public double Threshold { get; init; }

    public string? OutputDirectory { get; init; }

    public MouseClickOptions? ClickOptions { get; init; }

    public IReadOnlyList<string> Logs { get; init; } = [];
}
