using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Debug;
using OneTouchAutomation.Services.Input;
using OneTouchAutomation.Services.Vision;

namespace OneTouchAutomation.ViewModels;

public partial class VisionDebugViewModel : ViewModelBase
{
    private TemplateMatchResult? _lastMatchResult;

    [ObservableProperty] private CaptureWindowInfo? _selectedWindow;

    [ObservableProperty] private string? _windowTitleKeyword;

    public ObservableCollection<string> Logs { get; } = new();

    public ObservableCollection<CaptureWindowInfo> Windows { get; } = new();


    #region Constructors

    public VisionDebugViewModel
    (IScreenCaptureService screenCaptureService,
     IVisionDebugService visionDebugService,
     IVisionDebugOutputService visionDebugOutputService,
     IInputService inputService)
    {
        _screenCaptureService = screenCaptureService;
        _visionDebugService   = visionDebugService;
        _debugOutputService   = visionDebugOutputService;
        _inputService         = inputService;
    }

    public VisionDebugViewModel() : this
        (new ScreenCaptureService(),
         new OpenCvVisionDebugService(),
         new VisionDebugOutputService(),
         new WindowsInputService()) { }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task RefreshWindowsAsync()
    {
        Windows.Clear();

        var windows = await _screenCaptureService.ListWindowsAsync();

        foreach (var window in windows)
        {
            Windows.Add(window);
        }

        SelectedWindow = Windows.FirstOrDefault();

        Logs.Insert(0, $"Loaded {Windows.Count} window(s).");
    }

    [RelayCommand]
    private async Task CaptureScreenAsync()
    {
        CurrentFrame = await _screenCaptureService.CaptureScreenAsync();

        using var stream = new MemoryStream(CurrentFrame.PngBytes);
        PreviewImage = new Bitmap(stream);

        ScreenshotInfo =
            $"{CurrentFrame.Width} x {CurrentFrame.Height} " +
            $"- {CurrentFrame.SourceName} - {CurrentFrame.CapturedAt:HH:mm:ss}";
        Logs.Insert(0, $"Captured screen: {ScreenshotInfo}");
    }

    [RelayCommand]
    private async Task CaptureWindowAsync()
    {
        if (string.IsNullOrWhiteSpace(WindowTitleKeyword))
        {
            Logs.Insert(0, "Window title keyword is required.");
            return;
        }

        try
        {
            CurrentFrame = await _screenCaptureService.CaptureWindowAsync(WindowTitleKeyword);

            using var stream = new MemoryStream(CurrentFrame.PngBytes);
            PreviewImage = new Bitmap(stream);

            ScreenshotInfo =
                $"{CurrentFrame.Width} x {CurrentFrame.Height} - {CurrentFrame.SourceName} - {CurrentFrame.CapturedAt:HH:mm:ss}";

            Logs.Insert(0, $"Captured window: {ScreenshotInfo}");
        }
        catch (Exception ex)
        {
            MatchResultText = ex.Message;
            Logs.Insert(0, $"Capture window failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CaptureSelectedWindowAsync()
    {
        if (SelectedWindow is null)
        {
            Logs.Insert(0, "Select a window first.");
            return;
        }

        try
        {
            CurrentFrame = await _screenCaptureService.CaptureWindowAsync(SelectedWindow.Handle);

            using var stream = new MemoryStream(CurrentFrame.PngBytes);
            PreviewImage = new Bitmap(stream);

            ScreenshotInfo =
                $"{CurrentFrame.Width} x {CurrentFrame.Height} - {CurrentFrame.SourceName} - {CurrentFrame.CapturedAt:HH:mm:ss}";

            Logs.Insert(0, $"Captured selected window: {ScreenshotInfo}");
        }
        catch (Exception ex)
        {
            MatchResultText = ex.Message;
            Logs.Insert(0, $"Capture selected window failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CaptureSelectedWindowClientAsync()
    {
        if (SelectedWindow is null)
        {
            Logs.Insert(0, "Select a window first.");
            return;
        }

        try
        {
            CurrentFrame = await _screenCaptureService.CaptureWindowClientAsync(SelectedWindow.Handle);

            using var stream = new MemoryStream(CurrentFrame.PngBytes);
            PreviewImage = new Bitmap(stream);

            ScreenshotInfo =
                $"{CurrentFrame.Width} x {CurrentFrame.Height} - {CurrentFrame.SourceName} - {CurrentFrame.CapturedAt:HH:mm:ss}";

            Logs.Insert(0, $"Captured selected window client: {ScreenshotInfo}");
        }
        catch (Exception ex)
        {
            MatchResultText = ex.Message;
            Logs.Insert(0, $"Capture selected window client failed: {ex.Message}");
        }
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

                UseRegion    = UseRegion,
                RegionX      = RegionX,
                RegionY      = RegionY,
                RegionWidth  = RegionWidth,
                RegionHeight = RegionHeight,
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
    private async Task ClickBestMatchAsync()
    {
        if (CurrentFrame is null)
        {
            Logs.Insert(0, "Capture screen first.");
            return;
        }

        if (_lastMatchResult is null || !_lastMatchResult.IsMatch)
        {
            Logs.Insert(0, "Run match first.");
            return;
        }

        var bounds = _lastMatchResult.MatchBounds;

        var screenX = CurrentFrame.SourceX + bounds.X + bounds.Width / 2;
        var screenY = CurrentFrame.SourceY + bounds.Y + bounds.Height / 2;

        try
        {
            await _inputService.ClickMatchCenterAsync(CurrentFrame, _lastMatchResult);

            Logs.Insert
                (
                 0,
                 $"Clicked best match: screen=({screenX}, {screenY}), local=({bounds.X + bounds.Width / 2}, {bounds.Y + bounds.Height / 2})");
        }
        catch (Exception ex)
        {
            MatchResultText = ex.Message;
            Logs.Insert(0, $"Click best match failed: {ex.Message}");
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

    #endregion

    #region Services

    private readonly IVisionDebugOutputService _debugOutputService;

    private readonly IScreenCaptureService _screenCaptureService;

    private readonly IVisionDebugService _visionDebugService;

    private readonly IInputService _inputService;

    #endregion

    #region Bindable Properties

    [ObservableProperty] private CapturedFrame? _currentFrame;

    [ObservableProperty] private string _matchResultText = "No result";

    [ObservableProperty] private Bitmap? _previewImage;

    [ObservableProperty] private string _screenshotInfo = "No screenshot";

    [ObservableProperty] private byte[]? _templateBytes;

    [ObservableProperty] private Bitmap? _templateImage;

    [ObservableProperty] private string? _templatePath;

    [ObservableProperty] private double _threshold = 0.85;

    #endregion

    #region Region Selection Properties

    [ObservableProperty] private bool _useRegion;

    [ObservableProperty] private int _regionX;

    [ObservableProperty] private int _regionY;

    [ObservableProperty] private int _regionWidth = 400;

    [ObservableProperty] private int _regionHeight = 300;

    #endregion
}