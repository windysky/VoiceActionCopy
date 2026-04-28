# PROJECT_HANDOFF.md — VoiceClip

## 1. Project Overview

VoiceClip is a Windows 11 system tray app that captures voice dictation into a persistent clipboard buffer using `Windows.Media.SpeechRecognition` (same engine as Voice Access). Independent of Voice Access — both can coexist.

- Last updated: 2026-04-28
- Last coding CLI used: Claude Code CLI

## 2. Current State

| Component | Status | Notes |
|-----------|--------|-------|
| Core app (tray, hotkeys, speech, history) | Completed | Working on both dev and target machines |
| Spec compliance (all REQ-VC-001 through REQ-VC-010) | Completed | |
| Icon generation tool + generated icons | Completed | |
| Self-contained publish (187MB, no trimming) | Completed | Trimming disabled — trimmed exe crashes at runtime |
| Inno Setup installer script | Completed | Speech privacy check, all files included |
| Runtime dictation verified on target machine | Completed | Requires speech privacy acceptance + microphone access |
| Code review (5 passes, 17 bugs fixed) | Completed | |
| UX improvements (click-to-copy, settings clone, restart notice) | Completed | |

- **Build**: 0 errors, 0 warnings
- **Tests**: 57/57 passing
- **SPEC**: SPEC-VOICECLIP-001 — Status: Implemented
- **Published exe**: 187MB (self-contained, no trimming)
- **Branch**: main

## 3. Execution Plan Status

| Phase | Status |
|-------|--------|
| SPEC-VOICECLIP-001 Phase 1: Core Infrastructure | Completed |
| SPEC-VOICECLIP-001 Phase 2: Speech Recognition | Completed |
| SPEC-VOICECLIP-001 Phase 3: History & Clipboard | Completed |
| SPEC-VOICECLIP-001 Phase 4: Polish & Settings | Completed |
| SPEC-VOICECLIP-001 Phase 5: Testing & Packaging | Completed |
| Icon generation + deployment | Completed |
| Trimming disabled (runtime crash fix) | Completed |
| Code review passes 1-5 (17 bugs fixed) | Completed |
| Installer speech privacy guidance | Completed |
| UX improvements (text segmentation, click-to-copy, settings clone) | Completed |
| Runtime dictation verified | Completed |

## 4. Outstanding Work

None. All core functionality working.

## 5. Risks and Known Limitations

| Item | Status | Notes |
|------|--------|-------|
| WPF trimming disabled (187MB exe) | Accepted | Trimmed exe crashes at runtime; untrimmed is reliable |
| Language/silence timeout changes require restart | By design | Settings shows restart notification |
| Speech requires Windows Online Speech Recognition enabled | By design | Installer guides user to settings page |
| Debug logging enabled in SpeechRecognitionService | Cleanup needed | `debug.log` written to `%APPDATA%\VoiceClip\` on each session |

## 6. Verification Status

| Item | Result |
|------|--------|
| Build | 0 errors, 0 warnings |
| Unit tests | 57/57 pass |
| Runtime dictation | Verified on dev + target machines |
| Tray icon + hotkeys | Working |
| History popup (click-to-copy, delete, search, clear) | Working |
| Settings (clone/edit, restart notice) | Working |
| Installer (speech privacy check, all files) | Working |
| Uninstaller (cleans %APPDATA%\VoiceClip) | Working |

## 7. Restart Instructions

The project is feature-complete. For the next session:

1. Remove debug logging from `SpeechRecognitionService.cs` (the `LogDebug` calls)
2. Remove `%APPDATA%\VoiceClip\debug.log` from target machines
3. Rebuild installer if any changes are made
4. `git push` to sync to origin

## 8. Tech Stack

- .NET 8 (net8.0-windows10.0.22621.0)
- WPF (WinExe, hidden MainWindow for HWND/message pump)
- Windows.Media.SpeechRecognition (WinRT, continuous dictation)
- Hardcodet.NotifyIcon.Wpf (system tray)
- System.Drawing.Common (icon loading)
- Inno Setup 6 (installer)
- xUnit + FluentAssertions + Moq (tests)
