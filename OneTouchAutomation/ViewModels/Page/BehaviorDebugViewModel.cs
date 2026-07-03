using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneTouchAutomation.Services.Automation.Behavior;
using OneTouchAutomation.Services.Capture;

namespace OneTouchAutomation.ViewModels;

public partial class BehaviorDebugViewModel : ViewModelBase
{
    private readonly IAutomationBehavior<ClickTemplateBehaviorParameters> _clickTemplateBehavior;
    private readonly IScreenCaptureService                                _screenCaptureService;

    [ObservableProperty] private int _regionHeight = 300;

    [ObservableProperty] private int _regionWidth = 400;

    [ObservableProperty] private int _regionX;

    [ObservableProperty] private int _regionY;

    [ObservableProperty] private string _resultText = "No result";

    [ObservableProperty] private CaptureWindowInfo? _selectedWindow;

    [ObservableProperty] private string? _templatePath;

    [ObservableProperty] private double _threshold = 0.85;

    [ObservableProperty] private bool _useRegion;

    public BehaviorDebugViewModel
    (
            IScreenCaptureService screenCaptureService,
            IAutomationBehavior<ClickTemplateBehaviorParameters> clickTemplateBehavior)
    {
        _screenCaptureService  = screenCaptureService;
        _clickTemplateBehavior = clickTemplateBehavior;
    }

    public BehaviorDebugViewModel()
            : this(new ScreenCaptureService(), null!) { }

    public ObservableCollection<CaptureWindowInfo> Windows { get; } = new();

    public ObservableCollection<string> Logs { get; } = new();

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
    private async Task RunClickTemplateBehaviorAsync()
    {
        if(SelectedWindow is null)
        {
            Logs.Insert(0, "Select a window first.");
            return;
        }

        if(string.IsNullOrWhiteSpace(TemplatePath))
        {
            Logs.Insert(0, "Template path is required.");
            return;
        }

        var parameters = new ClickTemplateBehaviorParameters
        {
                TemplatePath = TemplatePath,
                Threshold    = Threshold,
                UseRegion    = UseRegion,
                RegionX      = RegionX,
                RegionY      = RegionY,
                RegionWidth  = RegionWidth,
                RegionHeight = RegionHeight
        };

        var result = await _clickTemplateBehavior.ExecuteAsync
                (
                 new BehaviorExecutionContext
                 {
                         WindowHandle = SelectedWindow.Handle,
                         Log          = message => Logs.Insert(0, message)
                 },
                 parameters);

        ResultText =
                result.IsSuccess
                        ? $"Success. {result.Message} score={result.MatchScore:0.000}, screen=({result.ScreenX},{result.ScreenY})"
                        : $"Failed. {result.Message} score={result.MatchScore:0.000}";

        Logs.Insert(0, ResultText);
    }
}