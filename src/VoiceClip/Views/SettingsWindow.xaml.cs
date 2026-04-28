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
    private readonly StartupService _startupService;

    public SettingsWindow(ISettingsService settingsService, AppSettings settings)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _settings = settings;
        _startupService = new StartupService();
        DataContext = _settings;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
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

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
