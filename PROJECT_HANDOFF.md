# PROJECT_HANDOFF.md — VoiceClip

## 1. Project Overview

VoiceClip is a Windows 11 system tray app that captures voice dictation into a persistent clipboard buffer using `Windows.Media.SpeechRecognition` (same engine as Voice Access). Independent of Voice Access — both can coexist.

- Last updated: 2026-04-29 22:30 CDT
- Last coding CLI used: Claude Code CLI (claude-sonnet-4-6)

## 2. Current State

| Component | Status | Notes |
|-----------|--------|-------|
| Core app (tray, hotkeys, speech, history) | Completed | Working |
| Spec compliance (REQ-VC-001 through REQ-VC-010) | Completed | |
| Icon generation + generated icons | Completed | |
| Self-contained publish (187MB, no trimming) | Completed | Trimming disabled — trimmed exe crashes |
| Inno Setup installer script | Completed | Speech privacy check, all files included |
| Runtime dictation verified on target machine | Completed | Requires speech privacy acceptance |
| Code review (7 passes, 17+ bugs fixed) | Completed | |
| UX improvements (click-to-copy, settings clone, restart notice) | Completed | |
| Floating button (always-on-top, draggable, NOACTIVATE) | Completed | |
| `_isStartingDictation` re-entrancy guard | Completed 2026-04-28 22:53 CDT | |
| Removed startup `CheckSpeechAvailabilityAsync()` race | Completed 2026-04-28 22:53 CDT | |
| Tray right-click context menu | Completed 2026-04-29 13:02 CDT | Via TrayRightMouseUp + PlacementMode.Mouse |
| HistoryPopup force-focus + empty-state message | Completed 2026-04-29 13:02 CDT | popup.Activate(); DataTrigger on Count==0 |
| OnSessionCompleted status check + RecognitionError event | Completed 2026-04-29 13:02 CDT | Non-success statuses fire RecognitionError |
| UserCanceled treated as normal completion | Completed 2026-04-29 17:29 CDT | Words spoken before system mic steal are now saved |
| INotifyPropertyChanged on PartialResultsIndicator | Completed 2026-04-29 17:29 CDT | WPF binding was never receiving updates — fixed |
| Real-time phrase-by-phrase typing (Voice Access style) | Completed 2026-04-29 17:29 CDT | TypeText() via KEYEVENTF_UNICODE; no clipboard mid-session |
| EndSilenceTimeout override removed | Completed 2026-04-29 17:29 CDT | Now uses OS default ~150ms for natural phrase finalization |
| Silence auto-stop default changed to 5s | Completed 2026-04-29 17:29 CDT | Was 8s; matches Windows Voice Access OS default |
| Tray click mapping revised | Completed 2026-04-29 22:30 CDT | Left=history, Right=context menu, Double=dictate; manual double-click detection via DispatcherTimer |
| Floating button right-click context menu | Completed 2026-04-29 22:30 CDT | Same menu as tray: Dictate/History/Settings/Exit |
| Toast auto-close (1s success / 3s error) | Completed 2026-04-29 22:30 CDT | Custom WPF popup replaces Hardcodet balloon tip |
| History Delete button layout fix | Completed 2026-04-29 22:30 CDT | Button row 0 only; timestamp+duration merged into one line |
| History Run element crash fix | Completed 2026-04-29 22:30 CDT | VisualTreeHelper.GetParent guarded for non-Visual elements |
| "No speech detected" downgraded from error to info | Completed 2026-04-29 22:30 CDT | 1s info toast, no error log entry |

- **Build**: 0 errors, 0 warnings (verified 2026-04-29 22:30 CDT — post Session 16)
- **Tests**: 57/57 passing (verified 2026-04-29 22:30 CDT — 2 stale assertions fixed: SilenceTimeoutSeconds 8→5)
- **SPEC**: SPEC-VOICECLIP-001 — Status: Implemented
- **Published exe**: 187MB (self-contained, no trimming) — needs rebuild after all session 15 changes
- **Branch**: main

## 3. Execution Plan Status

| Phase | Status | Last Updated |
|-------|--------|-------------|
| All SPEC-VOICECLIP-001 phases | Completed | 2026-04-28 |
| Icon generation + deployment | Completed | 2026-04-28 |
| Trimming disabled (runtime crash fix) | Completed | 2026-04-28 |
| Code review passes 1-7 | Completed | 2026-04-28 |
| Floating button + auto-paste | Completed | 2026-04-28 |
| Bug fix: button resets to idle immediately | Completed | 2026-04-28 22:53 CDT |
| Bug fix: tray right-click context menu | Completed | 2026-04-29 13:02 CDT |
| Bug fix: HistoryPopup focus + empty state | Completed | 2026-04-29 13:02 CDT |
| Bug fix: RecognitionError event for failed sessions | Completed | 2026-04-29 13:02 CDT |
| Session 15: Speech quality + real-time typing | Completed (code) | 2026-04-29 17:29 CDT |

