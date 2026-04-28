# PROJECT_LOG.md — VoiceClip Session History

## Session 8: 2026-04-28 — UX Improvements + Runtime Verification

- Coding CLI used: Claude Code CLI + external agent
- Phase(s) worked on: Speech recognition fix, UX improvements, runtime verification
- Concrete changes implemented:
  - Fixed text segmentation: spaces between segments, punctuation-aware joining
  - Added click-to-copy on history entries (click any item to copy)
  - Added per-item delete buttons in history list
  - Moved clipboard error handling to ViewModel (COMException, SecurityException)
  - Added settings clone/edit pattern so Cancel truly discards changes
  - Added restart notification when language/timeout/history settings change
  - Added 4 new tests (57 total, up from 53)
- Files modified:
  - `src/VoiceClip/Services/SpeechRecognitionService.cs` — Text segmentation, diagnostic logging
  - `src/VoiceClip/App.xaml.cs` — ViewModel event wiring for copy/fail
  - `src/VoiceClip/ViewModels/HistoryViewModel.cs` — EntryCopyFailed event, clipboard error handling
  - `src/VoiceClip/Views/HistoryPopup.xaml` — Click handler, per-item delete buttons
  - `src/VoiceClip/Views/HistoryPopup.xaml.cs` — HistoryList_PreviewMouseLeftButtonUp
  - `src/VoiceClip/Models/AppSettings.cs` — Clone(), CopyFrom() methods
  - `src/VoiceClip/Views/SettingsWindow.xaml` — Minor layout
  - `src/VoiceClip/Views/SettingsWindow.xaml.cs` — Clone/edit, restart notification
  - `src/VoiceClip/Services/SettingsService.cs` — Minor update
  - `tests/VoiceClip.Tests/` — 4 new tests across 3 test files
- Verification: `dotnet build` (0/0), `dotnet test` (57/57)
- Runtime: Dictation verified working on dev machine + target machine

---

## Session 7: 2026-04-28 — Deployment Fixes

- Fixed WPF trimming crash (disabled trimming, 187MB exe)
- Fixed installer only packaging VoiceClip.exe (changed to include all publish files)
- Fixed InitialSilenceTimeout (default ~5s was auto-stopping session)
- Added no-speech feedback toast
- Added speech privacy policy detection in installer
- Added speech privacy policy guidance in app error handling
- Diagnostic logging added to SpeechRecognitionService

---

## Sessions 1-6: 2026-04-28 — Initial Development + Code Reviews

- Implemented all SPEC-VOICECLIP-001 requirements (phases 1-5)
- 5 code review passes, 17 bugs fixed total
- Icon generation, installer script, error handling
- All 53 tests passing at end of session 6
