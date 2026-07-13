namespace OneTouchAutomation.Views;

public partial class BehaviorDebugPage : UserControl
{
    public BehaviorDebugPage() { this.InitializeComponent(); }

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
                Title = "Choose behavior template image",
                AllowMultiple = false,
                FileTypeFilter = [Avalonia.Platform.Storage.FilePickerFileTypes.ImageAll]
            });

        var path = files.Count == 0
            ? null
            : Avalonia.Platform.Storage.StorageProviderExtensions.TryGetLocalPath(files[0]);

        if(path is not null && DataContext is OneTouchAutomation.ViewModels.BehaviorDebugViewModel viewModel)
        {
            viewModel.TemplatePath = path;
        }
    }
}
