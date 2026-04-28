# PROJECT_HANDOFF.md — VoiceClip

**Last Updated**: 2026-04-28 (Session 2)
**Status**: Implemented, all known issues resolved

---

## Project Summary

VoiceClip is a Windows 11 system tray app that captures voice dictation into a persistent clipboard buffer using `Windows.Media.SpeechRecognition` (same engine as Voice Access). Independent of Voice Access — both can coexist.

## Tech Stack

- C# / .NET 8 WPF
- Windows.Media.SpeechRecognition (WinRT)
- Hardcodet.NotifyIcon.Wpf (system tray)
- Inno Setup 6 (installer)
- xUnit (53 tests)

## Build & Run

```
dotnet build src/VoiceClip/VoiceClip.csproj
dotnet run --project src/VoiceClip
dotnet test tests/VoiceClip.Tests
```

## Current State

- **SPEC**: SPEC-VOICECLIP-001 — Status: Implemented
- **Build**: 0 errors, 0 warnings
- **Tests**: 53/53 passing
- **Published exe**: 53MB (self-contained + trimmed)
- **Installer**: `installer/VoiceClip.iss` (Inno Setup)

## Code Review Sessions

### Session 1 (2026-04-28)

| # | Severity | Issue | Status |
|---|----------|-------|--------|
| 1 | 🟠 High | Dispose() called .AsTask() directly on WinRT — crash at runtime | ✅ Fixed (a1e2b0f) |
| 2 | 🟡 Medium | Preview truncation no ellipsis | ✅ Fixed (7b06403) |
| 3 | 🟡 Medium | Startup failure not reported | ✅ Fixed (7b06403) |

### Session 2 (2026-04-28)

| # | Severity | Issue | Status |
|---|----------|-------|--------|
| 1 | 🔴 Critical | Auto-stop (silence timeout) loses dictation text — OnSessionCompleted didn't save or notify App | ✅ Fixed (7b06403) |
| 2 | 🟠 High | "Clear All" has no confirmation dialog (SPEC REQ-VC-004) | ✅ Fixed (7b06403) |
| 3 | 🟡 Medium | Preview truncation no ellipsis | ✅ Fixed (7b06403) |
| 4 | 🟡 Medium | Startup registration failure not shown to user | ✅ Fixed (7b06403) |

### All Issues Resolved

No known remaining issues. All SPEC requirements implemented.

## Key Architecture Decisions

1. **WinRTAsyncHelper** — Reflection-based AsTask() wrapper for explicit `.AsTask()` calls. `await` keyword works fine (uses GetAwaiter, not AsTask).
2. **Trimming** — Full trim mode with `_SuppressWpfTrimError=true`. VoiceClip is a TrimmerRootAssembly.
3. **DictationCompleted event** — Raised from both `StopDictationAsync` (manual stop) and `OnSessionCompleted` (auto-stop). App's `OnDictationCompleted` handler is the single point for saving to history, clipboard, and UI updates.
4. **Single-instance** — Named mutex with GUID.
5. **Storage** — JSON files in `%APPDATA%\VoiceClip\` (history.json, settings.json, error.log).
