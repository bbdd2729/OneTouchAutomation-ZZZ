using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OneTouchAutomation.Services.Automation.Behavior;
using OneTouchAutomation.Services.Input;
using OneTouchAutomation.Services.Automation.Workflows;
using OneTouchAutomation.Services.Vision;

namespace OneTouchAutomation.Models;

public partial class WorkflowEditorModel : ObservableObject
{
    [ObservableProperty] private string _id = Guid.NewGuid().ToString("N");

    [ObservableProperty] private string _name = "New workflow";

    public ObservableCollection<WorkflowNodeEditorModel> Nodes { get; } = new();

    public ObservableCollection<WorkflowTransitionEditorModel> Transitions { get; } = new();

    public static WorkflowEditorModel FromDefinition(WorkflowDefinition definition)
    {
        var model = new WorkflowEditorModel { Id = definition.Id, Name = definition.Name };

        foreach(var node in definition.Nodes)
        {
            model.Nodes.Add(WorkflowNodeEditorModel.FromDefinition(node));
        }

        foreach(var transition in definition.Transitions)
        {
            model.Transitions.Add(WorkflowTransitionEditorModel.FromDefinition(transition));
        }

        return model;
    }
}

public partial class WorkflowTransitionEditorModel : ObservableObject
{
    [ObservableProperty] private string _fromNodeId = string.Empty;

    [ObservableProperty] private string? _toNodeId;

    [ObservableProperty] private WorkflowNodeOutcome _outcome = WorkflowNodeOutcome.Success;

    [ObservableProperty] private string? _expectedStatus;

    [ObservableProperty] private bool _isTerminal;

    public static WorkflowTransitionEditorModel FromDefinition(WorkflowTransitionDefinition definition)
    {
        return new WorkflowTransitionEditorModel
        {
            FromNodeId = definition.FromNodeId,
            ToNodeId = definition.ToNodeId,
            Outcome = definition.Outcome,
            ExpectedStatus = definition.ExpectedStatus,
            IsTerminal = definition.ToNodeId is null
        };
    }
}

public partial class WorkflowNodeEditorModel : ObservableObject
{
    [ObservableProperty] private string _id = Guid.NewGuid().ToString("N");

    [ObservableProperty] private string _name = "Delay";

    [ObservableProperty] private string _behaviorId = "delay";

    [ObservableProperty] private string _templatePath = string.Empty;

    [ObservableProperty] private double _threshold = 0.85;

    [ObservableProperty] private bool _useRegion;

    [ObservableProperty] private int _regionX;

    [ObservableProperty] private int _regionY;

    [ObservableProperty] private int _regionWidth = 400;

    [ObservableProperty] private int _regionHeight = 300;

    [ObservableProperty] private int _timeoutSeconds = 10;

    [ObservableProperty] private AutomationKey _key = AutomationKey.Enter;

    [ObservableProperty] private int _delayMilliseconds = 500;

    [ObservableProperty] private string _workflowId = string.Empty;

    [ObservableProperty] private string _screenId = "screen";

    [ObservableProperty] private string _excludedTemplatePath = string.Empty;

    [ObservableProperty] private int _clickOffsetX;

    [ObservableProperty] private int _clickOffsetY;

    [ObservableProperty] private MouseClickMode _clickMode = MouseClickMode.Single;

    [ObservableProperty] private int _clickRepeatCount = 1;

    [ObservableProperty] private int _clickIntervalMilliseconds = 100;

    [ObservableProperty] private int _clickHoldDurationMilliseconds = 60;

    [ObservableProperty] private bool _failWhenNotFound = true;

    public bool RequiresTemplate => BehaviorId is "click-template" or "wait-for-template" or "wait-for-template-disappear" or "check-screen" or "wait-for-screen";

    public bool IsClickTemplate => BehaviorId == "click-template";

    public bool IsWaitForTemplate => BehaviorId == "wait-for-template";

    public bool IsWaitForTemplateDisappear => BehaviorId == "wait-for-template-disappear";

    public bool IsPressKey => BehaviorId == "press-key";

    public bool IsDelay => BehaviorId == "delay";

    public bool IsRunWorkflow => BehaviorId == "run-workflow";

    public bool IsCheckScreen => BehaviorId == "check-screen";

