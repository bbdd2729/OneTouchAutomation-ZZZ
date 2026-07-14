using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
    private CancellationTokenSource? _continuousCaptureCancellationSource;
    private TemplateMatchResult? _lastMatchResult;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartContinuousCaptureCommand))]
    private CaptureWindowInfo? _selectedWindow;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartContinuousCaptureCommand))]
    private string? _windowTitleKeyword;

    public ObservableCollection<string> Logs { get; } = new();

    public ObservableCollection<CaptureWindowInfo> Windows { get; } = new();


    #region Constructors

    public VisionDebugViewModel
    (IScreenCaptureService screenCaptureService,
     IContinuousCaptureService continuousCaptureService,
     IVisionDebugService visionDebugService,
     IVisionDebugOutputService visionDebugOutputService,
     IInputService inputService,
     IWindowActivationService windowActivationService)
    {
        _screenCaptureService = screenCaptureService;
        _continuousCaptureService = continuousCaptureService;
        _visionDebugService   = visionDebugService;
        _debugOutputService   = visionDebugOutputService;
        _inputService         = inputService;
        _windowActivationService = windowActivationService;
    }

    public VisionDebugViewModel() : this
        (new ScreenCaptureService(),
         new ContinuousCaptureService(new ScreenCaptureService()),
         new OpenCvVisionDebugService(),
         new VisionDebugOutputService(),
         new WindowsInputService(),
         new WindowsWindowActivationService()) { }

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
        var frame = await _screenCaptureService.CaptureScreenAsync();
        ShowFrame(frame);
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
            var frame = await _screenCaptureService.CaptureWindowAsync(WindowTitleKeyword);
            ShowFrame(frame);

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
            var frame = await _screenCaptureService.CaptureWindowAsync(SelectedWindow.Handle);
            ShowFrame(frame);

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
            var frame = await _screenCaptureService.CaptureWindowClientAsync(SelectedWindow.Handle);
            ShowFrame(frame);

            Logs.Insert(0, $"Captured selected window client: {ScreenshotInfo}");
        }
        catch (Exception ex)
        {
            MatchResultText = ex.Message;
            Logs.Insert(0, $"Capture selected window client failed: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartContinuousCapture))]
    private void StartContinuousCapture()
    {
        _continuousCaptureCancellationSource = new CancellationTokenSource();
        IsContinuousCapturing = true;
        _ = CaptureContinuouslyAsync(_continuousCaptureCancellationSource);
    }

    [RelayCommand(CanExecute = nameof(CanStopContinuousCapture))]
    private void StopContinuousCapture()
    {
        _continuousCaptureCancellationSource?.Cancel();
    }

    [RelayCommand]
    private async Task LoadTemplateAsync()
    {
        await LoadTemplateFromPathAsync(TemplatePath);
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

        var clickOptions = CreateClickOptions();
        var screenX = CurrentFrame.SourceX + bounds.X + bounds.Width / 2 + clickOptions.OffsetX;
        var screenY = CurrentFrame.SourceY + bounds.Y + bounds.Height / 2 + clickOptions.OffsetY;

        try
        {
            if(SelectedWindow is not null)
            {
                await _windowActivationService.ActivateAsync(SelectedWindow.Handle);
            }

            await _inputService.ClickMatchAsync(CurrentFrame, _lastMatchResult, clickOptions);

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
                OutputDirectory = DebugOutputDirectory,
                ClickOptions = CreateClickOptions(),
                Result        = _lastMatchResult,
                Logs          = Logs.ToArray()
            });

        Logs.Insert(0, $"Saved debug output: {dir}");
    }

    #endregion

    #region Services

    private readonly IVisionDebugOutputService _debugOutputService;

    private readonly IContinuousCaptureService _continuousCaptureService;

    private readonly IScreenCaptureService _screenCaptureService;

    private readonly IVisionDebugService _visionDebugService;

    private readonly IInputService _inputService;

    private readonly IWindowActivationService _windowActivationService;

    #endregion

    #region Bindable Properties

    [ObservableProperty] private CapturedFrame? _currentFrame;

    [ObservableProperty] private string _matchResultText = "No result";

    [ObservableProperty] private Bitmap? _previewImage;

    [ObservableProperty] private string _screenshotInfo = "No screenshot";

    [ObservableProperty] private string? _debugOutputDirectory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartContinuousCaptureCommand))]
    private ContinuousCaptureSource _selectedContinuousCaptureSource = ContinuousCaptureSource.WindowClient;

    public IReadOnlyList<ContinuousCaptureSource> ContinuousCaptureSources { get; } =
        Enum.GetValues<ContinuousCaptureSource>();

    [ObservableProperty] private int _captureFramesPerSecond = 20;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartContinuousCaptureCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopContinuousCaptureCommand))]
    private bool _isContinuousCapturing;

    [ObservableProperty] private int _clickOffsetX;

    [ObservableProperty] private int _clickOffsetY;

    [ObservableProperty] private MouseClickMode _clickMode = MouseClickMode.Single;

    public IReadOnlyList<MouseClickMode> ClickModes { get; } = Enum.GetValues<MouseClickMode>();

    [ObservableProperty] private int _clickRepeatCount = 1;

    [ObservableProperty] private int _clickIntervalMilliseconds = 100;

    [ObservableProperty] private int _clickHoldDurationMilliseconds = 60;

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

    public async Task LoadTemplateFromPathAsync(string? templatePath)
    {
        if(string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
        {
            MatchResultText = "Template file not found.";
            return;
        }

        TemplatePath = templatePath;
        TemplateBytes = await File.ReadAllBytesAsync(templatePath);

        using var stream = new MemoryStream(TemplateBytes);
        TemplateImage = new Bitmap(stream);

        Logs.Insert(0, $"Loaded template: {TemplatePath}");
    }

    public void SetDebugOutputDirectory(string directory)
    {
        DebugOutputDirectory = directory;
    }

    private MouseClickOptions CreateClickOptions()
    {
        return new MouseClickOptions
        {
            OffsetX = ClickOffsetX,
            OffsetY = ClickOffsetY,
            Mode = ClickMode,
            RepeatCount = ClickRepeatCount,
            IntervalMilliseconds = ClickIntervalMilliseconds,
            HoldDurationMilliseconds = ClickHoldDurationMilliseconds
        };
    }

    private async Task CaptureContinuouslyAsync(CancellationTokenSource cancellationSource)
    {
        try
        {
            var request = new ContinuousCaptureRequest
            {
                Source = SelectedContinuousCaptureSource,
                WindowHandle = SelectedWindow?.Handle ?? IntPtr.Zero,
                WindowTitleKeyword = WindowTitleKeyword,
                FramesPerSecond = CaptureFramesPerSecond
            };

            await foreach(var frame in _continuousCaptureService.CaptureFramesAsync(
                              request,
                              cancellationSource.Token))
            {
                ShowFrame(frame, isContinuous: true);
            }
        }
        catch(OperationCanceledException) when(cancellationSource.IsCancellationRequested)
        {
            Logs.Insert(0, "Continuous capture stopped.");
        }
        catch(Exception exception)
        {
            MatchResultText = exception.Message;
            Logs.Insert(0, $"Continuous capture failed: {exception.Message}");
        }
        finally
        {
            if(ReferenceEquals(_continuousCaptureCancellationSource, cancellationSource))
            {
                _continuousCaptureCancellationSource = null;
                IsContinuousCapturing = false;
            }

            cancellationSource.Dispose();
        }
    }

    private bool CanStartContinuousCapture()
    {
        return !IsContinuousCapturing
            && (SelectedContinuousCaptureSource == ContinuousCaptureSource.Screen
                || SelectedWindow is not null
                || (SelectedContinuousCaptureSource == ContinuousCaptureSource.Window
                    && !string.IsNullOrWhiteSpace(WindowTitleKeyword)));
    }

    private bool CanStopContinuousCapture()
    {
        return IsContinuousCapturing;
    }

    private void ShowFrame(CapturedFrame frame, bool isContinuous = false)
    {
        CurrentFrame = frame;

        using var stream = new MemoryStream(frame.PngBytes);
        PreviewImage = new Bitmap(stream);
        ScreenshotInfo =
            $"{frame.Width} x {frame.Height} - {frame.SourceName} - {frame.CapturedAt:HH:mm:ss}" +
            (isContinuous ? $" - Live {CaptureFramesPerSecond} FPS" : string.Empty);
    }
}
