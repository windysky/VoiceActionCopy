# PROJECT_LOG.md — VoiceClip Session History

## Session 6: 2026-04-28 13:40 CDT — Code Review Pass 5

- Coding CLI used: Claude Code CLI
- Phase(s) worked on: Full codebase re-review, XAML binding analysis, shutdown lifecycle tracing
- Concrete changes implemented:
  - Added `_disposed` guard to TrayIconManager.SetState — prevents ObjectDisposedException during app shutdown with active recording
  - Added SecurityException catch to clipboard error handling — prevents generic "UI error" dialog in restricted environments
- Files/modules touched:
  - `src/VoiceClip/Tray/TrayIconManager.cs` — _disposed check in SetState
  - `src/VoiceClip/App.xaml.cs` — SecurityException catch for clipboard operations
- Items completed in this session:
  - 2 issues fixed (2 Medium)
  - No Critical or High issues found
  - Full XAML binding analysis confirmed correct (ComboBox+SelectedItem pattern works via ContentControl content model)
  - Shutdown lifecycle traced end-to-end: DictationCompleted event timing during app exit verified
- Verification performed: `dotnet build` (0/0), `dotnet test` (53/53)

---

## Session 5: 2026-04-28 13:35 CDT — Code Review Pass 4

- Coding CLI used: Claude Code CLI
- Phase(s) worked on: Deep code review (fresh pass), bug identification, fix implementation
- Concrete changes implemented:
  - Fixed memory leak in HistoryPopup: EntryCopied event handler was never unsubscribed, causing popup instances to be retained by the long-lived ViewModel
  - Fixed silent clipboard failure: COMException when clipboard is locked by another process now shows error toast instead of false "Copied to clipboard"
  - Removed dead _recordingStartTime field in App.xaml.cs (SpeechRecognitionService tracks its own)
- Files/modules touched:
  - `src/VoiceClip/App.xaml.cs` — Clipboard COMException handling, removed dead field
  - `src/VoiceClip/Views/HistoryPopup.xaml.cs` — Unsubscribe EntryCopied in Closed handler
- Items completed in this session:
  - 3 issues fixed (2 Medium, 1 Low)
  - No Critical or High issues found — codebase is stable
- Verification performed: `dotnet build` (0/0), `dotnet test` (53/53)

---

## Session 4: 2026-04-28 13:30 CDT — Code Review Pass 3

- Coding CLI used: Claude Code CLI
- Phase(s) worked on: Deep code review (full source read), bug identification, fix implementation
- Concrete changes implemented:
  - Fixed ReleaseMutex crash on second-instance launch (Critical)
  - Fixed potential deadlock on exit during active recording (High)
  - Added settings validation with Math.Clamp on SilenceTimeoutSeconds and MaxHistoryEntries
  - Added null/whitespace guard on Language setter
  - Changed history file writes to atomic (temp file + Replace/Move)
  - Marked _isRecording as volatile to prevent race condition in DictationCompleted
  - Removed dead code: DictationEntryItem.xaml and empty Controls directory
  - Fixed HistoryService.Search reentrant lock by inlining the empty-query path
- Files/modules touched:
  - `src/VoiceClip/App.xaml.cs` — Added _ownsMutex field, conditional ReleaseMutex
  - `src/VoiceClip/Services/SpeechRecognitionService.cs` — volatile _isRecording, Dispose timeout
  - `src/VoiceClip/Models/AppSettings.cs` — Math.Clamp validation on setters
  - `src/VoiceClip/Services/HistoryService.cs` — Atomic file writes, reentrant lock fix
  - `src/VoiceClip/Controls/DictationEntryItem.xaml` — Deleted (dead code)
- Key technical decisions:
  - Used `File.Replace` for existing files and `File.Move` for first write (Replace requires destination to exist)
  - Used `volatile` on `_isRecording` instead of full lock — sufficient for bool flag on x64
  - Added 2-second timeout to Dispose `.Wait()` instead of removing it entirely — balances cleanup vs deadlock risk
