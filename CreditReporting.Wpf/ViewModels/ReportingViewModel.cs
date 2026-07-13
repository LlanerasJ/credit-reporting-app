using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CreditReporting.Shared.Dtos;
using CreditReporting.Wpf.Services;
using Microsoft.Win32;

namespace CreditReporting.Wpf.ViewModels;

/// <summary>One parameter input in the runner, rendered from its descriptor.</summary>
public partial class ReportParameterInputViewModel : ObservableObject
{
    [ObservableProperty] private string _value = "";

    public ReportParameterDto Definition { get; }
    public bool IsChoice { get; }
    public List<string> Options { get; }

    public string Label
    {
        get
        {
            string label = Definition.Label;
            if (Definition.Type == "date") label += " (yyyy-mm-dd)";
            if (Definition.Required) label += " *";
            return label;
        }
    }

    public ReportParameterInputViewModel(ReportParameterDto definition)
    {
        Definition = definition;
        IsChoice = definition.Type == "choice";
        // Optional choices get a blank entry so the selection can be cleared.
        Options = definition.Options is null ? new List<string>()
            : definition.Required ? definition.Options
            : new List<string> { "" }.Concat(definition.Options).ToList();
        _value = definition.DefaultValue ?? "";
    }
}

/// <summary>
/// The Reporting tab: a library of saved reports (mine + shared) on the left,
/// and a runner (pick a catalog report, fill parameters, run/save/export) on
/// the right.
/// </summary>
public partial class ReportingViewModel : ObservableObject
{
    private readonly ApiService _api;
    private bool _loaded;
    private bool _applyingSavedReport;
    private List<SavedReportDto> _allSaved = new();
    private ReportResultDto? _lastResult;

    public bool IsAdmin => _api.Role == "Admin";

    // --- library ----------------------------------------------------------

