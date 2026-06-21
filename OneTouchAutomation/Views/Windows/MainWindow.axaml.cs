using OneTouchAutomation.ViewModels;

namespace OneTouchAutomation.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
    }


    public MainWindow() : this(new MainWindowViewModel()) { }
}