- Problems encountered:
  - Initial atomic write using `File.Replace` failed on first write (destination doesn't exist) — caught by test, fixed with exists-check
- Items completed in this session:
  - 7 issues identified and fixed (1 Critical, 1 High, 3 Medium, 2 Low)
- Verification performed: `dotnet build` (0/0), `dotnet test` (53/53)

---

## Session 3: 2026-04-28 13:17 CDT — End-of-Day Consolidation

- Coding CLI used: Claude Code CLI
- Phase(s) worked on: Code review (2 passes), bug fixes, installer setup, documentation
- Concrete changes implemented:
  - Added global exception handlers + error logging to App.xaml.cs
  - Added ms-settings:privacy-speech link when speech unavailable
  - Added SpeechRecognitionTopicConstraint for dictation quality
  - Added try/catch around SpeechRecognizer initialization
  - Fixed WinRTAsyncHelper.AsTask() usage in Dispose()
  - Fixed critical auto-stop data loss (OnSessionCompleted now saves text)
  - Added Clear All confirmation dialog
  - Added preview ellipsis (77 chars + "...")
  - Added startup registration failure warning
  - Created icon generator tool + generated 3 tray icons
  - Added icon Content items to csproj for build output
  - Configured trimmed self-contained publish (188MB → 53MB)
  - Created Inno Setup installer script
  - Updated .gitignore, README.md, SPEC status
- Files/modules touched:
  - `src/VoiceClip/App.xaml.cs` — Error handling, ms-settings, DictationCompleted handler
  - `src/VoiceClip/Services/SpeechRecognitionService.cs` — WinRTAsyncHelper, auto-stop save
  - `src/VoiceClip/VoiceClip.csproj` — Trimming config, icon Content
  - `src/VoiceClip/Models/DictationEntry.cs` — Preview ellipsis
  - `src/VoiceClip/Views/HistoryPopup.xaml` — Clear All button click handler
  - `src/VoiceClip/Views/HistoryPopup.xaml.cs` — ClearAllButton_Click confirmation
  - `src/VoiceClip/Views/SettingsWindow.xaml.cs` — Startup failure warning
  - `src/VoiceClip/Assets/*.ico` — Generated icons
  - `tools/*` — Icon generator
  - `installer/VoiceClip.iss` — Inno Setup script
  - `tests/VoiceClip.Tests/DictationEntryTests.cs` — Updated for ellipsis
  - `.gitignore`, `README.md`, `docs/SPEC-VOICECLIP-001.md`
- Key technical decisions:
  - `await` on WinRT types is safe (uses GetAwaiter, not AsTask). Only explicit `.AsTask()` calls need WinRTAsyncHelper.
  - Auto-stop text saving unified into DictationCompleted event pattern — single handler for both manual and auto stop.
  - Full trim mode with `_SuppressWpfTrimError=true` reduces exe from 188MB to 53MB.
- Problems encountered:
  - WPF officially doesn't support trimming — required internal `_SuppressWpfTrimError` flag
  - Inno Setup paths relative to .iss file, not project root — fixed all paths with `..\`
  - Inno Setup `filesandirs` is invalid type — fixed to `filesandordirs`
  - Inno Setup `{localappdata}` wrong — VoiceClip uses Roaming (`{userappdata}`)
- Items completed in this session:
  - All SPEC-VOICECLIP-001 requirements implemented
  - 6 bugs fixed across 2 code review passes (1 Critical, 2 High, 3 Medium)
  - Installer script created and debugged
  - Published exe trimmed to 53MB
- Verification performed: `dotnet build` (0/0), `dotnet test` (53/53), icon output verified, trimmed publish verified

---

## Session 2: 2026-04-28 — Second Pass Code Review

- Fixed auto-stop data loss (Critical), Clear All confirmation (High), preview ellipsis (Medium), startup warning (Medium)
- Commit: `7b06403`

---

## Session 1: 2026-04-28 — Initial Code Review & Installer Setup

- Icon generation, trimming config, installer script, error handling, SPEC compliance
- Fixed WinRT Dispose crash
- Commits: `5ce5fa0`, `8907adc`, `bcd8cbb`, `3a89975`, `34efc57`, `a1e2b0f`
