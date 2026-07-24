using CommunityToolkit.Mvvm.ComponentModel;
using CreditReporting.Shared.Dtos;

namespace CreditReporting.Wpf.ViewModels;

/// <summary>A single checkable row in the Metro 2 export account picker.</summary>
public partial class Metro2AccountItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected = true;

    public Metro2AccountItem(Metro2AccountSummaryDto account)
    {
        Id = account.Id;
        AccountNumberMasked = account.AccountNumberMasked;
        CustomerName = account.CustomerName;
        AccountType = account.AccountType;
        CurrentBalance = account.CurrentBalance;
    }

    public int Id { get; }
    public string AccountNumberMasked { get; }
    public string CustomerName { get; }
    public string AccountType { get; }
    public decimal CurrentBalance { get; }
}
