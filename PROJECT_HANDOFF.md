# PROJECT_HANDOFF.md — VoiceClip

**Last Updated**: 2026-04-28
**Status**: Implemented, functional, installer-ready

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
- **Branch**: main (6 commits ahead of origin)

## Code Review Session (2026-04-28)

### Issues Found

| # | Severity | File | Issue | Status |
|---|----------|------|-------|--------|
| 1 | 🟠 High | SpeechRecognitionService.cs | Dispose() called .AsTask() directly on WinRT type — crashes at runtime. WinRTAsyncHelper needed. | ✅ Fixed |
| 2 | 🟡 Medium | DictationEntry.cs | Preview truncation has no ellipsis | Open (cosmetic) |
| 3 | 🟡 Medium | SettingsWindow.xaml.cs | SetStartup failure not reported to user | Open (edge case) |
| 4 | 🟢 Low | HistoryPopup.xaml | ListBox InputBindings binding context | Not a bug (verified) |
| 5 | 🟢 Low | SpeechRecognitionService.cs | OnSessionCompleted race with StopDictationAsync | Not a bug (idempotent) |

### Resolved

- `a1e2b0f` — Fix Dispose() WinRT AsTask() crash (Issue #1)

### Open (Low Priority)

- Preview text truncation could show "..." suffix
- Settings Save could report startup registration failure

## Key Architecture Decisions

1. **WinRTAsyncHelper** — Reflection-based AsTask() wrapper required due to Windows SDK / CsWinRt type conflicts in WPF. Used in SpeechRecognitionService everywhere WinRT async → Task conversion happens.
2. **Trimming** — Full trim mode with `_SuppressWpfTrimError=true`. VoiceClip is a TrimmerRootAssembly to preserve reflection targets.
3. **Single-instance** — Named mutex with GUID.
4. **Storage** — JSON files in `%APPDATA%\VoiceClip\` (history.json, settings.json, error.log).

## Files Modified in This Session

```
src/VoiceClip/App.xaml.cs              — Error handlers, ms-settings link, SpeechRecognitionTopicConstraint
src/VoiceClip/Services/SpeechRecognitionService.cs — Try/catch, topic constraint, WinRTAsyncHelper in Dispose
src/VoiceClip/VoiceClip.csproj         — Trimming config, Content items for icons
src/VoiceClip/Properties/PublishProfiles/single-file.pubxml — TrimMode=full
src/VoiceClip/Assets/*.ico             — Generated tray icons
tools/*                                — Icon generator
installer/VoiceClip.iss                — Inno Setup installer script
.gitignore                             — publish/, dist/
README.md                              — Updated docs
docs/SPEC-VOICECLIP-001.md             — Status → Implemented
```
