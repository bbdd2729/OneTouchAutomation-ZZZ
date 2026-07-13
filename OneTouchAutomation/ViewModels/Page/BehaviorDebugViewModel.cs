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
    private readonly IAutomationBehavior<WaitForTemplateBehaviorParameters> _waitForTemplateBehavior;
    private readonly IBehaviorRegistry                                   _behaviorRegistry;
    private readonly IScreenCaptureService                                _screenCaptureService;

    [ObservableProperty] private int _regionHeight = 300;

    [ObservableProperty] private int _regionWidth = 400;

    [ObservableProperty] private int _regionX;

    [ObservableProperty] private int _regionY;

    [ObservableProperty] private string _resultText = "No result";

    [ObservableProperty] private IAutomationBehavior? _selectedBehavior;

    [ObservableProperty] private CaptureWindowInfo? _selectedWindow;

    [ObservableProperty] private string? _templatePath;

    [ObservableProperty] private double _threshold = 0.85;

    [ObservableProperty] private int _timeoutSeconds = 10;

    [ObservableProperty] private bool _useRegion;

    public BehaviorDebugViewModel
    (
            IScreenCaptureService screenCaptureService,
            IAutomationBehavior<ClickTemplateBehaviorParameters> clickTemplateBehavior,
            IAutomationBehavior<WaitForTemplateBehaviorParameters> waitForTemplateBehavior,
            IBehaviorRegistry behaviorRegistry)
    {
        _screenCaptureService  = screenCaptureService;
        _clickTemplateBehavior = clickTemplateBehavior;
        _waitForTemplateBehavior = waitForTemplateBehavior;
        _behaviorRegistry      = behaviorRegistry;

        foreach(var behavior in _behaviorRegistry.Behaviors)
        {
            Behaviors.Add(behavior);
        }

        SelectedBehavior = Behaviors.FirstOrDefault();
    }

    public BehaviorDebugViewModel()
            : this(new ScreenCaptureService(), null!, null!, new BehaviorRegistry([])) { }

    public ObservableCollection<IAutomationBehavior> Behaviors { get; } = new();

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
    private async Task RunSelectedBehaviorAsync()
    {
        if(SelectedBehavior is null)
        {
            Logs.Insert(0, "Select a behavior first.");
            return;
        }

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

        var context = new BehaviorExecutionContext
        {
            WindowHandle = SelectedWindow.Handle,
            Log = message => Logs.Insert(0, message)
        };

        BehaviorExecutionResult result;

        if(SelectedBehavior.Id == "click-template")
        {
            result = await _clickTemplateBehavior.ExecuteAsync(context, CreateClickParameters());
        }
        else if(SelectedBehavior.Id == "wait-for-template")
        {
            result = await _waitForTemplateBehavior.ExecuteAsync(context, CreateWaitParameters());
        }
        else
        {
            Logs.Insert(0, $"Behavior is not supported by this debug panel: {SelectedBehavior.Name}");
            return;
        }

        ResultText =
                result.IsSuccess
                        ? $"Success. {result.Message} score={result.MatchScore:0.000}, screen=({result.ScreenX},{result.ScreenY})"
                        : $"Failed. {result.Message} score={result.MatchScore:0.000}";

        Logs.Insert(0, ResultText);
    }

    private ClickTemplateBehaviorParameters CreateClickParameters()
    {
        return new ClickTemplateBehaviorParameters
        {
            TemplatePath = TemplatePath!,
            Threshold = Threshold,
            UseRegion = UseRegion,
            RegionX = RegionX,
            RegionY = RegionY,
            RegionWidth = RegionWidth,
            RegionHeight = RegionHeight
        };
    }

    private WaitForTemplateBehaviorParameters CreateWaitParameters()
    {
        return new WaitForTemplateBehaviorParameters
        {
            TemplatePath = TemplatePath!,
            Threshold = Threshold,
            UseRegion = UseRegion,
            RegionX = RegionX,
            RegionY = RegionY,
            RegionWidth = RegionWidth,
            RegionHeight = RegionHeight,
            TimeoutSeconds = TimeoutSeconds
        };
    }
}
