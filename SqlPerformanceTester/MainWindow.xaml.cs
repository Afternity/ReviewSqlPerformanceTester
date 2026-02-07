using SqlPerformanceTester.ViewModels;

namespace SqlPerformanceTester;

public partial class MainWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
