using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using XAU.Services.LiveCapture;

namespace XAU.ViewModels.Pages;

public partial class AchievementResearchLabViewModel
{
    [ObservableProperty] private bool _isLiveCaptureRunning;
    [ObservableProperty] private string _liveCaptureStatus = "Select a title, then start a passive capture session.";
    [ObservableProperty] private string _liveCaptureProcessStatus = "Not checked";
    [ObservableProperty] private string _liveCaptureDuration = "00:00:00";
    [ObservableProperty] private string _liveCaptureLastPoll = "Never";
    [ObservableProperty] private int _liveCapturePollIntervalSeconds = 10;
    [ObservableProperty] private ObservableCollection<CaptureSourceCapability> _liveCaptureCapabilities = new();
    [ObservableProperty] private ObservableCollection<LiveCaptureObservation> _liveCaptureObservations = new();
    [ObservableProperty] private LiveCaptureObservation? _selectedLiveCaptureObservation;

    private readonly DispatcherTimer _liveCaptureDurationTimer = new()
    {
        Interval = TimeSpan.FromSeconds(1)
    };

    public IReadOnlyList<int> LiveCapturePollIntervals { get; } = [10, 15];

    public string LiveCaptureTitleInfo => SelectedResearchTitle == null
        ? "No title selected"
        : $"{SelectedResearchTitle.Name} | Title ID {SelectedResearchTitle.TitleId}";

    private void InitializeLiveCapture()
    {
        _liveCaptureCoordinator.ObservationAdded += OnLiveCaptureObservationAdded;
        _liveCaptureCoordinator.SessionUpdated += OnLiveCaptureSessionUpdated;
        _liveCaptureDurationTimer.Tick += (_, _) => UpdateLiveCaptureDuration();
        if (_liveCaptureCoordinator.CurrentSession != null)
            ApplyLiveCaptureSession(_liveCaptureCoordinator.CurrentSession, true);
    }

