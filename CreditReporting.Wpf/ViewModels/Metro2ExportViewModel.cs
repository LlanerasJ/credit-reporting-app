using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CreditReporting.Shared.Dtos;
using CreditReporting.Wpf.Services;
using Microsoft.Win32;

namespace CreditReporting.Wpf.ViewModels;

public partial class Metro2ExportViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly ApiService _api;

    [ObservableProperty] private DateTime _fromDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime _toDate = DateTime.Today;
    [ObservableProperty] private int _recordCount;
    [ObservableProperty] private int _errorCount;
    [ObservableProperty] private int _warningCount;
    [ObservableProperty] private bool _hasPreview;
    [ObservableProperty] private string _statusMessage = "Choose a reporting window, then Preview.";
    [ObservableProperty] private bool _isBusy;
    // True once the account picker has been populated for the current window; while
    // false, preview/generate report every account in range (the original behaviour).
    [ObservableProperty] private bool _accountsLoaded;

    public ObservableCollection<Metro2ValidationIssueDto> Issues { get; } = new();
    public ObservableCollection<Metro2AccountItem> Accounts { get; } = new();

    public Metro2ExportViewModel(SettingsService settings, ApiService api)
    {
        _settings = settings;
        _api = api;
    }

    // A different window makes the loaded accounts stale, so make the user reload.
    partial void OnFromDateChanged(DateTime value) => ResetAccountPicker();
    partial void OnToDateChanged(DateTime value) => ResetAccountPicker();

    private void ResetAccountPicker()
    {
        if (!AccountsLoaded && Accounts.Count == 0) return;
        Accounts.Clear();
        AccountsLoaded = false;
    }

    private Metro2GenerateRequest BuildRequest() => new()
    {
        FromDate = FromDate.Date,
        ToDate = ToDate.Date,
        // When the picker is populated, report exactly the checked accounts; otherwise
        // null means "all accounts in range".
        AccountIds = AccountsLoaded
            ? Accounts.Where(a => a.IsSelected).Select(a => a.Id).ToList()
            : null,
        // Read on every request so preview and generate agree, and so an edit in
        // Settings takes effect without reopening this screen.
        FurnisherIdentificationNumber = _settings.Current.FurnisherIdentificationNumber
    };

    /// <summary>Blocks preview/generate when the picker is shown but nothing is checked.</summary>
    private bool HasEmptyAccountSelection => AccountsLoaded && !Accounts.Any(a => a.IsSelected);

    [RelayCommand]
    private async Task LoadAccountsAsync()
    {
        if (FromDate > ToDate)
        {
            StatusMessage = "From date must be on or before To date.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Loading accounts…";
        try
        {
            var accounts = await _api.Metro2AccountsAsync(FromDate.Date, ToDate.Date);
            Accounts.Clear();
            foreach (var account in accounts)
                Accounts.Add(new Metro2AccountItem(account));

            // Leave the picker off when the window is empty so preview still reports "all".
            AccountsLoaded = accounts.Count > 0;
            StatusMessage = accounts.Count == 0
                ? "No accounts have activity in this window."
                : $"{accounts.Count} account(s) with activity; all selected. Uncheck any to exclude, then Preview.";
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

    [RelayCommand]
    private void SelectAllAccounts()
    {
        foreach (var account in Accounts)
            account.IsSelected = true;
    }

    [RelayCommand]
    private void ClearAccountSelection()
    {
        foreach (var account in Accounts)
            account.IsSelected = false;
    }

    [RelayCommand]
    private async Task PreviewAsync()
    {
        if (FromDate > ToDate)
        {
            StatusMessage = "From date must be on or before To date.";
            return;
        }
        if (HasEmptyAccountSelection)
        {
            StatusMessage = "Select at least one account to include in the export.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Building preview…";
        try
        {
            var preview = await _api.Metro2PreviewAsync(BuildRequest());
            RecordCount = preview.RecordCount;
            ErrorCount = preview.ErrorCount;
            WarningCount = preview.WarningCount;
            Issues.Clear();
            foreach (var issue in preview.Issues)
                Issues.Add(issue);
            HasPreview = true;
            StatusMessage =
                $"{preview.RecordCount} record(s) would be generated: {preview.ErrorCount} error(s), {preview.WarningCount} warning(s).";
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

    [RelayCommand]
    private async Task GenerateAsync()
    {
        if (FromDate > ToDate)
        {
            StatusMessage = "From date must be on or before To date.";
            return;
        }
        if (HasEmptyAccountSelection)
        {
            StatusMessage = "Select at least one account to include in the export.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Generating file…";
        try
        {
            var (content, fileName) = await _api.Metro2GenerateAsync(BuildRequest());

            var dialog = new SaveFileDialog
            {
                FileName = fileName,
                Filter = "Metro 2 file (*.dat)|*.dat|All files (*.*)|*.*",
                // Opens in the configured default folder; empty falls back to the last-used folder.
                InitialDirectory = _settings.Current.Metro2DefaultFolderLocation ?? ""
            };
            if (dialog.ShowDialog() == true)
            {
                await File.WriteAllBytesAsync(dialog.FileName, content);
                StatusMessage = $"Saved {content.Length:N0} bytes to {dialog.FileName}.";
            }
            else
            {
                StatusMessage = "Save cancelled.";
            }
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
}