    public bool IsWaitForScreen => BehaviorId == "wait-for-screen";

    public bool IsScreenBehavior => IsCheckScreen || IsWaitForScreen;

    public bool RequiresTimeout => IsWaitForTemplate || IsWaitForTemplateDisappear || IsWaitForScreen;

    public static WorkflowNodeEditorModel FromDefinition(WorkflowNodeDefinition definition)
    {
        return new WorkflowNodeEditorModel
        {
            Id = definition.Id,
            Name = definition.Name,
            BehaviorId = definition.BehaviorId,
            TemplatePath = GetTemplatePath(definition.Parameters),
            Threshold = GetThreshold(definition.Parameters),
            UseRegion = GetUseRegion(definition.Parameters),
            RegionX = GetRegionX(definition.Parameters),
            RegionY = GetRegionY(definition.Parameters),
            RegionWidth = GetRegionWidth(definition.Parameters),
            RegionHeight = GetRegionHeight(definition.Parameters),
            TimeoutSeconds = GetTimeoutSeconds(definition.Parameters),
            Key = GetKey(definition.Parameters),
            DelayMilliseconds = GetDelayMilliseconds(definition.Parameters),
            WorkflowId = GetWorkflowId(definition.Parameters),
            ScreenId = GetScreenId(definition.Parameters),
            ExcludedTemplatePath = GetExcludedTemplatePath(definition.Parameters),
            ClickOffsetX = GetClickOffsetX(definition.Parameters),
            ClickOffsetY = GetClickOffsetY(definition.Parameters),
            ClickMode = GetClickMode(definition.Parameters),
            ClickRepeatCount = GetClickRepeatCount(definition.Parameters),
            ClickIntervalMilliseconds = GetClickInterval(definition.Parameters),
            ClickHoldDurationMilliseconds = GetClickHoldDuration(definition.Parameters)
            ,FailWhenNotFound = GetFailWhenNotFound(definition.Parameters)
        };
    }

    public object ToParameters()
    {
        return BehaviorId switch
        {
            "click-template" => new ClickTemplateBehaviorParameters
            {
                TemplatePath = TemplatePath,
                Threshold = Threshold,
                UseRegion = UseRegion,
                RegionX = RegionX,
                RegionY = RegionY,
                RegionWidth = RegionWidth,
                RegionHeight = RegionHeight,
                ClickOffsetX = ClickOffsetX,
                ClickOffsetY = ClickOffsetY,
                ClickMode = ClickMode,
                ClickRepeatCount = ClickRepeatCount,
                ClickIntervalMilliseconds = ClickIntervalMilliseconds,
                ClickHoldDurationMilliseconds = ClickHoldDurationMilliseconds,
                FailWhenNotFound = FailWhenNotFound
            },
            "wait-for-template" => new WaitForTemplateBehaviorParameters
            {
                TemplatePath = TemplatePath,
                Threshold = Threshold,
                UseRegion = UseRegion,
                RegionX = RegionX,
                RegionY = RegionY,
                RegionWidth = RegionWidth,
                RegionHeight = RegionHeight,
                TimeoutSeconds = TimeoutSeconds
            },
            "wait-for-template-disappear" => new WaitForTemplateDisappearBehaviorParameters
            {
                TemplatePath = TemplatePath,
                Threshold = Threshold,
                UseRegion = UseRegion,
                RegionX = RegionX,
                RegionY = RegionY,
                RegionWidth = RegionWidth,
                RegionHeight = RegionHeight,
                TimeoutSeconds = TimeoutSeconds
            },
            "press-key" => new PressKeyBehaviorParameters { Key = Key },
            "delay" => new DelayBehaviorParameters { DurationMilliseconds = DelayMilliseconds },
            "run-workflow" => new RunWorkflowBehaviorParameters { WorkflowId = WorkflowId },
            "check-screen" => new CheckScreenBehaviorParameters
            {
                Screen = CreateScreenDefinition()
            },
            "wait-for-screen" => new WaitForScreenBehaviorParameters
            {
                Screen = CreateScreenDefinition(),
                TimeoutSeconds = TimeoutSeconds
            },
            _ => throw new InvalidOperationException($"Unsupported workflow behavior: {BehaviorId}")
        };
    }

