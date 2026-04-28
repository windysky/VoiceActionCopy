using System.Windows;
using System.Windows.Input;
using VoiceClip.ViewModels;

namespace VoiceClip.Views;

/// <summary>
/// Borderless popup window for displaying dictation history.
/// Positioned near the system tray.
/// </summary>
public partial class HistoryPopup : Window
{
    private readonly HistoryViewModel _viewModel;

    public HistoryPopup(HistoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.EntryCopied += OnEntryCopied;
        Closed += (s, e) => _viewModel.EntryCopied -= OnEntryCopied;

        PositionNearTray();

        // Close on Escape
        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        };
    }

    /// <summary>
    /// Positions the popup near the system tray (bottom-right of screen).
    /// </summary>
    private void PositionNearTray()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Bottom - Height - 20;
    }

    private void OnEntryCopied(object? sender, EventArgs e)
    {
        // Brief visual feedback could be added here
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        Close();
    }

    private void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Delete all dictation history?", "VoiceClip",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            _viewModel.ClearAllCommand.Execute(null);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
