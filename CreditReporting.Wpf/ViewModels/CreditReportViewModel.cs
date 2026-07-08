using CommunityToolkit.Mvvm.ComponentModel;
using CreditReporting.Shared.Dtos;
using CreditReporting.Wpf.Services;

namespace CreditReporting.Wpf.ViewModels;

public partial class CreditReportViewModel : ObservableObject
{
    private readonly ApiService _api;

    [ObservableProperty] private CreditReportDto? _report;
    [ObservableProperty] private AccountDto? _selectedAccount;
    [ObservableProperty] private CreditScoreDto? _latestScore;
    [ObservableProperty] private string _statusMessage = "Search for a customer and choose View Report.";
    [ObservableProperty] private bool _isBusy;

    public bool HasReport => Report is not null;

    public CreditReportViewModel(ApiService api) => _api = api;

    public async Task LoadAsync(int customerId)
    {
        IsBusy = true;
        StatusMessage = "Loading credit report…";
        try
        {
            Report = await _api.GetCreditReportAsync(customerId);
            LatestScore = Report.Scores.FirstOrDefault();
            SelectedAccount = Report.Accounts.FirstOrDefault();
            StatusMessage = $"Report {Report.ReportId:N} generated {Report.GeneratedAtUtc:g} UTC.";
        }
        catch (ApiException ex)
        {
            Report = null;
            LatestScore = null;
            SelectedAccount = null;
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(HasReport));
        }
    }
}