    partial void OnBehaviorIdChanged(string value)
    {
        OnPropertyChanged(nameof(RequiresTemplate));
        OnPropertyChanged(nameof(IsClickTemplate));
        OnPropertyChanged(nameof(IsWaitForTemplate));
        OnPropertyChanged(nameof(IsWaitForTemplateDisappear));
        OnPropertyChanged(nameof(IsPressKey));
        OnPropertyChanged(nameof(IsDelay));
        OnPropertyChanged(nameof(IsRunWorkflow));
        OnPropertyChanged(nameof(IsCheckScreen));
        OnPropertyChanged(nameof(IsWaitForScreen));
        OnPropertyChanged(nameof(IsScreenBehavior));
        OnPropertyChanged(nameof(RequiresTimeout));
    }

    private static string GetTemplatePath(object parameters) => parameters switch
    {
        ClickTemplateBehaviorParameters value => value.TemplatePath,
        WaitForTemplateBehaviorParameters value => value.TemplatePath,
        WaitForTemplateDisappearBehaviorParameters value => value.TemplatePath,
        CheckScreenBehaviorParameters { Screen.RequiredElements.Count: > 0 } value => value.Screen.RequiredElements[0].TemplatePath,
        WaitForScreenBehaviorParameters { Screen.RequiredElements.Count: > 0 } value => value.Screen.RequiredElements[0].TemplatePath,
        _ => string.Empty
    };

    private static double GetThreshold(object parameters) => parameters switch
    {
        ClickTemplateBehaviorParameters value => value.Threshold,
        WaitForTemplateBehaviorParameters value => value.Threshold,
        WaitForTemplateDisappearBehaviorParameters value => value.Threshold,
        CheckScreenBehaviorParameters { Screen.RequiredElements.Count: > 0 } value => value.Screen.RequiredElements[0].Threshold,
        WaitForScreenBehaviorParameters { Screen.RequiredElements.Count: > 0 } value => value.Screen.RequiredElements[0].Threshold,
        _ => 0.85
    };

    private static bool GetUseRegion(object parameters) => parameters switch
    {
        ClickTemplateBehaviorParameters value => value.UseRegion,
        WaitForTemplateBehaviorParameters value => value.UseRegion,
        WaitForTemplateDisappearBehaviorParameters value => value.UseRegion,
        CheckScreenBehaviorParameters { Screen.RequiredElements.Count: > 0 } value => value.Screen.RequiredElements[0].UseRegion,
        WaitForScreenBehaviorParameters { Screen.RequiredElements.Count: > 0 } value => value.Screen.RequiredElements[0].UseRegion,
        _ => false
    };

    private static int GetRegionX(object parameters) => parameters switch
    {
        ClickTemplateBehaviorParameters value => value.RegionX,
        WaitForTemplateBehaviorParameters value => value.RegionX,
        WaitForTemplateDisappearBehaviorParameters value => value.RegionX,
        CheckScreenBehaviorParameters { Screen.RequiredElements.Count: > 0 } value => value.Screen.RequiredElements[0].RegionX,
        WaitForScreenBehaviorParameters { Screen.RequiredElements.Count: > 0 } value => value.Screen.RequiredElements[0].RegionX,
        _ => 0
    };

    private static int GetRegionY(object parameters) => parameters switch
    {
        ClickTemplateBehaviorParameters value => value.RegionY,
        WaitForTemplateBehaviorParameters value => value.RegionY,
        WaitForTemplateDisappearBehaviorParameters value => value.RegionY,
        CheckScreenBehaviorParameters { Screen.RequiredElements.Count: > 0 } value => value.Screen.RequiredElements[0].RegionY,
        WaitForScreenBehaviorParameters { Screen.RequiredElements.Count: > 0 } value => value.Screen.RequiredElements[0].RegionY,
        _ => 0
    };

    private static int GetRegionWidth(object parameters) => parameters switch
    {
        ClickTemplateBehaviorParameters value => value.RegionWidth,
        WaitForTemplateBehaviorParameters value => value.RegionWidth,
        WaitForTemplateDisappearBehaviorParameters value => value.RegionWidth,
        CheckScreenBehaviorParameters { Screen.RequiredElements.Count: > 0 } value => value.Screen.RequiredElements[0].RegionWidth,
        WaitForScreenBehaviorParameters { Screen.RequiredElements.Count: > 0 } value => value.Screen.RequiredElements[0].RegionWidth,
        _ => 400
    };