    [ObservableProperty] private string _libraryFilter = "";
    public ObservableCollection<SavedReportDto> SavedReports { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCommand))]
    private SavedReportDto? _selectedSavedReport;

    // --- runner -----------------------------------------------------------

    public ObservableCollection<ReportDefinitionDto> Catalog { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private ReportDefinitionDto? _selectedDefinition;

    public ObservableCollection<ReportParameterInputViewModel> ParameterInputs { get; } = new();

    [ObservableProperty] private string _saveName = "";
    [ObservableProperty] private string _saveDescription = "";
    [ObservableProperty] private bool _saveShared;

    [ObservableProperty] private string _statusMessage = "Pick a report type, or load one from the library.";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private DataView? _results;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCsvCommand))]
    private bool _hasResults;

    public ReportingViewModel(ApiService api) => _api = api;

    /// <summary>Called from the view's Loaded event; fetches catalog + library once.</summary>
    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        _loaded = true;
        await RefreshAsync();
    }

    partial void OnLibraryFilterChanged(string value) => ApplyLibraryFilter();

    partial void OnSelectedSavedReportChanged(SavedReportDto? value)
    {
        if (value is not null) ApplySavedReport(value);
    }

    partial void OnSelectedDefinitionChanged(ReportDefinitionDto? value)
    {
        ParameterInputs.Clear();
        if (value is null) return;
        foreach (var parameter in value.Parameters)
            ParameterInputs.Add(new ReportParameterInputViewModel(parameter));
        if (!_applyingSavedReport)
            StatusMessage = value.Description;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            if (Catalog.Count == 0)
            {
                foreach (var definition in await _api.GetReportCatalogAsync())
                    Catalog.Add(definition);
            }

            _allSaved = await _api.GetSavedReportsAsync();
            ApplyLibraryFilter();
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

    private void ApplyLibraryFilter()
    {
        int? selectedId = SelectedSavedReport?.Id;
        SavedReports.Clear();
        foreach (var report in _allSaved.Where(Matches))
            SavedReports.Add(report);
        // Reselect without re-applying the config to the runner.
        if (selectedId is not null)
        {
            _applyingSavedReport = true;
            SelectedSavedReport = SavedReports.FirstOrDefault(r => r.Id == selectedId);
            _applyingSavedReport = false;
        }

        bool Matches(SavedReportDto r) =>
            string.IsNullOrWhiteSpace(LibraryFilter)
            || r.Name.Contains(LibraryFilter, StringComparison.OrdinalIgnoreCase)
            || r.Description.Contains(LibraryFilter, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Loads a saved report's configuration into the runner.</summary>
    private void ApplySavedReport(SavedReportDto report)
    {
        if (_applyingSavedReport) return;
        _applyingSavedReport = true;
        try
        {
            // Null first so the inputs rebuild (with defaults) even when the
            // loaded report has the same type as the current selection.
            SelectedDefinition = null;
            SelectedDefinition = Catalog.FirstOrDefault(d =>
                d.Key.Equals(report.ReportType, StringComparison.OrdinalIgnoreCase));
            foreach (var input in ParameterInputs)
                if (report.Parameters.TryGetValue(input.Definition.Name, out var value))
                    input.Value = value ?? "";
            SaveName = report.Name;
            SaveDescription = report.Description;
            SaveShared = report.IsShared;
            StatusMessage = $"Loaded \"{report.Name}\". Run it, or adjust and save.";
        }
        finally
        {
            _applyingSavedReport = false;
        }
    }

    private Dictionary<string, string?> CurrentParameters() =>
        ParameterInputs
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .ToDictionary(p => p.Definition.Name, p => (string?)p.Value.Trim());

    private bool CanRun() => SelectedDefinition is not null;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        if (SelectedDefinition is null) return;

        IsBusy = true;
        StatusMessage = "Running report…";
        try
        {
            var result = await _api.RunReportAsync(new RunReportRequest
            {
                ReportType = SelectedDefinition.Key,
                Parameters = CurrentParameters()
            });
            _lastResult = result;
            Results = ToDataView(result);
            HasResults = true;
            StatusMessage = $"{result.DisplayName}: {result.RowCount} row(s), generated {result.GeneratedAtUtc:HH:mm:ss} UTC.";
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

    private static DataView ToDataView(ReportResultDto result)
    {
        var table = new DataTable();
        foreach (var column in result.Columns)
            table.Columns.Add(column.Name, typeof(string));
        foreach (var row in result.Rows)
            table.Rows.Add(row.Cast<object>().ToArray());
        return table.DefaultView;
    }

    // --- save / update / delete -------------------------------------------

    private SaveReportRequest BuildSaveRequest() => new()
    {
        Name = SaveName,
        Description = SaveDescription,
        ReportType = SelectedDefinition?.Key ?? "",
        Parameters = CurrentParameters(),
        // Non-admins can't see the Shared checkbox, but loading a shared report
        // sets SaveShared — force private so "save a copy" doesn't get rejected.
        IsShared = IsAdmin && SaveShared
    };

    [RelayCommand]
    private async Task SaveAsNewAsync()
    {
        if (SelectedDefinition is null)
        {
            StatusMessage = "Pick a report type before saving.";
            return;
        }
        if (string.IsNullOrWhiteSpace(SaveName))
        {
            StatusMessage = "Give the report a name before saving.";
            return;
        }

        IsBusy = true;
        try
        {
            var created = await _api.CreateSavedReportAsync(BuildSaveRequest());
            await RefreshAsync();
            SelectedSavedReport = SavedReports.FirstOrDefault(r => r.Id == created.Id);
            StatusMessage = $"Saved \"{created.Name}\".";
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

    private bool CanEditSelected() =>
        SelectedSavedReport is not null
        && (IsAdmin || SelectedSavedReport.OwnerUsername == _api.Username);

    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task UpdateSelectedAsync()
    {
        if (SelectedSavedReport is null || SelectedDefinition is null) return;
        if (string.IsNullOrWhiteSpace(SaveName))
        {
            StatusMessage = "Give the report a name before saving.";
            return;
        }

        IsBusy = true;
        try
        {
            var updated = await _api.UpdateSavedReportAsync(SelectedSavedReport.Id, BuildSaveRequest());
            await RefreshAsync();
            SelectedSavedReport = SavedReports.FirstOrDefault(r => r.Id == updated.Id);
            StatusMessage = $"Updated \"{updated.Name}\".";
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

    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedSavedReport is null) return;

        var confirm = MessageBox.Show(
            $"Delete \"{SelectedSavedReport.Name}\"?",
            "Delete saved report", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            await _api.DeleteSavedReportAsync(SelectedSavedReport.Id);
            SelectedSavedReport = null;
            await RefreshAsync();
            StatusMessage = "Saved report deleted.";
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

    // --- export -------------------------------------------------------------

    private bool CanExport() => HasResults;

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportCsvAsync()
    {
        if (_lastResult is null) return;

        var dialog = new SaveFileDialog
        {
            FileName = $"{_lastResult.ReportType}-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            Filter = "CSV file (*.csv)|*.csv|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true)
        {
            StatusMessage = "Export cancelled.";
            return;
        }

        try
        {
            var csv = new StringBuilder();
            csv.AppendLine(string.Join(",", _lastResult.Columns.Select(c => CsvEscape(c.Name))));
            foreach (var row in _lastResult.Rows)
                csv.AppendLine(string.Join(",", row.Select(CsvEscape)));
            await File.WriteAllTextAsync(dialog.FileName, csv.ToString());
            StatusMessage = $"Exported {_lastResult.RowCount} row(s) to {dialog.FileName}.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Could not write the file: {ex.Message}";
        }
    }

    private static string CsvEscape(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
