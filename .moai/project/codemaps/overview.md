# VoiceClip — Architecture Overview

## Project Goals

VoiceClip is a Windows system tray app that captures voice dictation into a persistent clipboard buffer using Windows.Media.SpeechRecognition. Independent of Voice Access — both coexist.

## Architecture

MVVM pattern with service layer:

```
HotkeyService → SpeechRecognitionService → HistoryService → HistoryPopup → ClipboardService
                                     ↕                        ↕
                              TrayIconManager            JSON Storage
```

## Key Components

- **SpeechRecognitionService**: Continuous dictation via Windows.Media.SpeechRecognition
- **HistoryService**: JSON persistence at %APPDATA%\VoiceClip\history.json
- **HotkeyService**: Global hotkeys via P/Invoke (Ctrl+Alt+D, Ctrl+Alt+V)
- **TrayIconManager**: System tray icon with state transitions
- **WinRTAsyncHelper**: Reflection-based AsTask() for WinRT/SDK compatibility

## SPEC Reference

SPEC-VOICECLIP-001 — full requirements, acceptance criteria, and implementation phases.
