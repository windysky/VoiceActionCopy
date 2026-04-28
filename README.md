# VoiceClip

Voice dictation clipboard for Windows. Captures speech to a persistent clipboard buffer.

## What It Does

VoiceClip captures voice dictation into a persistent history, independent of which window has focus. Dictate text, then paste it anywhere.

## Requirements

- Windows 11 (build 10.0.22621+)
- Microphone
- Speech recognition enabled in Windows Settings

No .NET runtime needed — the published build is self-contained.

## Quick Start

1. Build: `dotnet build VoiceClip.sln`
2. Run: `dotnet run --project src/VoiceClip`
3. Press **Ctrl+Alt+D** to start/stop dictation
4. Press **Ctrl+Alt+V** to open history popup
5. Click any entry to copy it to clipboard, then Ctrl+V to paste

## Build from Source

```bash
# Prerequisites: .NET 8 SDK
dotnet build VoiceClip.sln
dotnet test VoiceClip.sln
```

## Publish Single File

```bash
dotnet publish src/VoiceClip -c Release -r win-x64 --self-contained -o publish
```

Produces a ~53MB self-contained exe with trimming enabled.

## Build Installer

Requires [Inno Setup 6+](https://jrsoftware.org/isdl.php):

```bash
iscc installer\VoiceClip.iss
```

Output: `dist/VoiceClip-1.0.0-setup.exe` — includes start menu shortcut, optional run-on-startup, and uninstaller.

## Icon Generation

Tray icons are generated programmatically (no manual asset editing needed):

```bash
cd tools
dotnet run --project IconGen.csproj -- ../src/VoiceClip/Assets
```

## Configuration

Settings are stored at `%APPDATA%\VoiceClip\settings.json`:

| Setting | Default | Range |
|---------|---------|-------|
| Language | en-US | Any Windows speech locale |
| Silence timeout | 60 seconds | 10-300 |
| Max history entries | 500 | 50-5000 |
| Run on startup | false | true/false |

## Hotkeys

| Hotkey | Action |
|--------|--------|
| Ctrl+Alt+D | Toggle dictation on/off |
| Ctrl+Alt+V | Show history popup |
| Double-click tray | Show history popup |

## Data Storage

- History: `%APPDATA%\VoiceClip\history.json`
- Settings: `%APPDATA%\VoiceClip\settings.json`

## Running Tests

```bash
dotnet test tests/VoiceClip.Tests
```

## License

MIT
