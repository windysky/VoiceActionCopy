# SPEC-VOICECLIP-001: VoiceClip — Voice Dictation Clipboard for Windows

| Field | Value |
|-------|-------|
| **SPEC ID** | SPEC-VOICECLIP-001 |
| **Status** | Implemented |
| **Priority** | High |
| **Author** | windysky |
| **Created** | 2026-04-27 |
| **Target Platform** | Windows 11 (10.0.22621+) |
| **Tech Stack** | C# / .NET 8 WPF |

---

## 1. Problem Statement

Windows Voice Access provides accurate voice dictation, but text is typed directly into the focused window. If the wrong window has focus, dictation is lost or goes to the wrong application. There is no built-in way to capture, store, and selectively reuse voice dictations.

**Existing tools evaluated:**

| Tool | Gap |
|------|-----|
| Handy (open source, whisper.cpp) | Targets active window, no history buffer |
| asr2clip (Python) | No history buffer, no UI for selective paste |
| Voice Capture (MS Store) | Targets active window, no buffer |
| Windows Clipboard History (Win+V) | Only captures explicitly copied text |
| Dragon Copilot | Expensive, healthcare-focused, overkill |

**No existing tool solves the problem.** Custom app needed.

---

## 2. Solution Overview

**VoiceClip** is a lightweight Windows system tray app that:
1. Provides its own dictation using the **same Windows.Media.SpeechRecognition engine** as Voice Access
2. Stores each dictation session as a persistent history entry
3. Shows a popup panel where user can click any entry to copy it to clipboard
4. User then pastes (Ctrl+V) into any app they choose

VoiceClip is **independent** of Voice Access — both can coexist. User chooses which to use per situation.

---

## 3. Requirements (EARS Format)

### REQ-VC-001: System Tray Presence
**When** the app starts, **the system shall** display an icon in the Windows system tray.

- Icon shows microphone graphic
- Right-click context menu: Dictate (Ctrl+Alt+D), History (Ctrl+Alt+V), Settings, Exit
- Icon changes visually when recording (e.g., red indicator)
- Double-click opens history popup

### REQ-VC-002: Dictation Toggle
**When** the user presses Ctrl+Alt+D, **the system shall** toggle dictation recording on/off.

- First press: starts recording audio and transcribing via Windows.Media.SpeechRecognition
- Second press: stops recording, saves the full transcription as a new history entry
- System tray icon visually indicates recording state
- If speech recognition is not enabled in Windows, the system shall open `ms-settings:privacy-speech`

### REQ-VC-003: Continuous Speech Recognition
**While** dictation is active, **the system shall** continuously recognize speech and display partial results.

- Uses `SpeechRecognizer` with `ContinuousRecognitionSession`
- Uses `SpeechRecognitionTopicConstraint( SpeechRecognitionScenario.Dictation, "dictation")`
- Partial results shown in a small floating indicator near the tray
- Auto-stop after 60 seconds of silence (configurable)
- Supports the system's current Windows language (configurable)

### REQ-VC-004: History Popup
**When** the user presses Ctrl+Alt+V or double-clicks the tray icon, **the system shall** display a history popup.

- Shows dictation entries in reverse chronological order (newest first)
- Each entry shows: timestamp, first 80 characters preview, duration
- Entries are selectable — click to copy full text to clipboard
- Visual feedback (brief highlight) on copy
- Search bar at top to filter entries
- "Clear All" button with confirmation dialog
- "Delete" button per entry
- Popup is borderless, positioned near the system tray
- Popup closes when clicking outside or pressing Escape

### REQ-VC-005: Copy to Clipboard
**When** the user clicks a dictation entry in the history popup, **the system shall** copy the full text to the Windows clipboard.

- Uses `Clipboard.SetText()` via WPF Clipboard
- Brief toast notification "Copied to clipboard" (auto-dismiss after 2 seconds)
- User pastes manually with Ctrl+V into target application
- No automatic typing/simulation into other apps

### REQ-VC-006: Persistent History Storage
**The system shall** persist dictation history to disk so it survives app restarts.

- Storage location: `%APPDATA%\VoiceClip\history.json`
- Each entry: `{ id, text, timestamp, duration_seconds }`
- Maximum 500 entries (oldest auto-deleted when limit reached, configurable)
- File created on first run, directory created if missing
- JSON format for easy backup and inspection

### REQ-VC-007: Global Hotkey Registration
**The system shall** register global hotkeys that work regardless of which application has focus.

