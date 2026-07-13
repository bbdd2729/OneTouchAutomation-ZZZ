using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneTouchAutomation.Models;
using OneTouchAutomation.Services.Automation.Behavior;
using OneTouchAutomation.Services.Automation.History;
using OneTouchAutomation.Services.Automation.Persistence;
using OneTouchAutomation.Services.Automation.Tasks;
using OneTouchAutomation.Services.Automation.Workflows;
using OneTouchAutomation.Services.Capture;
using OneTouchAutomation.Services.Input;

namespace OneTouchAutomation.ViewModels;

public partial class GamePageViewModel : ViewModelBase
{
    private CancellationTokenSource? _runCancellationSource;
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly ITaskConfigurationStore _taskConfigurationStore;
    private readonly ITaskRunHistoryStore _taskRunHistoryStore;
    private readonly ITaskRunner _taskRunner;
    private readonly IBehaviorRegistry _behaviorRegistry;
    private readonly IWorkflowConfigurationStore _workflowConfigurationStore;

    public GamePageViewModel(
        IScreenCaptureService screenCaptureService,
        ITaskRunner taskRunner,
        ITaskConfigurationStore taskConfigurationStore,
        ITaskRunHistoryStore taskRunHistoryStore,
        IBehaviorRegistry behaviorRegistry,
        IWorkflowConfigurationStore workflowConfigurationStore)
    {
        _screenCaptureService = screenCaptureService;
        _taskRunner = taskRunner;
        _taskConfigurationStore = taskConfigurationStore;
        _taskRunHistoryStore = taskRunHistoryStore;
        _behaviorRegistry = behaviorRegistry;
        _workflowConfigurationStore = workflowConfigurationStore;

        foreach(var behavior in _behaviorRegistry.Behaviors.Where(IsSupportedTaskBehavior))
        {
            AvailableBehaviors.Add(behavior);
        }

        if(!Design.IsDesignMode)
        {
            _ = LoadTasksAsync();
            _ = LoadRunHistoryAsync();
            _ = LoadWorkflowsAsync();
        }
    }

    public GamePageViewModel() : this(
        new ScreenCaptureService(),
        null!,
        new JsonTaskConfigurationStore(),
        new JsonTaskRunHistoryStore(),
        new BehaviorRegistry([]),
        new JsonWorkflowConfigurationStore(
            new BehaviorRegistry([]),
            new WorkflowValidator(new BehaviorRegistry([])))) { }

    public ObservableCollection<AutomationTaskModel> Tasks { get; } = new();

    public ObservableCollection<CaptureWindowInfo> Windows { get; } = new();

    public ObservableCollection<IAutomationBehavior> AvailableBehaviors { get; } = new();

    public ObservableCollection<WorkflowDefinition> AvailableWorkflows { get; } = new();

    public IReadOnlyList<TaskFailurePolicy> FailurePolicies { get; } = Enum.GetValues<TaskFailurePolicy>();

    public IReadOnlyList<AutomationKey> AvailableKeys { get; } = Enum.GetValues<AutomationKey>();

    public IReadOnlyList<MouseClickMode> AvailableClickModes { get; } = Enum.GetValues<MouseClickMode>();

    public ObservableCollection<string> Logs { get; } = new();

