# PROJECT_HANDOFF.md — VoiceClip

## 1. Project Overview

VoiceClip is a Windows 11 system tray app that captures voice dictation into a persistent clipboard buffer using `Windows.Media.SpeechRecognition` (same engine as Voice Access). Independent of Voice Access — both can coexist.

- Last updated: 2026-04-30 Session 19 CDT
- Last coding CLI used: Claude Code CLI (claude-opus-4-7)
- Current version: 1.0.1

## 2. Current State

| Component | Status | Notes |
|-----------|--------|-------|
| Core app (tray, hotkeys, speech, history) | Completed | Working |
| Spec compliance (REQ-VC-001 through REQ-VC-010) | Completed | |
| Icon generation + generated icons | Completed | |
| Self-contained publish (173MB, no trimming) | Completed | Rebuilt Session 18; trimming disabled — trimmed exe crashes |
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
| Mic device picker in Settings | Completed Session 18 | IPolicyConfig COM; AudioDeviceHelper; AppSettings.MicrophoneDeviceId |
| Version bump to 1.0.1 | Completed Session 19 | csproj Version + AssemblyVersion + Authors=Junguk Hur |
| MIT LICENSE.txt + DISCLAIMER.txt | Completed Session 19 | Root LICENSE.txt; installer/DISCLAIMER.txt with privacy + AS IS terms |
| Installer license + disclaimer pages | Completed Session 19 | LicenseFile + InfoBeforeFile in VoiceClip.iss |
| Installer source path bug fix | Completed Session 19 | Was packaging stale ..\publish\ (151KB framework-dependent); now points to src/VoiceClip/bin/Release/.../publish/ (173MB self-contained) |
| Built installer VoiceClip-1.0.1-setup.exe | Completed Session 19 | 52MB at dist/ |

- **Build**: 0 errors, 0 warnings (verified Session 18)
- **Tests**: 63/63 passing (verified Session 18 — 6 new tests added for mic picker)
- **SPEC**: SPEC-VOICECLIP-001 Implemented; SPEC-VC-MIC-PICKER-001 Completed
- **Published exe**: 173MB (rebuilt Session 18, self-contained, no trimming)
- **Branch**: main (git clean)

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
| Session 15: Speech quality + real-time typing | Completed | Session 18 (committed) |
| Session 16: Tray UX + floating context menu + toast | Completed | Session 18 (committed) |
| Session 18: Mic picker + publish rebuild | Completed | Session 18 |

## 4. Outstanding Work

| Item | Priority | Status | Notes |
|------|----------|--------|-------|
| Installer 1.0.1 install verification on target machine | High | Needs manual test | Uninstall old version first; install dist/VoiceClip-1.0.1-setup.exe; verify license + disclaimer pages appear; confirm floating button + mic picker present after install |
| Runtime test: real-time typing works phrase-by-phrase | High | Needs hardware test | TypeText() via SendInput KEYEVENTF_UNICODE; unverified at runtime |
| Runtime test: partial text shows live in recording popup | High | Needs hardware test | INotifyPropertyChanged fix applied; unverified |
| Runtime test: tray right-click context menu | Medium | Needs hardware test | Automated test hit wrong icon; confirm visually |
| Runtime test: mic device picker works with real device | Medium | Needs hardware test | IPolicyConfig COM; verify Windows comm device changes and restores |
| Runtime test: UserCanceled saves text | Medium | Needs mic-steal test | Requires another app to grab microphone mid-session |

## 5. Risks and Known Limitations

| Item | Status | Notes |
|------|--------|-------|
| WPF trimming disabled (173MB exe) | Accepted | Trimmed exe crashes at runtime |
| Language/silence timeout changes require restart | By design | Settings shows restart notification |
| Speech requires Windows Online Speech Recognition enabled | By design | Installer guides user to settings page |
| TypeText() blocked by elevated target windows | Open | SendInput is rejected when target process runs as Admin; falls back to clipboard paste |
| 5s silence timeout may still cut off slow speakers | Open | Configurable in Settings → Silence Timeout (range 3–60s) |
| Mic picker: IPolicyConfig is undocumented COM API | Accepted | Stable since Vista, used by SoundSwitch/EarTrumpet. If unavailable, silently falls back to system default |
| Mic picker: app crash during dictation leaves Windows comm device changed | Accepted | Will be wrong device until user manually resets in Windows Sound Settings |

## 6. Verification Status

| Item | Result | Verified |
|------|--------|---------|
| Build (0 errors, 0 warnings) | Pass | Session 18 |
| Unit tests (63/63) | Pass | Session 18 |
| App launch (no crash) | Automated PASS | 2026-04-30 CDT |
| Floating button window visible | Automated PASS | 2026-04-30 CDT |
| Click button → recording popup | Automated PASS | 2026-04-30 CDT |
| Silence auto-stop (~5s) | Automated PASS | 2026-04-30 CDT |
| Floating button right-click → context menu | Automated PASS | 2026-04-30 CDT |
| Tray left-click = history (via hotkey) | Automated PASS | 2026-04-30 CDT |
| Tray double-click = dictate (via hotkey) | Automated PASS | 2026-04-30 CDT |
| Tray right-click = context menu | Needs visual confirm | Win11 tray icon not automatable |
| Runtime dictation (basic) | Verified working | 2026-04-29 (user spoke words, UserCanceled was root cause, now fixed) |
| UserCanceled now saves text | Code correct | Pending runtime re-test |
| Real-time phrase typing | Code correct | Pending runtime test |
| Partial text indicator live update | Code correct | Pending runtime test |
| HistoryPopup double-close crash | Fixed 2026-04-30 CDT | Closing event now guards Window_Deactivated |
| Mic device picker UI | Code correct | Pending runtime test |

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
9. Open **Settings** → "Microphone" row should show ComboBox with "(System Default)" + detected devices

### Inno Setup installer compile (ISCC at user-local install)
```
"C:\Users\juhur\AppData\Local\Programs\Inno Setup 6\ISCC.exe" "C:\Users\juhur\OneDrive\UND\VoiceActionCopy\installer\VoiceClip.iss"
```
The published exe is at:
`src\VoiceClip\bin\Release\net8.0-windows10.0.22621.0\win-x64\publish\VoiceClip.exe` (173MB)
Built installer ends up at `dist\VoiceClip-{Version}-setup.exe`.

WARNING: Do NOT recreate a top-level `publish/` folder at repo root. The .iss now sources directly from `src\VoiceClip\bin\Release\.../publish\`. A stale top-level `publish/` from older builds caused Sessions 15-18 features to silently NOT ship in 1.0.0 installer.

### Self-contained publish (if re-publish needed)
```
"C:\Program Files\dotnet\dotnet.exe" publish src/VoiceClip/VoiceClip.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=false
```

- Last updated: Session 18

## 8. Tech Stack

- .NET 8 (net8.0-windows10.0.22621.0)
- WPF (WinExe, hidden MainWindow for HWND/message pump)
- Windows.Media.SpeechRecognition (WinRT, continuous dictation)
- Hardcodet.NotifyIcon.Wpf (system tray)
- System.Drawing.Common (icon loading)
- Inno Setup 6 (installer)
- xUnit + FluentAssertions + Moq (tests)
