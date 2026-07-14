using CommunityToolkit.Mvvm.ComponentModel;

namespace OneTouchAutomation.ViewModels;

public partial class DebugPageViewModel : ViewModelBase
{
    [ObservableProperty] private int _selectedModeIndex;

    public DebugPageViewModel(
        VisionDebugViewModel visionDebug,
        BehaviorDebugViewModel behaviorDebug)
    {
        VisionDebug = visionDebug;
        BehaviorDebug = behaviorDebug;
    }

    public VisionDebugViewModel VisionDebug { get; }

    public BehaviorDebugViewModel BehaviorDebug { get; }

    public bool IsVisionMode => SelectedModeIndex == 0;

    public bool IsBehaviorMode => SelectedModeIndex == 1;

    partial void OnSelectedModeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsVisionMode));
        OnPropertyChanged(nameof(IsBehaviorMode));
    }
}
