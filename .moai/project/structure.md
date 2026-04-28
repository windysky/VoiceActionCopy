# VoiceClip — Project Structure

## Architecture Pattern

WPF MVVM (Model-View-ViewModel) with service layer architecture.

## Directory Layout

```
VoiceClip/
├── VoiceClip.sln
├── src/
│   └── VoiceClip/
│       ├── VoiceClip.csproj              # .NET 8 WPF project
│       ├── App.xaml / App.xaml.cs         # WPF app entry, singleton mode
│       ├── MainWindow.xaml.cs             # Hidden main window (WPF requirements)
│       ├── Models/
│       │   ├── DictationEntry.cs          # History entry data model
│       │   └── AppSettings.cs             # Settings data model
│       ├── Services/
│       │   ├── SpeechRecognitionService.cs # Windows.Media.SpeechRecognition wrapper
│       │   ├── HistoryService.cs          # JSON persistence for dictation history
│       │   ├── HotkeyService.cs           # Global hotkey registration (P/Invoke)
│       │   └── ClipboardService.cs        # Clipboard operations
│       ├── ViewModels/
│       │   └── HistoryViewModel.cs        # History popup data binding
│       ├── Views/
│       │   ├── HistoryPopup.xaml/.cs      # Borderless popup near tray
│       │   └── SettingsWindow.xaml/.cs    # Settings dialog
│       ├── Controls/
│       │   └── DictationEntryItem.xaml    # Custom list item template
│       ├── Tray/
│       │   └── TrayIconManager.cs         # System tray icon + context menu
│       ├── Helpers/
│       │   └── WinRTAsyncHelper.cs        # Reflection-based AsTask() wrapper
│       └── Assets/
│           ├── mic-idle.ico
│           ├── mic-recording.ico
│           └── mic-error.ico
├── tests/
│   └── VoiceClip.Tests/
│       ├── HistoryServiceTests.cs
│       ├── ClipboardServiceTests.cs
│       └── DictationEntryTests.cs
└── docs/
    └── SPEC-VOICECLIP-001.md
```

## Module Responsibilities

| Module | Purpose | Key Files |
|--------|---------|-----------|
| Models | Data models for dictation entries and settings | DictationEntry.cs, AppSettings.cs |
| Services | Business logic and system integration | SpeechRecognitionService.cs, HistoryService.cs, HotkeyService.cs, ClipboardService.cs |
| ViewModels | Data binding between Views and Services | HistoryViewModel.cs |
| Views | WPF windows and popups | HistoryPopup, SettingsWindow |
| Controls | Custom XAML templates | DictationEntryItem.xaml |
| Tray | System tray management | TrayIconManager.cs |
| Helpers | WinRT interop utilities | WinRTAsyncHelper.cs |

## Data Flow

1. **HotkeyService** registers global hotkeys → triggers **SpeechRecognitionService**
2. **SpeechRecognitionService** captures speech → generates text results
3. Results stored via **HistoryService** → persisted to `%APPDATA%\VoiceClip\history.json`
4. **HistoryPopup** (View) displays entries via **HistoryViewModel**
5. User clicks entry → **ClipboardService** copies text to clipboard
6. **TrayIconManager** reflects app state in tray icon
