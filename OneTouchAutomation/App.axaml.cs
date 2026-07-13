using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using OneTouchAutomation.Services.Automation.Behavior;
using OneTouchAutomation.Services.Automation.Tasks;
using OneTouchAutomation.ViewModels;
using OneTouchAutomation.Views;

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
        ConfigureServices(services);

        var provider = services.BuildServiceProvider();

        Ioc.Default.ConfigureServices(provider);

        var vm = Ioc.Default.GetRequiredService<MainWindowViewModel>();

        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(vm);
        }
        else if(ApplicationLifetime is ISingleViewApplicationLifetime singleViewApplicationLifetime)
        {
            singleViewApplicationLifetime.MainView = new MainView() { DataContext = vm };
        }

        base.OnFrameworkInitializationCompleted();
    }


    internal static void ConfigureViewModels(IServiceCollection services)
    {
        services.AddSingleton<AppAppearanceSettings>();
        services.AddSingleton<MainWindowViewModel>();

        services.Scan
                (scan => scan.FromAssemblyOf<App>().AddClasses
                         (classes => classes.AssignableTo<ViewModelBase>().Where
                                  (type =>
                                           type != typeof(AppAppearanceSettings) &&
                                           type != typeof(MainWindowViewModel))).AsSelf().WithTransientLifetime());
    }

    internal static void ConfigureViews(IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();

        services.Scan
                (scan => scan.FromAssemblyOf<App>().AddClasses
                         (classes => classes.AssignableTo<UserControl>()).AsSelf().WithTransientLifetime());
    }

    internal static void ConfigureServices(IServiceCollection services)
    {
        services.Scan
                (scan => scan.FromAssemblyOf<App>().AddClasses
                                      (classes => classes.InNamespaces
                                               (
                                                "OneTouchAutomation.Services.Capture",
                                                "OneTouchAutomation.Services.Vision",
                                                "OneTouchAutomation.Services.Debug",
                                                "OneTouchAutomation.Services.Input")).AsImplementedInterfaces().
                              WithSingletonLifetime());
        services.AddSingleton<ClickTemplateBehavior>();
        services.AddSingleton<IAutomationBehavior<ClickTemplateBehaviorParameters>>
            (provider => provider.GetRequiredService<ClickTemplateBehavior>());
        services.AddSingleton<IAutomationBehavior>
            (provider => provider.GetRequiredService<ClickTemplateBehavior>());
        services.AddSingleton<IBehaviorRegistry, BehaviorRegistry>();
        services.AddSingleton<ITaskRunner, TaskRunner>();
    }
}
