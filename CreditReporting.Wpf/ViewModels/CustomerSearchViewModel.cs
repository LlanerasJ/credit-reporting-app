using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CreditReporting.Shared.Dtos;
using CreditReporting.Wpf.Services;

namespace CreditReporting.Wpf.ViewModels;

public partial class CustomerSearchViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly Func<int, Task> _openReport;

    [ObservableProperty] private string _nameQuery = "";
    [ObservableProperty] private string _ssnLast4Query = "";
    [ObservableProperty] private string _statusMessage = "Enter a name or the last 4 digits of an SSN.";
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ViewReportCommand))]
    private CustomerSummaryDto? _selectedCustomer;

    public ObservableCollection<CustomerSummaryDto> Results { get; } = new();

    public CustomerSearchViewModel(ApiService api, Func<int, Task> openReport)
    {
        _api = api;
        _openReport = openReport;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(NameQuery) && string.IsNullOrWhiteSpace(SsnLast4Query))
        {
            StatusMessage = "Enter a name or the last 4 digits of an SSN.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Searching…";
        try
        {
            var results = await _api.SearchCustomersAsync(NameQuery, SsnLast4Query);
            Results.Clear();
            foreach (var customer in results)
                Results.Add(customer);
            StatusMessage = results.Count == 0
                ? "No customers matched."
                : $"{results.Count} customer(s) found.";
        }
        catch (ApiException ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanViewReport() => SelectedCustomer is not null;

    [RelayCommand(CanExecute = nameof(CanViewReport))]
    private Task ViewReportAsync() =>
        SelectedCustomer is null ? Task.CompletedTask : _openReport(SelectedCustomer.Id);
}
