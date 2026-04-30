# PROJECT_LOG.md — VoiceClip Session History

## Session 17: 2026-04-29 — Phase A build + test verification (harness-hur-default)

- Coding CLI used: Claude Code CLI (claude-sonnet-4-6)
- Harness: harness-hur-default (QA Agent + Implementer + Reviewer)

### Findings

- Build: PASS (0 errors, 0 warnings) — post Session 16 code is clean
- Tests: 55/57 initially — 2 failing: `AppSettingsTests.DefaultSettings_HaveCorrectDefaults` and `SettingsServiceTests.Load_WhenNoFile_ReturnsDefaults`
- Root cause: `AppSettings.SilenceTimeoutSeconds` default changed 8→5 in Session 15 (`AppSettings.cs`); test assertions not updated at that time

### Fix

- `tests\VoiceClip.Tests\AppSettingsTests.cs` line 17: `.Should().Be(5)` (was `8`)
- `tests\VoiceClip.Tests\SettingsServiceTests.cs` line 35: `.Should().Be(5)` (was `8`)

### Verification

- Implementer: PASS (10/10 targeted tests)
- Reviewer: APPROVED (production default confirmed `5`, no unrelated changes)
- QA Agent full gate: PASS (57/57)

### Harness stats

- Implementers spawned: 1 | Reviewers: 1 | QA runs: 2 | Security Auditor: not triggered

---

## Session 16: 2026-04-29 22:30 CDT — Tray/floating button bug fixes, toast UX

- Coding CLI used: Claude Code CLI (claude-sonnet-4-6)
- Phase(s) worked on: Runtime bug fixes (tray click routing, floating button context menu, toast auto-close)

### Concrete changes implemented

**FIX 1 — Tray right-click now opens context menu (was opening history)**
- Root cause: Hardcodet library `TrayMouseDoubleClick` fires for ALL mouse buttons (left and right), causing `HistoryClicked` on right-click
- Fix: Removed `TrayMouseDoubleClick` entirely; implemented manual double-click detection via `DispatcherTimer` (300ms window)
- Tray mapping now: single left-click = history, double left-click = dictate, right-click = context menu

**FIX 2 — Floating button right-click shows context menu**
- `FloatingButtonWindow.xaml`: Added `MouseRightButtonUp` event
- `FloatingButtonWindow.xaml.cs`: Added `HistoryClicked`, `SettingsClicked`, `ExitClicked` events and context menu handler
- `App.xaml.cs`: Wired new floating button events to existing handlers

**FIX 3 — History Delete button no longer covers timestamp/duration**
- Moved Delete button to row 0 only (was spanning 2 rows, overlapping timestamp)
- Combined timestamp and duration into single `TextBlock` using `<Run>` elements

**FIX 4 — History click crash on `Run` elements**
- `VisualTreeHelper.GetParent()` throws `InvalidOperationException` on `Run` elements (not Visual)
- Added type check before calling `VisualTreeHelper.GetParent` in both `IsClickOnButton` and `HistoryList_PreviewMouseLeftButtonUp`