    private static int GetRegionHeight(object parameters) => parameters switch
    {
        ClickTemplateBehaviorParameters value => value.RegionHeight,
        WaitForTemplateBehaviorParameters value => value.RegionHeight,
        WaitForTemplateDisappearBehaviorParameters value => value.RegionHeight,
        CheckScreenBehaviorParameters { Screen.RequiredElements.Count: > 0 } value => value.Screen.RequiredElements[0].RegionHeight,
        WaitForScreenBehaviorParameters { Screen.RequiredElements.Count: > 0 } value => value.Screen.RequiredElements[0].RegionHeight,
        _ => 300
    };

    private static int GetTimeoutSeconds(object parameters) => parameters switch
    {
        WaitForTemplateBehaviorParameters value => value.TimeoutSeconds,
        WaitForTemplateDisappearBehaviorParameters value => value.TimeoutSeconds,
        WaitForScreenBehaviorParameters value => value.TimeoutSeconds,
        _ => 10
    };

    private static AutomationKey GetKey(object parameters) => parameters is PressKeyBehaviorParameters value ? value.Key : AutomationKey.Enter;

    private static int GetDelayMilliseconds(object parameters) => parameters is DelayBehaviorParameters value ? value.DurationMilliseconds : 500;

    private static string GetWorkflowId(object parameters) => parameters is RunWorkflowBehaviorParameters value ? value.WorkflowId : string.Empty;

    private static string GetScreenId(object parameters) => parameters switch
    {
        CheckScreenBehaviorParameters value => value.Screen.Id,
        WaitForScreenBehaviorParameters value => value.Screen.Id,
        _ => "screen"
    };

    private static string GetExcludedTemplatePath(object parameters) => parameters switch
    {
        CheckScreenBehaviorParameters { Screen.ExcludedElements.Count: > 0 } value => value.Screen.ExcludedElements[0].TemplatePath,
        WaitForScreenBehaviorParameters { Screen.ExcludedElements.Count: > 0 } value => value.Screen.ExcludedElements[0].TemplatePath,
        _ => string.Empty
    };

    private static int GetClickOffsetX(object parameters) => parameters is ClickTemplateBehaviorParameters value ? value.ClickOffsetX : 0;

    private static int GetClickOffsetY(object parameters) => parameters is ClickTemplateBehaviorParameters value ? value.ClickOffsetY : 0;

    private static MouseClickMode GetClickMode(object parameters) => parameters is ClickTemplateBehaviorParameters value ? value.ClickMode : MouseClickMode.Single;

    private static int GetClickRepeatCount(object parameters) => parameters is ClickTemplateBehaviorParameters value ? value.ClickRepeatCount : 1;

    private static int GetClickInterval(object parameters) => parameters is ClickTemplateBehaviorParameters value ? value.ClickIntervalMilliseconds : 100;

    private static int GetClickHoldDuration(object parameters) => parameters is ClickTemplateBehaviorParameters value ? value.ClickHoldDurationMilliseconds : 60;

    private static bool GetFailWhenNotFound(object parameters) => parameters is ClickTemplateBehaviorParameters value ? value.FailWhenNotFound : true;

    private ScreenDefinition CreateScreenDefinition()
    {
        IReadOnlyList<ScreenElementDefinition> excludedElements = string.IsNullOrWhiteSpace(ExcludedTemplatePath)
            ? []
            : new[]
            {
                new ScreenElementDefinition
                {
                    TemplatePath = ExcludedTemplatePath,
                    Threshold = Threshold,
                    UseRegion = UseRegion,
                    RegionX = RegionX,
                    RegionY = RegionY,
                    RegionWidth = RegionWidth,
                    RegionHeight = RegionHeight
                }
            };

        return new ScreenDefinition
        {
            Id = ScreenId,
            Name = ScreenId,
            RequiredElements =
            [
                new ScreenElementDefinition
                {
                    TemplatePath = TemplatePath,
                    Threshold = Threshold,
                    UseRegion = UseRegion,
                    RegionX = RegionX,
                    RegionY = RegionY,
                    RegionWidth = RegionWidth,
                    RegionHeight = RegionHeight
                }
            ],
            ExcludedElements = excludedElements
        };
    }
}
