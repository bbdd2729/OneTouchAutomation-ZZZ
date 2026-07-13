namespace OneTouchAutomation.Views;

public partial class GamePage : UserControl
{
    public GamePage() { InitializeComponent(); }

    private async void SelectTaskTemplateFile_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;

        if(storageProvider is null)
        {
            return;
        }

        var files = await storageProvider.OpenFilePickerAsync(
            new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Choose task template image",
                AllowMultiple = false,
                FileTypeFilter = [Avalonia.Platform.Storage.FilePickerFileTypes.ImageAll]
            });

        var path = files.Count == 0
            ? null
            : Avalonia.Platform.Storage.StorageProviderExtensions.TryGetLocalPath(files[0]);

        if(path is not null && DataContext is OneTouchAutomation.ViewModels.GamePageViewModel viewModel
           && viewModel.SelectedTask is not null)
        {
            viewModel.SelectedTask.TemplatePath = path;
        }
    }
}
