using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneTouchAutomation.Models;

namespace OneTouchAutomation.ViewModels;

public partial class GamePageViewModel : ViewModelBase
{
    public GamePageViewModel()
    {
        Behaviors = new ObservableCollection<AutomationBehaviorModel>
        {
            new()
            {
                Name = "Find start button",
                Target = "Main game window",
                Trigger = "Template match",
                Action = "Click center",
                Confidence = 0.9,
            },
            new()
            {
                Name = "Confirm reward",
                Target = "Reward dialog",
                Trigger = "Image match",
                Action = "Press Enter",
                Confidence = 0.82,
            },
        };

        SelectedBehavior = Behaviors.FirstOrDefault();
    }

    public ObservableCollection<AutomationBehaviorModel> Behaviors { get; }

    public string[] TriggerOptions { get; } =
    {
        "Image match",
        "Template match",
        "Color region",
        "Manual trigger",
    };

    public string[] ActionOptions { get; } =
    {
        "Click",
        "Click center",
        "Press Enter",
        "Send hotkey",
        "Wait",
    };

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveBehaviorCommand))]
    private AutomationBehaviorModel? _selectedBehavior;

    [RelayCommand]
    private void AddBehavior()
    {
        var behavior = new AutomationBehaviorModel
        {
            Name = $"Behavior {Behaviors.Count + 1}",
        };

        Behaviors.Add(behavior);
        SelectedBehavior = behavior;
    }

    [RelayCommand(CanExecute = nameof(CanRemoveBehavior))]
    private void RemoveBehavior()
    {
        if (SelectedBehavior is null) return;

        var index = Behaviors.IndexOf(SelectedBehavior);
        Behaviors.Remove(SelectedBehavior);

        SelectedBehavior = Behaviors.Count == 0
            ? null
            : Behaviors.ElementAtOrDefault(index) ?? Behaviors.Last();
    }

    private bool CanRemoveBehavior()
    {
        return SelectedBehavior is not null;
    }
}
