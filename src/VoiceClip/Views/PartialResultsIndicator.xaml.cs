using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace VoiceClip.Views;

/// <summary>
/// Floating indicator showing partial speech recognition results near the tray.
/// </summary>
public partial class PartialResultsIndicator : Window, INotifyPropertyChanged
{
    private string _partialText = string.Empty;

    public string PartialText
    {
        get => _partialText;
        set
        {
            _partialText = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PartialText)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PartialResultsIndicator()
    {
        InitializeComponent();
        DataContext = this;
        PositionNearTray();
    }

    /// <summary>
    /// Updates the displayed partial text.
    /// </summary>
    public void UpdatePartialText(string text)
    {
        PartialText = text;
    }

    private void PositionNearTray()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Bottom - Height - 110; // Offset above history popup area
    }
}
