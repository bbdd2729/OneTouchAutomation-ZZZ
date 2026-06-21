using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneTouchAutomation.Services.Capture;

namespace OneTouchAutomation.ViewModels;

public partial class VisionDebugViewModel : ViewModelBase
{
    private readonly IScreenCaptureService _screenCaptureService;

    [ObservableProperty] private CapturedFrame? _currentFrame;

    [ObservableProperty] private string _matchResultText = "No result";

    [ObservableProperty] private Bitmap? _previewImage;

    [ObservableProperty] private string _screenshotInfo = "No screenshot";

    [ObservableProperty] private byte[]? _templateBytes;

    [ObservableProperty] private Bitmap? _templateImage;

    [ObservableProperty] private string? _templatePath;

    [ObservableProperty] private double _threshold = 0.85;

    public VisionDebugViewModel(IScreenCaptureService screenCaptureService)
    {
        _screenCaptureService = screenCaptureService;
    }

    public VisionDebugViewModel() : this(new ScreenCaptureService()) { }

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
    private Task RunMatchAsync()
    {
        MatchResultText = "Match service not implemented yet.";
        Logs.Insert(0, "Run match clicked.");

        return Task.CompletedTask;
    }
}