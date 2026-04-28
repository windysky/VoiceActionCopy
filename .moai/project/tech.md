# VoiceClip — Technology Stack

## Primary Language

C# (.NET 8)

## Framework

WPF (Windows Presentation Foundation) — Rich data binding, popup support, XAML templating

## Target Platform

Windows 11 (build 10.0.22621+)

## Key Dependencies

| Package | Purpose | Version |
|---------|---------|---------|
| Microsoft.Windows.SDK.Contracts | Windows Runtime API access | 10.0.22621.x |
| Microsoft.Windows.CsWinRt | WinRT projection for C# | Latest stable |

## Build and Publish

- **SDK**: .NET 8 SDK
- **Build**: `dotnet build`
- **Test**: `dotnet test`
- **Publish**: Single-file publish via `dotnet publish` (.NET 8 publish profile)
- **Output**: Self-contained executable (~30MB with SDK dependencies)

## Key Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Speech engine | Windows.Media.SpeechRecognition | Same engine as Voice Access, best Windows accuracy, offline |
| Async wrapper | Reflection-based AsTask() | WinRT/SDK type conflict in WPF (Rick Strahl pattern) |
| Storage format | JSON file | Simple, human-readable, no database dependency |
| Global hotkeys | P/Invoke RegisterHotKey | Required for system-wide hotkeys in .NET |
| Single instance | Named mutex | Prevent multiple app instances |
| Target framework | .NET 8 | LTS, modern C# features, single-file publish |

## Storage

- History: `%APPDATA%\VoiceClip\history.json` (JSON, max 500 entries)
- Settings: `%APPDATA%\VoiceClip\settings.json`

## Development Environment

- .NET 8 SDK
- Visual Studio 2022 or VS Code with C# extension
- Windows 11 (required for Windows.Media.SpeechRecognition)

## Testing

- xUnit test framework
- Tests in `tests/VoiceClip.Tests/`
- Test targets: HistoryService, ClipboardService, DictationEntry model
