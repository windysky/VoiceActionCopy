# PROJECT_HANDOFF.md — VoiceClip

## 1. Project Overview

VoiceClip is a Windows 11 system tray app that captures voice dictation into a persistent clipboard buffer using `Windows.Media.SpeechRecognition` (same engine as Voice Access). Independent of Voice Access — both can coexist.

- Last updated: 2026-04-28 13:35 CDT
- Last coding CLI used: Claude Code CLI

## 2. Current State

| Component | Status | Notes |
|-----------|--------|-------|
| Core app (tray, hotkeys, speech, history) | Completed | Completed in Session 2026-04-28 |
| Spec compliance (all REQ-VC-001 through REQ-VC-010) | Completed | Completed in Session 2026-04-28 |
| Icon generation tool + generated icons | Completed | Completed in Session 2026-04-28 |
| Self-contained trimmed publish (53MB) | Completed | Completed in Session 2026-04-28 |
| Inno Setup installer script | Completed | Completed in Session 2026-04-28 |
| Code review (2 passes, 6 bugs fixed) | Completed | Completed in Session 2026-04-28 13:17 CDT |
| Code review pass 3 (6 additional fixes) | Completed | Completed in Session 2026-04-28 13:30 CDT |
| Code review pass 4 (3 additional fixes) | Completed | Completed in Session 2026-04-28 13:35 CDT |
| Runtime end-to-end test with microphone | Not started | Needs manual testing on target machine |

- **Build**: 0 errors, 0 warnings
- **Tests**: 53/53 passing
- **SPEC**: SPEC-VOICECLIP-001 — Status: Implemented
- **Published exe**: 53MB (self-contained + full trimming)
- **Branch**: main

## 3. Execution Plan Status

| Phase | Status | Last Updated |
|-------|--------|-------------|
| SPEC-VOICECLIP-001 Phase 1: Core Infrastructure | Completed | 2026-04-28 |
| SPEC-VOICECLIP-001 Phase 2: Speech Recognition | Completed | 2026-04-28 |
| SPEC-VOICECLIP-001 Phase 3: History & Clipboard | Completed | 2026-04-28 |
| SPEC-VOICECLIP-001 Phase 4: Polish & Settings | Completed | 2026-04-28 |
| SPEC-VOICECLIP-001 Phase 5: Testing & Packaging | Completed | 2026-04-28 |
| Icon generation + deployment fix | Completed | 2026-04-28 13:17 CDT |
| Trimming + installer setup | Completed | 2026-04-28 13:17 CDT |
| Code review pass 1 (WinRT Dispose fix) | Completed | 2026-04-28 13:17 CDT |
| Code review pass 2 (auto-stop, clear-all, preview, startup) | Completed | 2026-04-28 13:17 CDT |
| Code review pass 3 (mutex crash, deadlock, validation, atomic writes, race, dead code) | Completed | 2026-04-28 13:30 CDT |
| Code review pass 4 (event leak, clipboard error handling, dead field) | Completed | 2026-04-28 13:35 CDT |

## 4. Outstanding Work

None. All identified issues resolved across four code review passes (total: 15 bugs fixed).

## 5. Risks, Open Questions, and Assumptions

| Item | Status | Date Opened | Notes |
|------|--------|-------------|-------|
| WPF trimming with `_SuppressWpfTrimError` is unsupported by Microsoft | Mitigated | 2026-04-28 | VoiceClip is TrimmerRootAssembly; 53MB trimmed exe builds and tests pass. Runtime test needed. |
| Inno Setup not on PATH by default | Mitigated | 2026-04-28 | User installed Inno Setup 6 GUI. CLI needs fresh terminal for PATH. |
| Language setting requires app restart | Known limitation | 2026-04-28 | SpeechRecognitionService is initialized once. Not a bug — documented behavior. |

## 6. Code Review Pass 4 — Issues Found & Fixed

| SPEC | Severity | Issue | Fix |
|------|----------|-------|-----|
| CR4-001 | 🟡 Medium | Memory leak: HistoryPopup subscribes to `EntryCopied` but never unsubscribes | Unsubscribe in `Closed` event handler |
| CR4-002 | 🟡 Medium | Silent clipboard failure: `COMException` when clipboard is locked shows "Copied" toast | Catch COMException, show error toast |
| CR4-003 | 🟢 Low | Dead field `_recordingStartTime` in App.xaml.cs | Removed unused field and assignment |

## 7. Code Review Pass 3 — Issues Found & Fixed

| SPEC | Severity | Issue | Fix |
|------|----------|-------|-----|
| CR-001 | 🔴 Critical | `ReleaseMutex()` crash on second-instance attempt in `App.OnExit` | Track `_ownsMutex` field; only release if acquired |
| CR-002 | 🟠 High | Deadlock on exit during active recording — `.Wait()` blocks UI thread on WinRT STA | Add 2-second timeout to `.Wait()` in Dispose |
| CR-003 | 🟡 Medium | No validation on loaded settings — corrupt JSON values accepted | Add `Math.Clamp` and null-coalesce in setters |
| CR-004 | 🟡 Medium | Non-atomic history file writes — `File.WriteAllText` can corrupt on crash | Write to `.tmp` then `File.Replace` or `File.Move` |
| CR-005 | 🟡 Medium | Race condition in DictationCompleted — `_isRecording` not volatile | Mark `_isRecording` as `volatile` |
| CR-006 | 🟢 Low | Dead code: `DictationEntryItem.xaml` never referenced | Deleted file and empty Controls directory |
| L3 fix | 🟢 Low | `HistoryService.Search` reentrant lock | Inline the query to avoid calling GetAll inside lock |

## 8. Verification Status

| Item | Method | Result | Date/Time |
|------|--------|--------|-----------|
| Build | `dotnet build` | 0 errors, 0 warnings | 2026-04-28 13:35 CDT |
| Unit tests | `dotnet test` | 53/53 pass | 2026-04-28 13:35 CDT |
| Icons in build output | `ls bin/.../Assets/` | 3 ico files present | 2026-04-28 |
| Trimmed publish size | `ls -lh publish/VoiceClip.exe` | 53MB | 2026-04-28 |
| Runtime end-to-end dictation | Manual test | Not yet verified | — |
| Inno Setup installer build | Manual test (GUI) | User reported path/type errors, all fixed | 2026-04-28 |

## 9. Restart Instructions

The project is feature-complete with all known bugs fixed. Next session should:

1. **Runtime test**: Launch `publish\VoiceClip.exe`, test dictation with microphone, verify tray icons, hotkeys, history popup, auto-stop saves text
2. **Build installer**: Run `iscc installer\VoiceClip.iss` (ensure fresh terminal after Inno Setup install)
3. **Push to origin**: `git push`

Last updated: 2026-04-28 13:35 CDT
