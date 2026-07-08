using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CreditReporting.Shared.Dtos;
using CreditReporting.Wpf.Services;
using Microsoft.Win32;

namespace CreditReporting.Wpf.ViewModels;

public partial class Metro2ImportViewModel : ObservableObject
{
    private readonly ApiService _api;

    [ObservableProperty] private string _loadedFile = "";
    [ObservableProperty] private Metro2HeaderDto? _header;
    [ObservableProperty] private Metro2TrailerDto? _trailer;
    [ObservableProperty] private string _statusMessage = "Load a Metro 2 file to preview its contents.";
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<Metro2ParsedRecordDto> Records { get; } = new();
    public ObservableCollection<Metro2ValidationIssueDto> Issues { get; } = new();

    public Metro2ImportViewModel(ApiService api) => _api = api;

    [RelayCommand]
    private async Task BrowseAndParseAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Metro 2 file (*.dat;*.txt)|*.dat;*.txt|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        IsBusy = true;
        StatusMessage = "Parsing…";
        try
        {
            var result = await _api.Metro2ParseAsync(dialog.FileName);

            LoadedFile = dialog.FileName;
            Header = result.Header;
            Trailer = result.Trailer;

            Records.Clear();
            foreach (var record in result.Records)
                Records.Add(record);

            Issues.Clear();
            foreach (var issue in result.Issues)
                Issues.Add(issue);

            int errors = result.Issues.Count(i => i.Severity == "Error");
            StatusMessage =
                $"Parsed {result.Records.Count} record(s): {errors} error(s), {result.Issues.Count - errors} warning(s).";
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