    public ObservableCollection<TaskRunHistoryEntry> RunHistory { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveTaskCommand))]
    [NotifyCanExecuteChangedFor(nameof(DuplicateTaskCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveTaskUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveTaskDownCommand))]
    [NotifyCanExecuteChangedFor(nameof(RunAllCommand))]
    private AutomationTaskModel? _selectedTask;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunAllCommand))]
    private CaptureWindowInfo? _selectedWindow;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelRunCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveTaskCommand))]
    [NotifyCanExecuteChangedFor(nameof(DuplicateTaskCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveTaskUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveTaskDownCommand))]
    private bool _isRunning;

    [ObservableProperty] private string _runStatus = "Select a window and add a task.";

    [RelayCommand]
    private async Task RefreshWindowsAsync()
    {
        Windows.Clear();

        var windows = await _screenCaptureService.ListWindowsAsync();

        foreach(var window in windows)
        {
            Windows.Add(window);
        }

        SelectedWindow = Windows.FirstOrDefault();
        AddLog($"Loaded {Windows.Count} window(s).");
    }

    [RelayCommand]
    private async Task LoadTasksAsync()
    {
        if(IsRunning)
        {
            return;
        }

        try
        {
            var configurations = await _taskConfigurationStore.LoadAsync();

            Tasks.Clear();

            foreach(var configuration in configurations)
            {
                Tasks.Add(ToTaskModel(configuration));
            }

            SelectedTask = Tasks.FirstOrDefault();
            RunStatus = $"Loaded {Tasks.Count} saved task(s).";
            AddLog(RunStatus);
            RunAllCommand.NotifyCanExecuteChanged();
        }
        catch(Exception exception)
        {
            RunStatus = $"Failed to load tasks: {exception.Message}";
            AddLog(RunStatus);
        }
    }

    [RelayCommand]
    private async Task SaveTasksAsync()
    {
        try
        {
            var configurations = Tasks
                .Select(ToTaskConfiguration)
                .ToArray();

            await _taskConfigurationStore.SaveAsync(configurations);

            RunStatus = $"Saved {configurations.Length} task(s).";
            AddLog(RunStatus);
        }
        catch(Exception exception)
        {
            RunStatus = $"Failed to save tasks: {exception.Message}";
            AddLog(RunStatus);
        }
    }

    [RelayCommand]
    private async Task LoadRunHistoryAsync()
    {
        try
        {
            var entries = await _taskRunHistoryStore.LoadRecentAsync();

            RunHistory.Clear();

            foreach(var entry in entries)
            {
                RunHistory.Add(entry);
            }
        }
        catch(Exception exception)
        {
            AddLog($"Failed to load run history: {exception.Message}");
        }
    }

    [RelayCommand]
    private async Task LoadWorkflowsAsync()
    {
        try
        {
            var workflows = await _workflowConfigurationStore.LoadAsync();

            AvailableWorkflows.Clear();

            foreach(var workflow in workflows)
            {
                AvailableWorkflows.Add(workflow);
            }

            AddLog($"Loaded {AvailableWorkflows.Count} workflow(s).");
        }
        catch(Exception exception)
        {
            AddLog($"Failed to load workflows: {exception.Message}");
        }
    }

    [RelayCommand]
    private void AddTask()
    {
        var task = new AutomationTaskModel
        {
            Name = $"Click template {Tasks.Count + 1}",
            BehaviorId = AvailableBehaviors.FirstOrDefault()?.Id ?? "click-template"
        };

        Tasks.Add(task);
        SelectedTask = task;
        RunAllCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRemoveTask))]
    private void RemoveTask()
    {
        if(SelectedTask is null)
        {
            return;
        }

        var index = Tasks.IndexOf(SelectedTask);
        Tasks.Remove(SelectedTask);

        SelectedTask = Tasks.Count == 0
            ? null
            : Tasks.ElementAtOrDefault(index) ?? Tasks.Last();

        RunAllCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanDuplicateTask))]
    private void DuplicateTask()
    {
        if(SelectedTask is null)
        {
            return;
        }

        var index = Tasks.IndexOf(SelectedTask);
        var duplicate = CloneTask(SelectedTask);

        Tasks.Insert(index + 1, duplicate);
        SelectedTask = duplicate;
        RunAllCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanMoveTaskUp))]
    private void MoveTaskUp()
    {
        if(SelectedTask is null)
        {
            return;
        }

        var index = Tasks.IndexOf(SelectedTask);

        if(index <= 0)
        {
            return;
        }

        Tasks.Move(index, index - 1);
        RefreshTaskOrderCommands();
    }

    [RelayCommand(CanExecute = nameof(CanMoveTaskDown))]
    private void MoveTaskDown()
    {
        if(SelectedTask is null)
        {
            return;
        }

        var index = Tasks.IndexOf(SelectedTask);

        if(index < 0 || index >= Tasks.Count - 1)
        {
            return;
        }

        Tasks.Move(index, index + 1);
        RefreshTaskOrderCommands();
    }

    [RelayCommand(CanExecute = nameof(CanRunAll))]
    private async Task RunAllAsync()
    {
        if(SelectedWindow is null)
        {
            return;
        }

        var tasks = Tasks
            .Where(task => task.IsEnabled)
            .Select(CreateTaskDefinition)
            .ToArray();

        if(tasks.Length == 0)
        {
            RunStatus = "No enabled tasks to run.";
            return;
        }

        _runCancellationSource = new CancellationTokenSource();
        IsRunning = true;
        RunStatus = "Running tasks.";
        AddLog($"Starting {tasks.Length} task(s). Target: {SelectedWindow.Title}.");
        var startedAt = DateTimeOffset.UtcNow;

        try
        {
            var result = await _taskRunner.RunAsync(
                SelectedWindow.Handle,
                tasks,
                AddLog,
                _runCancellationSource.Token);

            RunStatus = result.IsCancelled
                ? "Task run cancelled."
                : result.IsSuccess
                    ? $"Completed {result.TaskResults.Count} task(s)."
                    : result.TaskResults.Count == tasks.Length
                        ? $"Completed with {result.TaskResults.Count(task => !task.BehaviorResult.IsSuccess)} failed task(s)."
                        : $"Stopped after {result.TaskResults.Count} task(s).";

            AddLog(RunStatus);
            await SaveRunHistoryAsync(result, SelectedWindow.Title, startedAt, RunStatus);
        }
        catch(Exception exception)
        {
            RunStatus = $"Task run failed: {exception.Message}";
            AddLog(RunStatus);
        }
        finally
        {
            _runCancellationSource.Dispose();
            _runCancellationSource = null;
            IsRunning = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelRun))]
    private void CancelRun()
    {
        _runCancellationSource?.Cancel();
        AddLog("Cancellation requested.");
    }

    private bool CanRemoveTask()
    {
        return SelectedTask is not null && !IsRunning;
    }

    private bool CanDuplicateTask()
    {
        return SelectedTask is not null && !IsRunning;
    }

    private bool CanMoveTaskUp()
    {
        return SelectedTask is not null && !IsRunning && Tasks.IndexOf(SelectedTask) > 0;
    }

    private bool CanMoveTaskDown()
    {
        return SelectedTask is not null
            && !IsRunning
            && Tasks.IndexOf(SelectedTask) is var index
            && index >= 0
            && index < Tasks.Count - 1;
    }

    private bool CanRunAll()
    {
        return !IsRunning && SelectedWindow is not null && Tasks.Any(task => task.IsEnabled);
    }

    private bool CanCancelRun()
    {
        return IsRunning;
    }

    private static AutomationTaskDefinition CreateTaskDefinition(AutomationTaskModel task)
    {
        object parameters = task.BehaviorId switch
        {
            "click-template" => new ClickTemplateBehaviorParameters
            {
                TemplatePath = task.TemplatePath,
                Threshold = task.Threshold,
                UseRegion = task.UseRegion,
                RegionX = task.RegionX,
                RegionY = task.RegionY,
                RegionWidth = task.RegionWidth,
                RegionHeight = task.RegionHeight,
                ClickOffsetX = task.ClickOffsetX,
                ClickOffsetY = task.ClickOffsetY,
                ClickMode = task.ClickMode,
                ClickRepeatCount = task.ClickRepeatCount,
                ClickIntervalMilliseconds = task.ClickIntervalMilliseconds,
                ClickHoldDurationMilliseconds = task.ClickHoldDurationMilliseconds
            },
            "wait-for-template" => new WaitForTemplateBehaviorParameters
            {
                TemplatePath = task.TemplatePath,
                Threshold = task.Threshold,
                UseRegion = task.UseRegion,
                RegionX = task.RegionX,
                RegionY = task.RegionY,
                RegionWidth = task.RegionWidth,
                RegionHeight = task.RegionHeight,
                TimeoutSeconds = task.TimeoutSeconds
            },
            "press-key" => new PressKeyBehaviorParameters
            {
                Key = task.Key
            },
            "delay" => new DelayBehaviorParameters
            {
                DurationMilliseconds = task.DelayMilliseconds
            },
            "run-workflow" => new RunWorkflowBehaviorParameters
            {
                WorkflowId = task.WorkflowId
            },
            _ => throw new InvalidOperationException($"Unsupported task behavior: {task.BehaviorId}")
        };

        return new AutomationTaskDefinition
        {
            Id = task.Id,
            Name = task.Name,
            BehaviorId = task.BehaviorId,
            Parameters = parameters,
            IsEnabled = task.IsEnabled,
            FailurePolicy = task.FailurePolicy,
            MaxRetryCount = task.MaxRetryCount
        };
    }

    private async Task SaveRunHistoryAsync(
        TaskRunResult result,
        string windowTitle,
        DateTimeOffset startedAt,
        string summary)
    {
        var entry = new TaskRunHistoryEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            StartedAt = startedAt,
            CompletedAt = DateTimeOffset.UtcNow,
            TargetWindowTitle = windowTitle,
            Summary = summary,
            IsSuccess = result.IsSuccess,
            IsCancelled = result.IsCancelled,
            Tasks = result.TaskResults.Select(task => new TaskRunTaskHistoryEntry
            {
                TaskId = task.TaskId,
                TaskName = task.TaskName,
                BehaviorId = task.BehaviorId,
                Message = task.BehaviorResult.Message,
                IsSuccess = task.BehaviorResult.IsSuccess,
                AttemptCount = task.AttemptCount,
                MatchScore = task.BehaviorResult.MatchScore,
                ScreenX = task.BehaviorResult.ScreenX,
                ScreenY = task.BehaviorResult.ScreenY,
                Duration = task.Duration
            }).ToArray()
        };

        try
        {
            await _taskRunHistoryStore.SaveAsync(entry);
            RunHistory.Insert(0, entry);
        }
        catch(Exception exception)
        {
            AddLog($"Failed to save run history: {exception.Message}");
        }
    }

    private static AutomationTaskConfiguration ToTaskConfiguration(AutomationTaskModel task)
    {
        return new AutomationTaskConfiguration
        {
            Id = task.Id,
            Name = task.Name,
            BehaviorId = task.BehaviorId,
            TemplatePath = task.TemplatePath,
            Threshold = task.Threshold,
            UseRegion = task.UseRegion,
            RegionX = task.RegionX,
            RegionY = task.RegionY,
            RegionWidth = task.RegionWidth,
            RegionHeight = task.RegionHeight,
            TimeoutSeconds = task.TimeoutSeconds,
            FailurePolicy = task.FailurePolicy,
            MaxRetryCount = task.MaxRetryCount,
            Key = task.Key,
            DelayMilliseconds = task.DelayMilliseconds,
            ClickOffsetX = task.ClickOffsetX,
            ClickOffsetY = task.ClickOffsetY,
            ClickMode = task.ClickMode,
            ClickRepeatCount = task.ClickRepeatCount,
            ClickIntervalMilliseconds = task.ClickIntervalMilliseconds,
            ClickHoldDurationMilliseconds = task.ClickHoldDurationMilliseconds,
            WorkflowId = task.WorkflowId,
            IsEnabled = task.IsEnabled
        };
    }

    private static AutomationTaskModel ToTaskModel(AutomationTaskConfiguration configuration)
    {
        return new AutomationTaskModel
        {
            Id = configuration.Id,
            Name = configuration.Name,
            BehaviorId = configuration.BehaviorId,
            TemplatePath = configuration.TemplatePath,
            Threshold = configuration.Threshold,
            UseRegion = configuration.UseRegion,
            RegionX = configuration.RegionX,
            RegionY = configuration.RegionY,
            RegionWidth = configuration.RegionWidth,
            RegionHeight = configuration.RegionHeight,
            TimeoutSeconds = configuration.TimeoutSeconds,
            FailurePolicy = configuration.FailurePolicy,
            MaxRetryCount = configuration.MaxRetryCount,
            Key = configuration.Key,
            DelayMilliseconds = configuration.DelayMilliseconds,
            ClickOffsetX = configuration.ClickOffsetX,
            ClickOffsetY = configuration.ClickOffsetY,
            ClickMode = configuration.ClickMode,
            ClickRepeatCount = configuration.ClickRepeatCount,
            ClickIntervalMilliseconds = configuration.ClickIntervalMilliseconds,
            ClickHoldDurationMilliseconds = configuration.ClickHoldDurationMilliseconds,
            WorkflowId = configuration.WorkflowId,
            IsEnabled = configuration.IsEnabled
        };
    }

    private static AutomationTaskModel CloneTask(AutomationTaskModel source)
    {
        return new AutomationTaskModel
        {
            Name = $"{source.Name} Copy",
            BehaviorId = source.BehaviorId,
            TemplatePath = source.TemplatePath,
            Threshold = source.Threshold,
            UseRegion = source.UseRegion,
            RegionX = source.RegionX,
            RegionY = source.RegionY,
            RegionWidth = source.RegionWidth,
            RegionHeight = source.RegionHeight,
            TimeoutSeconds = source.TimeoutSeconds,
            FailurePolicy = source.FailurePolicy,
            MaxRetryCount = source.MaxRetryCount,
            Key = source.Key,
            DelayMilliseconds = source.DelayMilliseconds,
            ClickOffsetX = source.ClickOffsetX,
            ClickOffsetY = source.ClickOffsetY,
            ClickMode = source.ClickMode,
            ClickRepeatCount = source.ClickRepeatCount,
            ClickIntervalMilliseconds = source.ClickIntervalMilliseconds,
            ClickHoldDurationMilliseconds = source.ClickHoldDurationMilliseconds,
            WorkflowId = source.WorkflowId,
            IsEnabled = source.IsEnabled
        };
    }

    private void RefreshTaskOrderCommands()
    {
        MoveTaskUpCommand.NotifyCanExecuteChanged();
        MoveTaskDownCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedTaskChanged(AutomationTaskModel? value)
    {
        RefreshTaskOrderCommands();
    }

    private void AddLog(string message)
    {
        Dispatcher.UIThread.Post(() => Logs.Insert(0, message));
    }

    private static bool IsSupportedTaskBehavior(IAutomationBehavior behavior)
    {
        return behavior.Id is "click-template" or "wait-for-template" or "press-key" or "delay" or "run-workflow";
    }
}
