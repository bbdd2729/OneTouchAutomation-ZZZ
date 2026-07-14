using CommunityToolkit.Mvvm.ComponentModel;
using OneTouchAutomation.Services.Automation.Tasks;
using OneTouchAutomation.Services.Input;

namespace OneTouchAutomation.Models;

public partial class AutomationTaskModel : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [ObservableProperty] private string _behaviorId = "click-template";

    public string BehaviorDisplayName => BehaviorId == "wait-for-template"
        ? "Wait For Template"
        : BehaviorId == "press-key"
            ? "Press Key"
        : BehaviorId == "delay"
            ? "Delay"
        : BehaviorId == "run-workflow"
            ? "Run Workflow"
        : BehaviorId == "run-daily-workflow"
            ? "Run Daily Workflow"
        : "Click Template";

    public bool RequiresTemplate => BehaviorId is "click-template" or "wait-for-template";

    public bool IsClickTemplate => BehaviorId == "click-template";

    public bool IsWaitForTemplate => BehaviorId == "wait-for-template";

    public bool IsPressKey => BehaviorId == "press-key";

    public bool IsDelay => BehaviorId == "delay";

    public bool IsRunWorkflow => BehaviorId == "run-workflow";

    public bool IsDailyWorkflow => BehaviorId == "run-daily-workflow";

    public bool IsWorkflowSelection => IsRunWorkflow || IsDailyWorkflow;

    [ObservableProperty] private string _name = "Click template";

    [ObservableProperty] private string _templatePath = string.Empty;

    [ObservableProperty] private double _threshold = 0.85;

    [ObservableProperty] private bool _useRegion;

    [ObservableProperty] private int _regionX;

    [ObservableProperty] private int _regionY;

    [ObservableProperty] private int _regionWidth = 400;

    [ObservableProperty] private int _regionHeight = 300;

    [ObservableProperty] private int _timeoutSeconds = 10;

    [ObservableProperty] private TaskFailurePolicy _failurePolicy = TaskFailurePolicy.Stop;

    [ObservableProperty] private int _maxRetryCount;

    [ObservableProperty] private AutomationKey _key = AutomationKey.Enter;

    [ObservableProperty] private int _delayMilliseconds = 500;

    [ObservableProperty] private int _clickOffsetX;

    [ObservableProperty] private int _clickOffsetY;

    [ObservableProperty] private MouseClickMode _clickMode = MouseClickMode.Single;

    [ObservableProperty] private int _clickRepeatCount = 1;

    [ObservableProperty] private int _clickIntervalMilliseconds = 100;

    [ObservableProperty] private int _clickHoldDurationMilliseconds = 60;

    [ObservableProperty] private string _workflowId = string.Empty;

    [ObservableProperty] private int _gameRefreshHour = 4;

    [ObservableProperty] private bool _forceDailyRun;

    [ObservableProperty] private bool _isEnabled = true;

    partial void OnBehaviorIdChanged(string value)
    {
        OnPropertyChanged(nameof(BehaviorDisplayName));
        OnPropertyChanged(nameof(RequiresTemplate));
        OnPropertyChanged(nameof(IsClickTemplate));
        OnPropertyChanged(nameof(IsWaitForTemplate));
        OnPropertyChanged(nameof(IsPressKey));
        OnPropertyChanged(nameof(IsDelay));
        OnPropertyChanged(nameof(IsRunWorkflow));
        OnPropertyChanged(nameof(IsDailyWorkflow));
        OnPropertyChanged(nameof(IsWorkflowSelection));
    }
}