    partial void OnSelectedResearchTitleChanged(ResearchTitleCard? value)
    {
        OnPropertyChanged(nameof(LiveCaptureTitleInfo));
        StartLiveCaptureCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLiveCaptureRunningChanged(bool value)
    {
        StartLiveCaptureCommand.NotifyCanExecuteChanged();
        StopLiveCaptureCommand.NotifyCanExecuteChanged();
        SendOneTestEventCommand.NotifyCanExecuteChanged();
        StartRangeTestCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanStartLiveCapture))]
    private async Task StartLiveCapture()
    {
        if (SelectedResearchTitle == null)
            return;

        LiveCaptureObservations.Clear();
        SelectedLiveCaptureObservation = null;
        var title = new LiveCaptureTitle(
            SelectedResearchTitle.TitleId,
            SelectedResearchTitle.Name,
            SelectedResearchTitle.PackageFamilyName);
        try
        {
            await _liveCaptureCoordinator.StartAsync(
                title,
                TimeSpan.FromSeconds(LiveCapturePollIntervalSeconds));
            if (_liveCaptureCoordinator.CurrentSession != null)
                ApplyLiveCaptureSession(_liveCaptureCoordinator.CurrentSession, true);
            _liveCaptureDurationTimer.Start();
        }
        catch (Exception ex)
        {
            LiveCaptureStatus = $"Capture error: {ex.Message}";
        }
    }

    private bool CanStartLiveCapture() =>
        SelectedResearchTitle != null
        && !IsLiveCaptureRunning
        && !IsSending
        && !IsRangeTesting;

    [RelayCommand(CanExecute = nameof(CanStopLiveCapture))]
    private async Task StopLiveCapture()
    {
        await _liveCaptureCoordinator.StopAsync();
        if (_liveCaptureCoordinator.CurrentSession != null)
            ApplyLiveCaptureSession(_liveCaptureCoordinator.CurrentSession, false);
        _liveCaptureDurationTimer.Stop();
    }

    private bool CanStopLiveCapture() => IsLiveCaptureRunning;

    [RelayCommand(CanExecute = nameof(CanExportLiveCapture))]
    private void CopyLiveCaptureSummary()
    {
        Clipboard.SetText(_liveCaptureCoordinator.ExportSessionJson());
        _snackbarService.Show(
            "Live Capture",
            "Sanitized capture summary copied.",
            Wpf.Ui.Controls.ControlAppearance.Success,
            new Wpf.Ui.Controls.SymbolIcon(Wpf.Ui.Common.SymbolRegular.ClipboardCheckmark24),
            _snackbarDuration);
    }

    [RelayCommand(CanExecute = nameof(CanExportLiveCapture))]
    private async Task ExportLiveCaptureSummary()
    {
        var session = _liveCaptureCoordinator.CurrentSession;
        if (session == null)
            return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export sanitized Live Capture summary",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = $"xau-live-capture-{SanitizeFileName(session.DisplayName)}-{DateTime.Now:yyyyMMdd-HHmmss}.json"
        };
        if (dialog.ShowDialog() != true)
            return;

        await File.WriteAllTextAsync(dialog.FileName, _liveCaptureCoordinator.ExportSessionJson());
        _snackbarService.Show(
            "Live Capture",
            "Sanitized capture summary exported.",
            Wpf.Ui.Controls.ControlAppearance.Success,
            new Wpf.Ui.Controls.SymbolIcon(Wpf.Ui.Common.SymbolRegular.Save24),
            _snackbarDuration);
    }

    private bool CanExportLiveCapture() => _liveCaptureCoordinator.CurrentSession != null;

    private void OnLiveCaptureObservationAdded(object? sender, LiveCaptureObservation observation)
    {
        DispatchToUi(() =>
        {
            while (LiveCaptureObservations.Count >= LiveCaptureCoordinator.MaximumObservations)
                LiveCaptureObservations.RemoveAt(0);
            LiveCaptureObservations.Add(observation);
            SelectedLiveCaptureObservation = observation;
            CopyLiveCaptureSummaryCommand.NotifyCanExecuteChanged();
            ExportLiveCaptureSummaryCommand.NotifyCanExecuteChanged();
        });
    }

    private void OnLiveCaptureSessionUpdated(object? sender, LiveCaptureSession session) =>
        DispatchToUi(() => ApplyLiveCaptureSession(session, false));

    private void ApplyLiveCaptureSession(LiveCaptureSession session, bool loadObservations)
    {
        IsLiveCaptureRunning = _liveCaptureCoordinator.IsRunning;
        LiveCaptureStatus = $"{session.Status}: {session.StatusMessage}";
        LiveCaptureProcessStatus = session.ProcessStatus;
        LiveCaptureLastPoll = session.LastSuccessfulApiPollUtc.HasValue
            ? session.LastSuccessfulApiPollUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
            : "Never";
        LiveCaptureCapabilities = new ObservableCollection<CaptureSourceCapability>(session.Capabilities);
        if (loadObservations)
            LiveCaptureObservations = new ObservableCollection<LiveCaptureObservation>(session.Observations);
        UpdateLiveCaptureDuration();
        if (IsLiveCaptureRunning)
            _liveCaptureDurationTimer.Start();
        else
            _liveCaptureDurationTimer.Stop();
        CopyLiveCaptureSummaryCommand.NotifyCanExecuteChanged();
        ExportLiveCaptureSummaryCommand.NotifyCanExecuteChanged();
    }

    private void UpdateLiveCaptureDuration()
    {
        var session = _liveCaptureCoordinator.CurrentSession;
        if (session == null)
        {
            LiveCaptureDuration = "00:00:00";
            return;
        }

        var duration = (session.EndedAtUtc ?? DateTimeOffset.UtcNow) - session.StartedAtUtc;
        LiveCaptureDuration = $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(character => invalid.Contains(character) ? '_' : character));
    }

    private static void DispatchToUi(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
            action();
        else
            _ = dispatcher.InvokeAsync(action);
    }
}
