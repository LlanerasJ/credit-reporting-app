using System.Windows.Controls;
using CreditReporting.Wpf.ViewModels;

namespace CreditReporting.Wpf.Views;

public partial class ReportingView : UserControl
{
    public ReportingView()
    {
        InitializeComponent();
        // Fetch the catalog and saved-report library the first time the tab is shown.
        Loaded += async (_, _) =>
        {
            if (DataContext is ReportingViewModel vm)
                await vm.EnsureLoadedAsync();
        };
    }
}
