# VoiceClip

## Description

VoiceClip is a lightweight Windows system tray application that captures voice dictation into a persistent clipboard buffer. It uses the same Windows.Media.SpeechRecognition engine as Windows Voice Access, storing each dictation session as a reusable history entry that can be copied to the clipboard and pasted into any application.

## Problem

Windows Voice Access provides accurate voice dictation, but text is typed directly into the focused window. If the wrong window has focus, dictation is lost or goes to the wrong application. No existing tool (Handy, asr2clip, Voice Capture, Windows Clipboard History) solves this problem.

## Target Audience

- Windows 11 users who rely on voice dictation for text input
- Users who need to dictate text independent of which window is focused
- Users who want to review, search, and reuse past dictations

## Core Features

1. **System Tray Presence** — Tray icon with visual state indication (idle, recording, error)
2. **Dictation Toggle** — Global hotkey (Ctrl+Alt+D) to start/stop voice recording
3. **Continuous Speech Recognition** — Real-time transcription via Windows.Media.SpeechRecognition
4. **History Popup** — Searchable popup (Ctrl+Alt+V) showing all past dictations
5. **Copy to Clipboard** — One-click copy of any dictation entry to Windows clipboard
6. **Persistent History Storage** — JSON-based storage at %APPDATA%\VoiceClip\history.json
7. **Global Hotkey Registration** — System-wide hotkeys via Windows API (P/Invoke)
8. **Windows Speech Recognition Integration** — Graceful dependency handling
9. **Settings Dialog** — Language, silence timeout, history limits, startup configuration

## Key Differentiator

VoiceClip is independent of Voice Access — both can coexist. User chooses which to use per situation. VoiceClip captures to a buffer; Voice Access types into the focused window.

## SPEC Reference

SPEC-VOICECLIP-001
