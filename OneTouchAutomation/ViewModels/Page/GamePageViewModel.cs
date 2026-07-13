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

    public GamePageViewModel(
        IScreenCaptureService screenCaptureService,
        ITaskRunner taskRunner,
        ITaskConfigurationStore taskConfigurationStore,
        ITaskRunHistoryStore taskRunHistoryStore,
        IBehaviorRegistry behaviorRegistry)
    {
        _screenCaptureService = screenCaptureService;
        _taskRunner = taskRunner;
        _taskConfigurationStore = taskConfigurationStore;
        _taskRunHistoryStore = taskRunHistoryStore;
        _behaviorRegistry = behaviorRegistry;

        foreach(var behavior in _behaviorRegistry.Behaviors.Where(IsSupportedTaskBehavior))
        {
            AvailableBehaviors.Add(behavior);
        }

        if(!Design.IsDesignMode)
        {
            _ = LoadTasksAsync();
            _ = LoadRunHistoryAsync();
        }
    }

    public GamePageViewModel() : this(
        new ScreenCaptureService(),
        null!,
        new JsonTaskConfigurationStore(),
        new JsonTaskRunHistoryStore(),
        new BehaviorRegistry([])) { }

    public ObservableCollection<AutomationTaskModel> Tasks { get; } = new();

    public ObservableCollection<CaptureWindowInfo> Windows { get; } = new();

    public ObservableCollection<IAutomationBehavior> AvailableBehaviors { get; } = new();

    public IReadOnlyList<TaskFailurePolicy> FailurePolicies { get; } = Enum.GetValues<TaskFailurePolicy>();

    public IReadOnlyList<AutomationKey> AvailableKeys { get; } = Enum.GetValues<AutomationKey>();

    public ObservableCollection<string> Logs { get; } = new();

    public ObservableCollection<TaskRunHistoryEntry> RunHistory { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveTaskCommand))]
    [NotifyCanExecuteChangedFor(nameof(RunAllCommand))]
    private AutomationTaskModel? _selectedTask;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunAllCommand))]
    private CaptureWindowInfo? _selectedWindow;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelRunCommand))]
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
                RegionHeight = task.RegionHeight
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
            IsEnabled = configuration.IsEnabled
        };
    }

    private void AddLog(string message)
    {
        Dispatcher.UIThread.Post(() => Logs.Insert(0, message));
    }

    private static bool IsSupportedTaskBehavior(IAutomationBehavior behavior)
    {
        return behavior.Id is "click-template" or "wait-for-template" or "press-key" or "delay";
    }
}
