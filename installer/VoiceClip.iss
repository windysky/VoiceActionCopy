; VoiceClip Installer Script (Inno Setup)
; Build: iscc installer\VoiceClip.iss
; Requires Inno Setup 6+: https://jrsoftware.org/isdl.php

#define AppName "VoiceClip"
#define AppVersion "1.0.1"
#define AppPublisher "Junguk Hur"
#define AppExeName "VoiceClip.exe"
#define AppCopyright "Copyright (c) 2026 Junguk Hur"

[Setup]
AppId={{B7E3F2A1-4D5C-6E8A-9F0B-1C2D3E4F5A6B}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/windysky/VoiceClip
AppCopyright={#AppCopyright}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#AppExeName}
OutputDir=..\dist
OutputBaseFilename=VoiceClip-{#AppVersion}-setup
SetupIconFile=..\src\VoiceClip\Assets\mic-idle.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
MinVersion=10.0.22621
LicenseFile=..\LICENSE.txt
InfoBeforeFile=DISCLAIMER.txt

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"
Name: "startup"; Description: "Run on Windows &startup"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
Source: "..\src\VoiceClip\bin\Release\net8.0-windows10.0.22621.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent; Check: SpeechPrivacyAccepted
Filename: "ms-settings:privacy-speech"; Description: "Open Speech settings (required for dictation)"; Flags: postinstall shellexec skipifsilent nowait; Check: not SpeechPrivacyAccepted

[Code]
function SpeechPrivacyAccepted: Boolean;
var
  Value: Cardinal;
begin
  Result := RegQueryDWordValue(HKCU, 'Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy', 'HasAccepted', Value) and (Value = 1);
end;

[Registry]
; Run on startup (optional task)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#AppName}"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\{#AppName}"
