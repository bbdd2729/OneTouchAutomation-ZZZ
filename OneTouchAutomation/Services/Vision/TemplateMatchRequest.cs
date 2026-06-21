namespace OneTouchAutomation.Services.Vision;

public class TemplateMatchRequest
{
    public required byte[] SourceBytes { get; init; } = Array.Empty<byte>();

    public required byte[] TemplateBytes { get; init; } = Array.Empty<byte>();

    public double Threshold { get; init; } = 0.85;
}