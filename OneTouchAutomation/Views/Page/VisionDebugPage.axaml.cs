namespace OneTouchAutomation.Views;

public partial class VisionDebugPage : UserControl
{
    public VisionDebugPage()
    {
        this.InitializeComponent();
        DetachedFromVisualTree += (_, _) =>
        {
            if(DataContext is OneTouchAutomation.ViewModels.VisionDebugViewModel viewModel
               && viewModel.StopContinuousCaptureCommand.CanExecute(null))
            {
                viewModel.StopContinuousCaptureCommand.Execute(null);
            }
        };
    }

    private async void SelectTemplateFile_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;

        if(storageProvider is null)
        {
            return;
        }

        var files = await storageProvider.OpenFilePickerAsync(
            new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Choose template image",
                AllowMultiple = false,
                FileTypeFilter = [Avalonia.Platform.Storage.FilePickerFileTypes.ImageAll]
            });

        var path = files.Count == 0
            ? null
            : Avalonia.Platform.Storage.StorageProviderExtensions.TryGetLocalPath(files[0]);

        if(path is not null && DataContext is OneTouchAutomation.ViewModels.VisionDebugViewModel viewModel)
        {
            await viewModel.LoadTemplateFromPathAsync(path);
        }
    }

    private async void SelectOutputFolder_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;

        if(storageProvider is null)
        {
            return;
        }

        var folders = await storageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = "Choose debug output folder",
                AllowMultiple = false
            });

        var path = folders.Count == 0
            ? null
            : Avalonia.Platform.Storage.StorageProviderExtensions.TryGetLocalPath(folders[0]);

        if(path is not null && DataContext is OneTouchAutomation.ViewModels.VisionDebugViewModel viewModel)
        {
            viewModel.SetDebugOutputDirectory(path);
        }
    }
}
