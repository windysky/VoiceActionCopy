using System.Windows;
using VoiceClip.Models;
using VoiceClip.Services;

namespace VoiceClip.Views;

/// <summary>
/// Settings dialog for VoiceClip configuration.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly AppSettings _editableSettings;
    private readonly StartupService _startupService;

    public SettingsWindow(ISettingsService settingsService, AppSettings settings)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _settings = settings;
        _startupService = new StartupService();
        _editableSettings = settings.Clone();
        _editableSettings.RunOnStartup = _startupService.IsStartupEnabled();
        DataContext = _editableSettings;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var requiresRestart =
            _settings.Language != _editableSettings.Language ||
            _settings.SilenceTimeoutSeconds != _editableSettings.SilenceTimeoutSeconds ||
            _settings.MaxHistoryEntries != _editableSettings.MaxHistoryEntries;

        _settings.CopyFrom(_editableSettings);
        _settingsService.Save(_settings);

        if (_settings.RunOnStartup)
        {
            var exePath = Environment.ProcessPath;
            if (!_startupService.SetStartup(true, exePath))
            {
                MessageBox.Show("Could not enable startup. Run VoiceClip as administrator once to set up.",
                    "VoiceClip", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        else
        {
            _startupService.SetStartup(false);
        }

        if (requiresRestart)
        {
            MessageBox.Show(
                "Language, silence timeout, and history size changes apply the next time VoiceClip starts.",
                "VoiceClip",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
