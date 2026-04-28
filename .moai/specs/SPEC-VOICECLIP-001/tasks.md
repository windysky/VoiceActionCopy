## Task Decomposition
SPEC: SPEC-VOICECLIP-001

### Phase 1: Foundation & First Vertical Slice

| Task ID | Description | Requirement | Dependencies | Planned Files | Status |
|---------|-------------|-------------|--------------|---------------|--------|
| T-001 | Create .NET 8 WPF solution with project structure, NuGet packages | REQ-VC-008 | - | VoiceClip.sln, src/VoiceClip/VoiceClip.csproj, tests/VoiceClip.Tests/VoiceClip.Tests.csproj | pending |
| T-002 | Implement DictationEntry model | REQ-VC-006 | T-001 | Models/DictationEntry.cs, tests/DictationEntryTests.cs | pending |
| T-003 | Implement AppSettings model | REQ-VC-010 | T-001 | Models/AppSettings.cs, tests/AppSettingsTests.cs | pending |
| T-004 | Implement WinRTAsyncHelper (reflection-based AsTask) | REQ-VC-008 | T-001 | Helpers/WinRTAsyncHelper.cs, tests/WinRTAsyncHelperTests.cs | pending |
| T-005 | Implement HistoryService (JSON persistence CRUD) | REQ-VC-006 | T-002 | Services/HistoryService.cs, tests/HistoryServiceTests.cs | pending |
| T-006 | Implement ClipboardService | REQ-VC-005 | T-001 | Services/ClipboardService.cs, tests/ClipboardServiceTests.cs | pending |
| T-007 | Implement TrayIconManager (system tray icon) | REQ-VC-001, REQ-VC-009 | T-001 | Tray/TrayIconManager.cs | pending |
| T-008 | Implement HotkeyService (P/Invoke RegisterHotKey) | REQ-VC-007 | T-007 | Services/HotkeyService.cs, tests/HotkeyServiceTests.cs | pending |
| T-009 | Implement SpeechRecognitionService (continuous dictation) | REQ-VC-002, REQ-VC-003 | T-004 | Services/SpeechRecognitionService.cs, tests/SpeechRecognitionServiceTests.cs | pending |

### Phase 2: History UI & User Interaction

| Task ID | Description | Requirement | Dependencies | Planned Files | Status |
|---------|-------------|-------------|--------------|---------------|--------|
| T-010 | Implement HistoryViewModel (ObservableCollection + search) | REQ-VC-004 | T-005, T-006 | ViewModels/HistoryViewModel.cs, tests/HistoryViewModelTests.cs | pending |
| T-011 | Implement HistoryPopup (borderless WPF window near tray) | REQ-VC-004 | T-010 | Views/HistoryPopup.xaml, Views/HistoryPopup.xaml.cs | pending |
| T-012 | Implement DictationEntryItem custom control | REQ-VC-004 | T-010 | Controls/DictationEntryItem.xaml | pending |
| T-013 | Wire ClipboardService to history popup click | REQ-VC-005 | T-011, T-006 | Views/HistoryPopup.xaml.cs | pending |
| T-014 | Implement toast notification system | REQ-VC-005 | T-007 | Helpers/ToastNotification.cs | pending |

### Phase 3: Polish & Settings

| Task ID | Description | Requirement | Dependencies | Planned Files | Status |
|---------|-------------|-------------|--------------|---------------|--------|
| T-015 | Implement SettingsService (JSON persistence) | REQ-VC-010 | T-003 | Services/SettingsService.cs, tests/SettingsServiceTests.cs | pending |
| T-016 | Implement SettingsWindow (WPF dialog) | REQ-VC-010 | T-015 | Views/SettingsWindow.xaml, Views/SettingsWindow.xaml.cs | pending |
| T-017 | Implement StartupService (Run on startup) | REQ-VC-010 | T-015 | Services/StartupService.cs, tests/StartupServiceTests.cs | pending |
| T-018 | Implement icon state transitions and partial results indicator | REQ-VC-002, REQ-VC-003, REQ-VC-009 | T-007, T-009 | Tray/TrayIconManager.cs, Views/PartialResultsIndicator.xaml | pending |

### Phase 4: Integration & App Entry

| Task ID | Description | Requirement | Dependencies | Planned Files | Status |
|---------|-------------|-------------|--------------|---------------|--------|
| T-019 | Implement App.xaml.cs (entry point, mutex, service wiring) | REQ-VC-001, REQ-VC-007, REQ-VC-008 | T-007, T-008, T-009, T-015 | App.xaml, App.xaml.cs, MainWindow.xaml.cs | pending |
| T-020 | Implement speech availability check and error flow | REQ-VC-008 | T-009, T-019 | Services/SpeechRecognitionService.cs | pending |

### Phase 5: Packaging

| Task ID | Description | Requirement | Dependencies | Planned Files | Status |
|---------|-------------|-------------|--------------|---------------|--------|
| T-PKG | Publish profile and README | AC-001 | T-019, T-020 | Properties/PublishProfiles/single-file.pubxml, README.md | pending |
