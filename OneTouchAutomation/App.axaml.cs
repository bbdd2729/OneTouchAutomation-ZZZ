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
                        "OneTouchAutomation.Services.Automation.History",
                        "OneTouchAutomation.Services.Automation.State",
                        "OneTouchAutomation.Services.Automation.Persistence",
                        "OneTouchAutomation.Services.Automation.Daily",
                        "OneTouchAutomation.Services.Automation.Workflows",
                        "OneTouchAutomation.Services.Vision",
                                                "OneTouchAutomation.Services.Debug",
                                                "OneTouchAutomation.Services.Input")).AsImplementedInterfaces().
                              WithSingletonLifetime());
        services.AddSingleton<ClickTemplateBehavior>();
        services.AddSingleton<IAutomationBehavior<ClickTemplateBehaviorParameters>>
            (provider => provider.GetRequiredService<ClickTemplateBehavior>());
        services.AddSingleton<IAutomationBehavior>
            (provider => provider.GetRequiredService<ClickTemplateBehavior>());
        services.AddSingleton<WaitForTemplateBehavior>();
        services.AddSingleton<IAutomationBehavior<WaitForTemplateBehaviorParameters>>
            (provider => provider.GetRequiredService<WaitForTemplateBehavior>());
        services.AddSingleton<IAutomationBehavior>
            (provider => provider.GetRequiredService<WaitForTemplateBehavior>());
        services.AddSingleton<WaitForTemplateDisappearBehavior>();
        services.AddSingleton<IAutomationBehavior<WaitForTemplateDisappearBehaviorParameters>>
            (provider => provider.GetRequiredService<WaitForTemplateDisappearBehavior>());
        services.AddSingleton<IAutomationBehavior>
            (provider => provider.GetRequiredService<WaitForTemplateDisappearBehavior>());
        services.AddSingleton<PressKeyBehavior>();
        services.AddSingleton<IAutomationBehavior<PressKeyBehaviorParameters>>
            (provider => provider.GetRequiredService<PressKeyBehavior>());
        services.AddSingleton<IAutomationBehavior>
            (provider => provider.GetRequiredService<PressKeyBehavior>());
        services.AddSingleton<DelayBehavior>();
        services.AddSingleton<IAutomationBehavior<DelayBehaviorParameters>>
            (provider => provider.GetRequiredService<DelayBehavior>());
        services.AddSingleton<IAutomationBehavior>
            (provider => provider.GetRequiredService<DelayBehavior>());
        services.AddSingleton<RunWorkflowBehavior>();
        services.AddSingleton<IAutomationBehavior<RunWorkflowBehaviorParameters>>
            (provider => provider.GetRequiredService<RunWorkflowBehavior>());
        services.AddSingleton<IAutomationBehavior>
            (provider => provider.GetRequiredService<RunWorkflowBehavior>());
        services.AddSingleton<RunDailyWorkflowBehavior>();
        services.AddSingleton<IAutomationBehavior<RunDailyWorkflowBehaviorParameters>>
            (provider => provider.GetRequiredService<RunDailyWorkflowBehavior>());
        services.AddSingleton<IAutomationBehavior>
            (provider => provider.GetRequiredService<RunDailyWorkflowBehavior>());
        services.AddSingleton<CheckScreenBehavior>();
        services.AddSingleton<IAutomationBehavior<CheckScreenBehaviorParameters>>
            (provider => provider.GetRequiredService<CheckScreenBehavior>());
        services.AddSingleton<IAutomationBehavior>
            (provider => provider.GetRequiredService<CheckScreenBehavior>());
        services.AddSingleton<WaitForScreenBehavior>();
        services.AddSingleton<IAutomationBehavior<WaitForScreenBehaviorParameters>>
            (provider => provider.GetRequiredService<WaitForScreenBehavior>());
        services.AddSingleton<IAutomationBehavior>
            (provider => provider.GetRequiredService<WaitForScreenBehavior>());
        services.AddSingleton<IBehaviorRegistry, BehaviorRegistry>();
        services.AddSingleton<ITaskRunner, TaskRunner>();
    }
}
