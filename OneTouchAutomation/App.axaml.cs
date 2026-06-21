using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using OneTouchAutomation.ViewModels;
using OneTouchAutomation.Views;
using OneTouchAutomation.Views.Page;

namespace OneTouchAutomation;

public class App : Application
{
    public override void Initialize() { AvaloniaXamlLoader.Load(this); }

    public override void OnFrameworkInitializationCompleted()
    {
        var locator = new ViewLocator();
        DataTemplates.Add(locator);

        var services = new ServiceCollection();
        ConfigureViewModels(services);
        ConfigureViews(services);

        var provider = services.BuildServiceProvider();

        Ioc.Default.ConfigureServices(provider);

        var vm = Ioc.Default.GetRequiredService<MainWindowViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(vm);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewApplicationLifetime)
        {
            singleViewApplicationLifetime.MainView = new MainView() { DataContext = vm };
        }

        base.OnFrameworkInitializationCompleted();
    }


    internal static void ConfigureViewModels(IServiceCollection services)
    {
        services.AddSingleton<AppAppearanceSettings>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<HomePageViewModel>();
        services.AddTransient<GamePageViewModel>();
        services.AddTransient<SettingsPageViewModel>();
        services.AddTransient<InfoPageViewModel>();
        services.AddTransient<DebugPageViewModel>();
    }

    internal static void ConfigureViews(IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddTransient<HomePage>();
        services.AddTransient<GamePage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<InfoPage>();
        services.AddTransient<DebugPage>();
    }
}