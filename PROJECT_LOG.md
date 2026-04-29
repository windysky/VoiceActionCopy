# PROJECT_LOG.md — VoiceClip Session History

## Session 11: 2026-04-28 — Code Review Session 8

- Coding CLI used: Claude Code CLI
- Phase(s) worked on: Full code review (third consecutive pass)
- Issues found: 0
- Analysis performed:
  - Re-read all 20+ source files at current HEAD (1c73d8e)
  - Thread safety: verified `_stopRequested`/`_isRecording` guards, `CleanupRecognizer` concurrency paths
  - Security: no secrets, no injection vectors, file I/O scoped to %APPDATA%, registry scoped to HKCU\Run
  - File I/O: atomic save via File.Replace with temp files, orphaned tmp files self-heal on next write
  - Edge cases: multiple history popups (low, accepted), toast null-conditional inconsistency (style only)
  - By-design items: settings require restart (documented + user notification)
- Baseline: build 0/0, tests 57/57
- Conclusion: No actionable issues found. Codebase is clean after 7 prior SPEC fixes.

---

- Coding CLI used: Claude Code CLI
- Phase(s) worked on: Code review, bug fixes
- Concrete changes implemented:
  - Fixed IsAvailableAsync() leaking SpeechRecognizer (now cleans up after check)
  - Capped error.log at 5MB (rotates on overflow)
  - Fixed PartialResultsIndicator staying open on recording start failure
- SPECs: FIX-VC-005 (high), FIX-VC-006 (high), FIX-VC-007 (low)
- Files modified:
  - `src/VoiceClip/Services/SpeechRecognitionService.cs` — IsAvailableAsync cleanup
  - `src/VoiceClip/App.xaml.cs` — Error log rotation, indicator close on failure
  - `PROJECT_HANDOFF.md` — Updated issue list
  - `PROJECT_LOG.md` — This entry
- Verification: `dotnet build` (0/0), `dotnet test` (57/57)

---

## Session 9: 2026-04-28 — Code Review Session 6

- Coding CLI used: Claude Code CLI
- Phase(s) worked on: Code review, bug fixes
- Concrete changes implemented:
  - Removed debug logging from SpeechRecognitionService (LogDebug method + all calls)
  - Fixed recognizer not reusable after session completion (CleanupRecognizer resets state)
  - Fixed double DictationCompleted event (added _stopRequested guard + unsubscribing handlers)
  - Removed unused durationMs parameter from ToastNotification.Show()
  - Updated default silence timeout parameter in SpeechRecognitionService constructor (60→8)
- SPECs: FIX-VC-001 (medium), FIX-VC-002 (critical), FIX-VC-003 (critical), FIX-VC-004 (low)
- Files modified:
  - `src/VoiceClip/Services/SpeechRecognitionService.cs` — Debug logging removal, reusability fix, double-event fix
  - `src/VoiceClip/Helpers/ToastNotification.cs` — Removed unused durationMs parameter
  - `src/VoiceClip/App.xaml.cs` — Updated toast caller, startup notification parameter
  - `PROJECT_HANDOFF.md` — Updated issue status, restart instructions
  - `PROJECT_LOG.md` — This entry
- Verification: `dotnet build` (0/0), `dotnet test` (57/57)

---

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
