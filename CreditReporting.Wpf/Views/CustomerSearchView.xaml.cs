using System.Windows.Controls;
using System.Windows.Input;
using CreditReporting.Wpf.ViewModels;

namespace CreditReporting.Wpf.Views;

public partial class CustomerSearchView : UserControl
{
    public CustomerSearchView() => InitializeComponent();

    // Double-click opens the report for the selected row.
    private void Results_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is CustomerSearchViewModel vm && vm.ViewReportCommand.CanExecute(null))
            vm.ViewReportCommand.Execute(null);
    }
}