- Ctrl+Alt+D: Toggle dictation
- Ctrl+Alt+V: Show history popup
- Hotkeys registered via Windows API (`RegisterHotKey` / P/Invoke)
- Hotkeys released on app exit
- If hotkey already registered by another app, show error notification with alternative suggestion

### REQ-VC-008: Windows Speech Recognition Dependencies
**The system shall** handle Windows speech recognition prerequisites gracefully.

- Requires: Windows 11 (10.0.22621+) with speech recognition enabled
- On startup, checks if speech recognition is available
- If not available: shows notification with link to enable (`ms-settings:privacy-speech`)
- NuGet packages: `Microsoft.Windows.SDK.Contracts` (10.0.22621.x), `Microsoft.Windows.CsWinRt`

### REQ-VC-009: System Tray Icon States
**The system shall** indicate app state through the tray icon.

| State | Icon | Tooltip |
|-------|------|---------|
| Idle (ready) | Microphone | "VoiceClip — Ready" |
| Recording | Microphone (red dot) | "VoiceClip — Recording..." |
| Error | Microphone (yellow warning) | "VoiceClip — Error: {message}" |

### REQ-VC-010: Settings
**The system shall** provide a settings dialog accessible from the tray context menu.

- Language selection (default: system language)
- Silence timeout (default: 60 seconds, range: 10-300)
- Maximum history entries (default: 500, range: 50-5000)
- Hotkey configuration (display current, allow future customization)
- Run on Windows startup toggle
- Settings persisted to `%APPDATA%\VoiceClip\settings.json`

---

## 4. Architecture

### 4.1 Project Structure

```
VoiceClip/
├── VoiceClip.sln
├── src/
│   └── VoiceClip/
│       ├── VoiceClip.csproj
│       ├── App.xaml / App.xaml.cs          # WPF app entry, singleton mode
│       ├── MainWindow.xaml.cs              # Hidden main window (for WPF requirements)
│       ├── Models/
│       │   ├── DictationEntry.cs           # History entry model
│       │   └── AppSettings.cs              # Settings model
│       ├── Services/
│       │   ├── SpeechRecognitionService.cs  # Windows.Media.SpeechRecognition wrapper
│       │   ├── HistoryService.cs           # JSON persistence for dictation history
│       │   ├── HotkeyService.cs            # Global hotkey registration (P/Invoke)
│       │   └── ClipboardService.cs         # Clipboard operations
│       ├── ViewModels/
│       │   └── HistoryViewModel.cs         # History popup data binding
│       ├── Views/
│       │   ├── HistoryPopup.xaml           # Borderless popup near tray
│       │   ├── HistoryPopup.xaml.cs
│       │   ├── SettingsWindow.xaml         # Settings dialog
│       │   └── SettingsWindow.xaml.cs
│       ├── Controls/
│       │   └── DictationEntryItem.xaml     # Custom list item template
│       ├── Tray/
│       │   └── TrayIconManager.cs          # System tray icon + context menu
│       ├── Helpers/
│       │   └── WinRTAsyncHelper.cs         # AsTask() reflection wrapper (from Rick Strahl)
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
    └── SPEC-VOICECLIP-001.md              # This file
```

### 4.2 Component Diagram