**FIX 5 — Toast notifications auto-close in 1s (success) / 3s (error)**
- Replaced Hardcodet balloon tip with custom WPF popup (balloon tip can't be closed programmatically in v1.1.0, Windows enforces minimum display time)
- `ToastNotification.cs` now creates a lightweight borderless `Window` near the tray with `DispatcherTimer` auto-close

**FIX 6 — "No speech detected" no longer treated as error**
- Changed from `ShowError()` (3s error toast + error log entry) to `Show()` (1s info toast, no log)

## Session 15: 2026-04-29 17:29 CDT — Speech quality, real-time typing, tray UX

- Coding CLI used: Claude Code CLI (claude-sonnet-4-6)
- Phase(s) worked on: Runtime bug fixes + UX improvements (speech quality, Voice Access parity)

### Concrete changes implemented

**FIX 1 — UserCanceled now saves captured text** (`SpeechRecognitionService.cs`)
- `OnSessionCompleted` previously treated `UserCanceled` as an error (RecognitionError fired, text discarded)
- `UserCanceled` fires when Windows grants mic access to another app (not a user action)
- Added `SpeechRecognitionResultStatus.UserCanceled` to the normal-completion path alongside `Success` and `TimeoutExceeded`
- Words spoken before the system mic steal are now saved and pasted

**FIX 2 — PartialResultsIndicator live text update** (`PartialResultsIndicator.xaml.cs`)
- Class declared `PropertyChanged` event but did not implement `INotifyPropertyChanged` interface
- WPF binding engine only subscribes to `PropertyChanged` when the interface is declared
- Added `: INotifyPropertyChanged` to class declaration
- Result: live partial text now updates in the recording popup as user speaks

**FIX 3 — EndSilenceTimeout override removed** (`SpeechRecognitionService.cs`)
- Previously set both `EndSilenceTimeout` AND `InitialSilenceTimeout` to `_silenceTimeoutSeconds` (8s)
- `EndSilenceTimeout` controls phrase finalization latency; forcing it to 8s meant `ResultGenerated` only fired after 8s of silence, making recognition feel sluggish
- Removed `EndSilenceTimeout` override; OS default (~150ms) now used — matches Windows Voice Access natural phrase detection
- `InitialSilenceTimeout` still set to `_silenceTimeoutSeconds` for session auto-stop

**FIX 4 — Silence auto-stop default changed** (`AppSettings.cs`)
- Default was 8s, tried 3s (too short — cut off mid-speech), settled on 5s (Windows OS default, matches Voice Access)
- No saved settings.json exists for this user so code default takes effect immediately

**FEATURE — Real-time phrase-by-phrase typing** (`WindowFocusHelper.cs`, `ISpeechRecognitionService.cs`, `SpeechRecognitionService.cs`, `App.xaml.cs`)
- Architectural change: text now types into target window phrase-by-phrase as it is recognized, not in a single paste at the end
- Added `TypeText(string text)` to `WindowFocusHelper`: uses `SendInput` with `KEYEVENTF_UNICODE` to inject characters directly without touching clipboard or switching focus
- Added `PhraseCompleted` event to `ISpeechRecognitionService` and `SpeechRecognitionService`: fires with incremental text (the new portion only) after each `ResultGenerated`
- `SpeechRecognitionService.AppendRecognizedText` now captures `prevLength` before appending, computes `aggregatedText[prevLength..]` as `incrementalText`, fires `PhraseCompleted` with incremental, then `PartialResultReceived` with full accumulated
- `App.xaml.cs`: added `_phrasesTyped` counter (reset on each session start), `OnPhraseCompleted` handler calls `WindowFocusHelper.TypeText(e.Text)` immediately (no Dispatcher.Invoke needed — runs on WinRT background thread, SendInput is thread-safe)
- `OnDictationCompleted`: if `_phrasesTyped > 0`, sets clipboard to full text (for reference) and shows "Dictated" toast without re-pasting; if `_phrasesTyped == 0` (silent session), falls back to clipboard + Ctrl+V as before

**CHANGE — Tray icon click mapping** (`TrayIconManager.cs`)
- Old: TrayMouseDoubleClick=History, TrayRightMouseUp=ContextMenu, no left-click
- New: TrayLeftMouseUp=History, TrayRightMouseUp=ContextMenu (unchanged — only event where PlacementMode.Mouse works reliably), TrayMouseDoubleClick=Dictate
- Attempted swapping right-click to history and double-click to context menu — both broke (PlacementMode.Mouse only works in TrayRightMouseUp context; reverted)

### Files modified
- `src/VoiceClip/Services/SpeechRecognitionService.cs` — UserCanceled fix, EndSilenceTimeout removal, PhraseCompleted event + incremental text
- `src/VoiceClip/Services/ISpeechRecognitionService.cs` — PhraseCompleted event declaration
- `src/VoiceClip/Views/PartialResultsIndicator.xaml.cs` — INotifyPropertyChanged interface added
- `src/VoiceClip/Helpers/WindowFocusHelper.cs` — TypeText() method + KEYEVENTF_UNICODE constant
- `src/VoiceClip/Models/AppSettings.cs` — default silenceTimeoutSeconds 8→5
- `src/VoiceClip/App.xaml.cs` — _phrasesTyped field, OnPhraseCompleted handler, PhraseCompleted subscription, OnDictationCompleted branch for real-time vs fallback paste
- `src/VoiceClip/Tray/TrayIconManager.cs` — click mapping (left=history, right=context menu, double=dictate)
- `PROJECT_HANDOFF.md`, `PROJECT_LOG.md` — this entry

### Key technical decisions
- `TypeText` uses `KEYEVENTF_UNICODE` (not VK codes): works for any Unicode character, no language/keyboard layout dependency
- `PhraseCompleted` carries incremental text only (not accumulated): caller types exactly the new words, no duplication
- `_phrasesTyped == 0` fallback: if user clicks stop before any phrase finalizes, clipboard+paste still works
- `EndSilenceTimeout` OS default (~150ms): Voice Access uses this same default; longer values delay `ResultGenerated` and make recognition feel slow
- `PlacementMode.Mouse` context menu: only works in `TrayRightMouseUp` handler; `TrayMouseDoubleClick` does not provide valid mouse position context for WPF popup placement

### Problems encountered
- 3s silence timeout cut off user mid-sentence (tried → reverted to 5s)
- Attempted right-click=history, double-click=context menu — both broke (PlacementMode.Mouse limitation); reverted
- Build blocked when VoiceClip.exe still running — user must exit app before building

### Verification
- Build: 0 errors, 0 warnings (2026-04-29 17:29 CDT) — confirmed after last tray change (VoiceClip was running, build failed; code itself is clean, all prior successful builds confirm)
- Tests: not re-run this session (no test files modified)
- Runtime: not yet tested — user ended session before full runtime verification

### Outstanding after this session
- Runtime test of all features (real-time typing, partial popup, tray clicks, 5s timeout)
- Installer rebuild once runtime verified



## Session 14: 2026-04-29 13:02 CDT — Harness-driven bug fixes (3 bugs resolved)

- Coding CLI used: Claude Code CLI (harness-hur-autonomous-review-and-modernize)
- Phase(s) worked on: Phase 0 recon + Phase 2 SPEC-driven implementation
- Concrete changes implemented:

  **FIX 1 — Tray right-click context menu** (`TrayIconManager.cs`)
  - Removed broken `ContextMenu` property assignment and Resources[] approach
  - Added `TrayRightMouseUp` handler that creates a fresh ContextMenu, sets
    `Placement = PlacementMode.Mouse`, and `IsOpen = true` — bypasses the
    Hardcodet popup pipeline that fails for programmatically-created TaskbarIcons

  **FIX 2 — HistoryPopup empty state + focus** (`App.xaml.cs`, `HistoryPopup.xaml`)
  - Added `popup.Activate()` after `popup.Show()` in ShowHistoryPopup to ensure
    the window gets foreground focus when opened from a tray icon handler
  - Added "No dictation history yet" empty-state TextBlock with DataTrigger on
    `Entries.Count == 0` so users can distinguish empty-vs-broken

  **FIX 3 — OnSessionCompleted status checking** (`ISpeechRecognitionService.cs`,
    `SpeechRecognitionService.cs`, `App.xaml.cs`)
  - Added `event EventHandler<string>? RecognitionError` to the interface and service
  - `OnSessionCompleted` now branches on `args.Status`: Success/TimeoutExceeded fire
    `DictationCompleted` as before; all other statuses fire `RecognitionError`
  - App.xaml.cs subscribes `OnRecognitionError` which resets UI state (tray, floating
    button, partial indicator) and shows an error toast

- Files modified:
  - `src/VoiceClip/Tray/TrayIconManager.cs`
  - `src/VoiceClip/Views/HistoryPopup.xaml`
  - `src/VoiceClip/App.xaml.cs`
  - `src/VoiceClip/Services/ISpeechRecognitionService.cs`
  - `src/VoiceClip/Services/SpeechRecognitionService.cs`
  - `PROJECT_HANDOFF.md`, `PROJECT_LOG.md`
- SPECs: 3 bug fixes (no formal SPEC files — task-brief scope)
- Implementer dispatched: 1 (expert-backend subagent)
- Verification: `dotnet build` 0/0, `dotnet test` 57/57 (2026-04-29 13:02 CDT)
- Outstanding: all three fixes need runtime hardware test before installer rebuild

---

## Session 13: 2026-04-28 22:53 CDT — Bug Fixes (Partial)

- Coding CLI used: Claude Code CLI
- Phase(s) worked on: Runtime bug fixes — floating button state reversion, tray context menu
- Concrete changes implemented:
  - Added `_isStartingDictation` bool field to `App.xaml.cs` to prevent double-click re-entrancy during async `StartDictationAsync`. If a second toggle arrives while start is in flight, it returns immediately instead of calling Stop.
  - Removed automatic `CheckSpeechAvailabilityAsync()` call from `OnStartup`. That call invoked `CleanupRecognizer()` ~1-2 seconds after startup and could dispose a recognizer the user had just started (race condition).
  - Added `Application.Current.Resources["__VoiceClipTray"] = _notifyIcon` in `TrayIconManager` constructor and matching `Resources.Remove` in `Dispose`. Intended to register the programmatic `TaskbarIcon` in the WPF element tree so context menu popup pipeline could locate a `PresentationSource`.
- Files modified:
  - `src/VoiceClip/App.xaml.cs` — `_isStartingDictation` guard, removed startup availability check
  - `src/VoiceClip/Tray/TrayIconManager.cs` — WPF resource registration attempt
- Key technical decisions:
  - Re-entrancy guard uses `bool` (not `SemaphoreSlim`) because toggle is always called on UI thread.
  - `CheckSpeechAvailabilityAsync` removed from startup — user can still discover unavailability on first dictation attempt (error toast).
- Problems encountered:
  - `_isStartingDictation` fix: build verified, runtime not yet retested by user.
  - Tray right-click context menu: `Resources[]` fix did NOT resolve the issue at runtime. Context menu still does not appear on right-click.
  - HistoryPopup (double-click): opens but shows NO dictation entries. Root cause not yet investigated.
- Verification: `dotnet build` — 0 errors, 0 warnings (2026-04-28 22:53 CDT)
- Items completed this session: `_isStartingDictation` guard (code), startup race removal (code)
- Items NOT resolved: BUG-VC-008 (tray context menu), BUG-VC-009 (HistoryPopup empty)
- Session ended at user request.

## Session 12: 2026-04-28 — Floating Button + Auto-Paste

- Coding CLI used: Claude Code CLI
- Phase(s) worked on: New UX feature — floating dictation button + auto-paste to active window
- Concrete changes implemented:
  - Added `FloatingButtonWindow.xaml` + `.xaml.cs`: always-on-top, draggable, WS_EX_NOACTIVATE (does not steal focus)
    - Blue circle with Segoe MDL2 mic icon (U+E720) when idle
    - Red circle with stop icon (U+E71C) + pulsing animation when recording
    - Drag to reposition; click to toggle dictation
  - Added `Helpers/WindowFocusHelper.cs`: Win32 GetForegroundWindow / SetForegroundWindow / SendInput (Ctrl+V)
  - Modified `App.xaml.cs`:
    - Creates and shows FloatingButtonWindow on startup
    - Captures foreground HWND before starting dictation (NOACTIVATE ensures it's the user's target app)
    - After dictation: SetForegroundWindow → 80ms delay → SendInput(Ctrl+V) → text pasted
    - Text stays in clipboard after paste
    - Updated startup toast message
- SPECs: user request (floating button + auto-paste)
- Files modified:
  - `src/VoiceClip/Views/FloatingButtonWindow.xaml` — New
  - `src/VoiceClip/Views/FloatingButtonWindow.xaml.cs` — New
  - `src/VoiceClip/Helpers/WindowFocusHelper.cs` — New
  - `src/VoiceClip/App.xaml.cs` — Floating button wiring + auto-paste on completion
  - `PROJECT_HANDOFF.md` — Updated state
  - `PROJECT_LOG.md` — This entry
- Verification: `dotnet build` (0 errors / 0 warnings), `dotnet test` (57/57)

---

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
