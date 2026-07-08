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
    private readonly ApiService _api;

    [ObservableProperty] private DateTime _fromDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime _toDate = DateTime.Today;
    [ObservableProperty] private int _recordCount;
    [ObservableProperty] private int _errorCount;
    [ObservableProperty] private int _warningCount;
    [ObservableProperty] private bool _hasPreview;
    [ObservableProperty] private string _statusMessage = "Choose a reporting window, then Preview.";
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<Metro2ValidationIssueDto> Issues { get; } = new();

    public Metro2ExportViewModel(ApiService api) => _api = api;

    private Metro2GenerateRequest BuildRequest() => new()
    {
        FromDate = FromDate.Date,
        ToDate = ToDate.Date
    };

    [RelayCommand]
    private async Task PreviewAsync()
    {
        if (FromDate > ToDate)
        {
            StatusMessage = "From date must be on or before To date.";
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

        IsBusy = true;
        StatusMessage = "Generating file…";
        try
        {
            var (content, fileName) = await _api.Metro2GenerateAsync(BuildRequest());

            var dialog = new SaveFileDialog
            {
                FileName = fileName,
                Filter = "Metro 2 file (*.dat)|*.dat|All files (*.*)|*.*"
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