## 4. Outstanding Work

| Item | Priority | Status | Notes |
|------|----------|--------|-------|
| Feature: Mic device picker in Settings | Medium | Not started | Let users select recording device instead of using system default; requires research into Windows.Media.SpeechRecognition device selection API |
| Runtime test: real-time typing works phrase-by-phrase | High | Needs hardware test | TypeText() via SendInput KEYEVENTF_UNICODE; unverified at runtime |
| Runtime test: partial text shows live in recording popup | High | Needs hardware test | INotifyPropertyChanged fix applied; unverified |
| Runtime test: 5s silence auto-stop feels correct | Medium | Needs hardware test | Was cutting off at 3s; now 5s |
| Runtime test: tray left-click shows history popup | Medium | Needs hardware test | New mapping; unverified |
| Runtime test: tray double-click toggles dictation | Medium | Needs hardware test | New mapping; unverified |
| Rebuild installer (publish + Inno Setup) | Medium | Not started | Code changed significantly since last publish |

## 5. Risks and Known Limitations

| Item | Status | Notes |
|------|--------|-------|
| WPF trimming disabled (187MB exe) | Accepted | Trimmed exe crashes at runtime |
| Language/silence timeout changes require restart | By design | Settings shows restart notification |
| Speech requires Windows Online Speech Recognition enabled | By design | Installer guides user to settings page |
| TypeText() blocked by elevated target windows | Open | SendInput is rejected when target process runs as Admin; falls back to clipboard paste |
| 5s silence timeout may still cut off slow speakers | Open | Configurable in Settings → Silence Timeout (range 3–60s) |

## 6. Verification Status

| Item | Result | Verified |
|------|--------|---------|
| Build (0 errors, 0 warnings) | Pass | 2026-04-29 17:29 CDT |
| Unit tests (57/57) | Pass | 2026-04-29 13:02 CDT |
| Runtime dictation (basic) | Verified working | 2026-04-29 (user spoke words, UserCanceled was root cause, now fixed) |
| UserCanceled now saves text | Code correct | Pending runtime re-test |
| Real-time phrase typing | Code correct | Pending runtime test |
| Partial text indicator live update | Code correct | Pending runtime test |
| Tray left-click = history | Code correct | Pending runtime test |
| Tray double-click = dictate | Code correct | Pending runtime test |
| Tray right-click = context menu | Code correct | Pending runtime test |

## 7. Restart Instructions

**Close any running VoiceClip.exe first** (right-click tray → Exit, or Task Manager).

### Build command (Windows Command Prompt — NOT Git Bash)
```
"C:\Program Files\dotnet\dotnet.exe" build "C:\Users\juhur\OneDrive\UND\VoiceActionCopy\src\VoiceClip\VoiceClip.csproj" --configuration Debug
```

### Runtime test sequence
1. Launch `src\VoiceClip\bin\Debug\net8.0-windows10.0.22621.0\VoiceClip.exe`
2. Open a text editor (Notepad) and click inside it
3. Click the **floating blue button** → should turn red, recording popup should appear showing "Listening… speak now"
4. Speak a sentence → words should appear **live in Notepad as you speak** (phrase by phrase, ~150ms after each natural pause) AND in the recording popup
5. Click the **floating red button** to stop → toast "Dictated" should appear; full text should be in clipboard
6. **Left-click** the tray icon → history popup should open showing the entry
7. **Right-click** the tray icon → context menu should appear (Dictate / History / Settings / Exit)
8. **Double-click** the tray icon → should toggle dictation

### Key architecture changes in session 15
- `PhraseCompleted` event fires for each finalized phrase (incremental text only)
- `App.xaml.cs` handles `PhraseCompleted` → calls `WindowFocusHelper.TypeText()` immediately
- `TypeText()` uses `SendInput` with `KEYEVENTF_UNICODE` — no clipboard, no focus switch needed
- At session end: clipboard = full accumulated text (for re-paste reference); no Ctrl+V sent if `_phrasesTyped > 0`
- `EndSilenceTimeout` no longer overridden — OS default ~150ms used for natural phrase detection
- `InitialSilenceTimeout` = 5 seconds (auto-stop after silence)

### Self-contained publish (for installer rebuild)
```
"C:\Program Files\dotnet\dotnet.exe" publish src/VoiceClip/VoiceClip.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=false
```

- Last updated: 2026-04-29 17:29 CDT

## 8. Tech Stack

- .NET 8 (net8.0-windows10.0.22621.0)
- WPF (WinExe, hidden MainWindow for HWND/message pump)
- Windows.Media.SpeechRecognition (WinRT, continuous dictation)
- Hardcodet.NotifyIcon.Wpf (system tray)
- System.Drawing.Common (icon loading)
- Inno Setup 6 (installer)
- xUnit + FluentAssertions + Moq (tests)
