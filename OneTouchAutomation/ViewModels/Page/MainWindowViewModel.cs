using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OneTouchAutomation.Constants;
using OneTouchAutomation.Models;

namespace OneTouchAutomation.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly List<SideBarItemModel> _templates = new()
    {
        new SideBarItemModel
        {
            ModelType = typeof(HomePageViewModel),
            IconKey   = Icon.Home,
            Title     = "Home"
        },

        new SideBarItemModel()
        {
            ModelType = typeof(GamePageViewModel),
            IconKey   = Icon.Game,
            Title     = "Game"
        },

        new SideBarItemModel()
        {
            ModelType = typeof(WorkflowPageViewModel),
            IconKey   = Icon.Workflow,
            Title     = "Workflows"
        },

        new SideBarItemModel()
        {
            ModelType = typeof(DebugPageViewModel),
            IconKey   = Icon.Debug,
            Title     = "Debug"
        },

        new SideBarItemModel
        {
            ModelType = typeof(SettingsPageViewModel),
            IconKey   = Icon.Settings,
            Title     = "Settings"
        },

        new SideBarItemModel
        {
            ModelType = typeof(InfoPageViewModel),
            IconKey   = Icon.Info,
            Title     = "Info"
        },
    };

    [ObservableProperty] private ViewModelBase _currentPage = new HomePageViewModel();

    [ObservableProperty] private bool _isPaneOpen;

    [ObservableProperty] private SideBarItemModel? _selectedListItem;

    public MainWindowViewModel(IMessenger messenger, AppAppearanceSettings appearance)
    {
        Appearance = appearance;
        Items      = new ObservableCollection<SideBarItemModel>(_templates);

        SelectedListItem = Items.First(vm => vm.ModelType == typeof(HomePageViewModel));
    }

    public MainWindowViewModel(AppAppearanceSettings appearance) : this(new WeakReferenceMessenger(), appearance) { }

    public MainWindowViewModel() : this(new WeakReferenceMessenger(), new AppAppearanceSettings()) { }

    public ObservableCollection<SideBarItemModel> Items { get; }

    public AppAppearanceSettings Appearance { get; }

    partial void OnSelectedListItemChanged(SideBarItemModel? value)
    {
        if (value is null) return;

        var vm = Design.IsDesignMode
            ? Activator.CreateInstance(value.ModelType)
            : Ioc.Default.GetService(value.ModelType);

        if (vm is not ViewModelBase vmb) return;

        CurrentPage = vmb;
    }

    [RelayCommand]
    private void TriggerPane() { IsPaneOpen = !IsPaneOpen; }
}
