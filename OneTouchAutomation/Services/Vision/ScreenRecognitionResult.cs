using System.Collections.Generic;

namespace OneTouchAutomation.Services.Vision;

public sealed class ScreenRecognitionResult
{
    public required string ScreenId { get; init; }

    public required bool IsMatch { get; init; }

    public required string StatusCode { get; init; }

    public required string Message { get; init; }

    public IReadOnlyList<TemplateMatchResult> RequiredMatches { get; init; } = [];

    public IReadOnlyList<TemplateMatchResult> ExcludedMatches { get; init; } = [];
}