```
┌─────────────────────────────────────────────────────────┐
│  VoiceClip (System Tray App)                             │
│                                                          │
│  ┌──────────────────┐     ┌───────────────────────────┐ │
│  │  HotkeyService    │────▶│  SpeechRecognitionService  │ │
│  │  (Ctrl+Alt+D/V)  │     │  (Windows.Media.Speech)    │ │
│  └──────────────────┘     └──────────┬────────────────┘ │
│                                      │ ResultGenerated   │
│                                      ▼                   │
│  ┌──────────────────┐     ┌───────────────────────────┐ │
│  │  TrayIconManager │◀────│  HistoryService             │ │
│  │  (icon states)    │     │  (JSON persistence)         │ │
│  └──────────────────┘     └──────────┬────────────────┘ │
│                                      │                   │
│  ┌──────────────────┐     ┌──────────▼────────────────┐ │
│  │  HistoryPopup     │────▶│  ClipboardService          │ │
│  │  (WPF popup)      │     │  (Clipboard.SetText)       │ │
│  └──────────────────┘     └───────────────────────────┘ │
│                                                          │
│  ┌──────────────────────────────────────────────────┐   │
│  │  WinRTAsyncHelper (reflection-based AsTask)       │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

### 4.3 Key Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Speech engine | Windows.Media.SpeechRecognition | Same engine as Voice Access, best accuracy, offline |
| Async wrapper | Reflection-based AsTask() | WinRT/SDK type conflict in WPF (see Rick Strahl article) |
| Storage format | JSON file | Simple, human-readable, no database dependency |
| UI framework | WPF | Rich data binding, popup support, XAML templating |
| Global hotkeys | P/Invoke RegisterHotKey | Required for system-wide hotkeys in .NET |
| Target framework | .NET 8 | LTS, modern C# features, single-file publish |

### 4.4 WinRT Async Helper (Critical)

The Windows SDK and WinRT packages have overlapping `AsTask()` extension methods that cannot be resolved in WPF. A reflection-based wrapper is required:

```csharp
// Based on Rick Strahl's approach
Task AsTask(object action)
{
    var assembly = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName()?.Name == "Microsoft.Windows.SDK.NET");
    var type = assembly.GetTypes()
        .FirstOrDefault(t => t.FullName == "System.WindowsRuntimeSystemExtensions");
    var method = type.GetMethod("AsTask", [typeof(IAsyncAction)]);
    return method.Invoke(null, [action]) as Task;
}
```

---

## 5. Acceptance Criteria

### AC-001: Basic Dictation Flow
1. Start VoiceClip → tray icon appears (idle state)
2. Press Ctrl+Alt+D → icon changes to recording state
3. Speak into microphone → partial results shown in floating indicator
4. Press Ctrl+Alt+D again → recording stops, text saved as history entry
5. Entry appears in history popup (Ctrl+Alt+V)
6. Click entry → "Copied to clipboard" notification
7. Open any app, press Ctrl+V → dictation text pasted

### AC-002: Persistence
1. Record 3 dictation entries
2. Close VoiceClip (Exit from tray menu)
3. Restart VoiceClip
4. Open history → all 3 entries present with correct timestamps

### AC-003: Search
1. Record entries with distinct content
2. Open history popup
3. Type keyword in search bar
4. Only matching entries shown
5. Clear search → all entries shown again

### AC-004: Error Handling
1. Disable speech recognition in Windows settings
2. Start VoiceClip → notification appears with link to enable
3. Try to dictate → graceful error message

### AC-005: Coexistence with Voice Access
1. Voice Access is running
2. Start VoiceClip → both icons in tray
3. Use VoiceClip to dictate → text goes to VoiceClip buffer
4. Use Voice Access separately → text goes to focused window
5. No conflicts between the two

---

## 6. Implementation Phases

### Phase 1: Core Infrastructure
- Project setup (.NET 8 WPF)
- System tray icon with context menu
- Global hotkey registration
- Settings models and persistence
- Single-instance enforcement (named mutex)

### Phase 2: Speech Recognition
- SpeechRecognitionService wrapper
- WinRT async helper (reflection-based AsTask)
- Continuous dictation session management
- Partial result display (floating indicator)
- Recording state management

### Phase 3: History & Clipboard
- HistoryService (JSON persistence)
- DictationEntry model
- History popup (WPF borderless window)
- Clipboard copy on click
- Search/filter functionality
- Delete individual entries, Clear All

### Phase 4: Polish & Settings
- Settings dialog
- Run on startup (registry or Task Scheduler)
- Icon state transitions (idle/recording/error)
- Toast notifications
- Language configuration
- Auto-update check (optional, future)

### Phase 5: Testing & Packaging
- Unit tests for HistoryService, ClipboardService
- Manual integration testing
- Single-file publish (.NET 8 publish profile)
- README with setup instructions

---

## 7. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| WinRT async conflicts | High | High | Reflection-based AsTask wrapper (proven by Rick Strahl) |
| Hotkey collision with other apps | Medium | Low | Configurable hotkeys, graceful error message |
| Speech recognition not available | Low | High | Graceful detection, link to Windows settings |
| App size (~30MB with SDK deps) | Medium | Low | Acceptable for desktop app, single-file publish |
| Voice Access conflict | Low | Medium | Separate microphone sessions, mutex to avoid overlap |

---

## 8. Sources

- Rick Strahl, "Using Windows.Media.SpeechRecognition in WPF" — https://weblog.west-wind.com/posts/2025/Mar/24/Using-WindowsMedia-SpeechRecognition-in-WPF
- Microsoft Learn, "Enable continuous dictation" — https://learn.microsoft.com/en-us/windows/uwp/ui-input/enable-continuous-dictation
- Handy (open source speech-to-text) — https://github.com/cjpais/handy
- asr2clip — https://github.com/Oaklight/asr2clip
- Windows Voice Access documentation — https://support.microsoft.com/en-us/windows/use-voice-recognition-in-windows-83ff75bd-63eb-0b6c-18d4-6fae94050571

---

Version: 1.0.0
Last Updated: 2026-04-27
