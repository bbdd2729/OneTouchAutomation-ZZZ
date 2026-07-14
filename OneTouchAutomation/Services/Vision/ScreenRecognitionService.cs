using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OneTouchAutomation.Services.Vision;

public sealed class ScreenRecognitionService : IScreenRecognitionService
{
    private readonly IVisionDebugService _visionService;

    public ScreenRecognitionService(IVisionDebugService visionService)
    {
        _visionService = visionService;
    }

    public async Task<ScreenRecognitionResult> RecognizeAsync(
        byte[] sourceBytes,
        ScreenDefinition screen,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(screen);
        if(string.IsNullOrWhiteSpace(screen.Id))
        {
            throw new ArgumentException("Screen ID is required.", nameof(screen));
        }

        var requiredMatches = new List<TemplateMatchResult>();
        foreach(var element in screen.RequiredElements)
        {
            var match = await MatchAsync(sourceBytes, element, cancellationToken);
            requiredMatches.Add(match);
            if(!match.IsMatch)
            {
                return NotMatched(screen, requiredMatches, []);
            }
        }

        var excludedMatches = new List<TemplateMatchResult>();
        foreach(var element in screen.ExcludedElements)
        {
            var match = await MatchAsync(sourceBytes, element, cancellationToken);
            excludedMatches.Add(match);
            if(match.IsMatch)
            {
                return NotMatched(screen, requiredMatches, excludedMatches);
            }
        }

        return new ScreenRecognitionResult
        {
            ScreenId = screen.Id,
            IsMatch = true,
            StatusCode = screen.Id,
            Message = $"Screen matched: {screen.Name}.",
            RequiredMatches = requiredMatches,
            ExcludedMatches = excludedMatches
        };
    }

    private async Task<TemplateMatchResult> MatchAsync(
        byte[] sourceBytes,
        ScreenElementDefinition element,
        CancellationToken cancellationToken)
    {
        if(string.IsNullOrWhiteSpace(element.TemplatePath) || !File.Exists(element.TemplatePath))
        {
            return new TemplateMatchResult { IsMatch = false, Message = "Screen template file not found." };
        }

        return await _visionService.MatchTemplateAsync(new TemplateMatchRequest
        {
            SourceBytes = sourceBytes,
            TemplateBytes = await File.ReadAllBytesAsync(element.TemplatePath, cancellationToken),
            Threshold = element.Threshold,
            UseRegion = element.UseRegion,
            RegionX = element.RegionX,
            RegionY = element.RegionY,
            RegionWidth = element.RegionWidth,
            RegionHeight = element.RegionHeight
        }, cancellationToken);
    }

    private static ScreenRecognitionResult NotMatched(
        ScreenDefinition screen,
        IReadOnlyList<TemplateMatchResult> requiredMatches,
        IReadOnlyList<TemplateMatchResult> excludedMatches)
    {
        return new ScreenRecognitionResult
        {
            ScreenId = screen.Id,
            IsMatch = false,
            StatusCode = $"not-{screen.Id}",
            Message = $"Screen did not match: {screen.Name}.",
            RequiredMatches = requiredMatches,
            ExcludedMatches = excludedMatches
        };
    }
}
