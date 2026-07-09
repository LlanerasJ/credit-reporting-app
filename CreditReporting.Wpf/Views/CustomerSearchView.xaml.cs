using System.Windows.Controls;
using System.Windows.Input;
using CreditReporting.Wpf.ViewModels;

namespace CreditReporting.Wpf.Views;

public partial class CustomerSearchView : UserControl
{
    public CustomerSearchView() => InitializeComponent();

    // Double-click opens the report for the selected row. The command is
    // dispatched at Background priority so it runs after the DataGrid finishes
    // handling the double-click; otherwise the TabControl snaps its selection
    // back to this tab and the switch to the report tab is undone.
    private void Results_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not CustomerSearchViewModel vm || !vm.ViewReportCommand.CanExecute(null))
            return;

        Dispatcher.BeginInvoke(
            new Action(() => vm.ViewReportCommand.Execute(null)),
            System.Windows.Threading.DispatcherPriority.Background);
    }
}
