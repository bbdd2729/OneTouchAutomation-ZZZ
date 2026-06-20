using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OneTouchAutomation.Models;
using ReactiveUI.SourceGenerators;

namespace OneTouchAutomation.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(IMessenger messenger, AppAppearanceSettings appearance)
    {
        Appearance = appearance;
        Items = new ObservableCollection<SideBarItemModel>(_templates);

        SelectedListItem = Items.First(vm => vm.ModelType == typeof(HomePageViewModel));
    }

    public MainWindowViewModel(AppAppearanceSettings appearance) : this(new WeakReferenceMessenger(), appearance) { }
    
    private readonly List<SideBarItemModel> _templates = new()
    {
        new SideBarItemModel
        {
            ModelType = typeof(HomePageViewModel), 
            IconKey = OneTouchAutomation.Constants.Icon.Home, 
            Title = "Home"
        },
        
        new SideBarItemModel()
        {
            ModelType = typeof(GamePageViewModel), 
            IconKey   = OneTouchAutomation.Constants.Icon.Game, 
            Title     = "Game"
        },
        
        new SideBarItemModel
        {
            ModelType = typeof(SettingsPageViewModel), 
            IconKey = OneTouchAutomation.Constants.Icon.Settings, 
            Title = "Settings"
        },
        
        new SideBarItemModel
        {
            ModelType = typeof(InfoPageViewModel), 
            IconKey = OneTouchAutomation.Constants.Icon.Info, 
            Title = "Info"
        },
        
        
        
    };

    public MainWindowViewModel() : this(new WeakReferenceMessenger(), new AppAppearanceSettings()) { }
    
    [ObservableProperty]
    private bool _isPaneOpen;

    [ObservableProperty]
    private ViewModelBase _currentPage = new HomePageViewModel();

    [ObservableProperty]
    private SideBarItemModel? _selectedListItem;
    
    partial void OnSelectedListItemChanged(SideBarItemModel? value)
    {
        if (value is null) return;

        var vm = Design.IsDesignMode
            ? Activator.CreateInstance(value.ModelType)
            : Ioc.Default.GetService(value.ModelType);

        if (vm is not ViewModelBase vmb) return;

        CurrentPage = vmb;
    }
    
    public ObservableCollection<SideBarItemModel> Items { get; }

    public AppAppearanceSettings Appearance { get; }

    [RelayCommand]
    private void TriggerPane()
    {
        IsPaneOpen = !IsPaneOpen;
    }

    
}
