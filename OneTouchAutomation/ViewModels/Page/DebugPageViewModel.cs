namespace OneTouchAutomation.ViewModels;

public class DebugPageViewModel : ViewModelBase
{
    public DebugPageViewModel
    (
        VisionDebugViewModel visionDebug,
        BehaviorDebugViewModel behaviorDebug)
    {
        VisionDebug   = visionDebug;
        BehaviorDebug = behaviorDebug;
    }

    public VisionDebugViewModel   VisionDebug   { get; }
    public BehaviorDebugViewModel BehaviorDebug { get; }
}