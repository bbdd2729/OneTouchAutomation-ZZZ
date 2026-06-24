using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Debug;
using OneTouchAutomation.Services.Vision;

namespace OneTouchAutomation.ViewModels;

public partial class VisionDebugViewModel : ViewModelBase
{
    private readonly IVisionDebugOutputService _debugOutputService;
    private readonly IScreenCaptureService     _screenCaptureService;

    private readonly IVisionDebugService _visionDebugService;

    [ObservableProperty] private CapturedFrame? _currentFrame;

    private TemplateMatchResult? _lastMatchResult;

    [ObservableProperty] private string _matchResultText = "No result";

    [ObservableProperty] private Bitmap? _previewImage;

    [ObservableProperty] private string _screenshotInfo = "No screenshot";

    [ObservableProperty] private byte[]? _templateBytes;

    [ObservableProperty] private Bitmap? _templateImage;

    [ObservableProperty] private string? _templatePath;

    [ObservableProperty] private double _threshold = 0.85;

    public VisionDebugViewModel
    (IScreenCaptureService screenCaptureService,
     IVisionDebugService visionDebugService,
     IVisionDebugOutputService visionDebugOutputService)
    {
        _screenCaptureService = screenCaptureService;
        _visionDebugService   = visionDebugService;
        _debugOutputService   = visionDebugOutputService;
    }

    public VisionDebugViewModel() : this
        (new ScreenCaptureService(),
         new OpenCvVisionDebugService(),
         new VisionDebugOutputService()) { }

    public ObservableCollection<string> Logs { get; } = new();

    [RelayCommand]
    private async Task CaptureScreenAsync()
    {
        CurrentFrame = await _screenCaptureService.CaptureScreenAsync();

        using var stream = new MemoryStream(CurrentFrame.PngBytes);
        PreviewImage = new Bitmap(stream);

        ScreenshotInfo = $"{CurrentFrame.Width} x {CurrentFrame.Height} - {CurrentFrame.CapturedAt:HH:mm:ss}";
        Logs.Insert(0, $"Captured screen: {ScreenshotInfo}");
    }

    [RelayCommand]
    private async Task LoadTemplateAsync()
    {
        if (string.IsNullOrWhiteSpace(TemplatePath) || !File.Exists(TemplatePath))
        {
            MatchResultText = "Template file not found.";
            return;
        }

        TemplateBytes = await File.ReadAllBytesAsync(TemplatePath);

        using var stream = new MemoryStream(TemplateBytes);
        TemplateImage = new Bitmap(stream);

        Logs.Insert(0, $"Loaded template: {TemplatePath}");
    }

    [RelayCommand]
    private async Task RunMatchAsync()
    {
        if (CurrentFrame is null)
        {
            MatchResultText = "Capture screen first.";
            Logs.Insert(0, MatchResultText);
            return;
        }

        if (TemplateBytes is null)
        {
            MatchResultText = "Load template first.";
            Logs.Insert(0, MatchResultText);
            return;
        }

        var result = await _visionDebugService.MatchTemplateAsync
            (new TemplateMatchRequest
            {
                SourceBytes   = CurrentFrame.PngBytes,
                TemplateBytes = TemplateBytes,
                Threshold     = Threshold,
            });

        MatchResultText =
            $"{result.Message} MatchScore={result.MatchScore:0.000}, Bounds={result.MatchBounds}";

        Logs.Insert(0, MatchResultText);

        _lastMatchResult = result;

        if (result.DebugImagePngBytes is not null)
        {
            using var stream = new MemoryStream(result.DebugImagePngBytes);
            PreviewImage = new Bitmap(stream);
        }
    }

    [RelayCommand]
    private async Task SaveDebugOutputAsync()
    {
        if (CurrentFrame is null)
        {
            Logs.Insert(0, "Capture screen first.");
            return;
        }

        if (TemplateBytes is null)
        {
            Logs.Insert(0, "Load template first.");
            return;
        }

        if (_lastMatchResult is null)
        {
            Logs.Insert(0, "Run match first.");
            return;
        }

        var dir = await _debugOutputService.SaveAsync
            (new VisionDebugOutputRequest
            {
                Frame         = CurrentFrame,
                TemplateBytes = TemplateBytes,
                TemplatePath  = TemplatePath,
                Threshold     = Threshold,
                Result        = _lastMatchResult,
                Logs          = Logs.ToArray()
            });

        Logs.Insert(0, $"Saved debug output: {dir}");
    }
}