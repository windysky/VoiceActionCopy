# VoiceClip — Automated screenshot capture
# Drives the running app via UI Automation + hotkeys, captures windows to PNG.
# Output: assets/screenshots/{floating-button,recording-popup,history-popup,settings,tray-icon}.png

param(
    [string]$Exe    = "C:\Users\juhur\OneDrive\UND\VoiceActionCopy\src\VoiceClip\bin\Debug\net8.0-windows10.0.22621.0\VoiceClip.exe",
    [string]$OutDir = "C:\Users\juhur\OneDrive\UND\VoiceActionCopy\assets\screenshots"
)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class W32 {
    [DllImport("user32.dll")] public static extern IntPtr FindWindow(string cls, string wnd);
    [DllImport("user32.dll")] public static extern bool   GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool   SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern int    GetSystemMetrics(int i);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
}
'@

$desktop = [System.Windows.Automation.AutomationElement]::RootElement

function Find-Win($title) {
    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $title)
    return $desktop.FindFirst([System.Windows.Automation.TreeScope]::Children, $c)
}

function Wait-Win($title, $sec=8) {
    for ($i = 0; $i -lt ($sec * 4); $i++) {
        $w = Find-Win $title
        if ($w) { return $w }
        Start-Sleep -Milliseconds 250
    }
    return $null
}

function Capture-Element($element, $path, [int]$pad = 8) {
    if (-not $element) { Write-Warning "null element for $path"; return $false }
    $rect = $element.Current.BoundingRectangle
    $x = [int]$rect.X - $pad
    $y = [int]$rect.Y - $pad
    $w = [int]$rect.Width  + (2 * $pad)
    $h = [int]$rect.Height + (2 * $pad)
    if ($w -le 0 -or $h -le 0) { Write-Warning ("bad rect for {0}: {1} x {2}" -f $path, $w, $h); return $false }
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($x, $y, 0, 0, (New-Object System.Drawing.Size $w, $h))
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host ("  saved: {0} ({1} x {2})" -f $path, $w, $h)
    return $true
}

function Capture-Region($x, $y, $w, $h, $path) {
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($x, $y, 0, 0, (New-Object System.Drawing.Size $w, $h))
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host ("  saved: {0} ({1} x {2})" -f $path, $w, $h)
}

# ── Setup ─────────────────────────────────────────────────────────────────
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
Get-Process VoiceClip -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

if (-not (Test-Path $Exe)) { throw "Exe not found: $Exe" }
Write-Host "[*] Launching VoiceClip..."
$proc = Start-Process -FilePath $Exe -PassThru
Start-Sleep -Seconds 4

$shell = New-Object -ComObject WScript.Shell

# ── 1. Floating button ────────────────────────────────────────────────────
Write-Host "[1/5] Floating button..."
$fb = Wait-Win "VoiceClip" 6
if ($fb) {
    Capture-Element $fb (Join-Path $OutDir "floating-button.png") 12 | Out-Null
} else {
    Write-Warning "Floating button window not found"
}

# ── 2. Recording popup (start dictation via Ctrl+Alt+D) ───────────────────
Write-Host "[2/5] Recording popup..."
$shell.SendKeys("^%d")
Start-Sleep -Milliseconds 1500
$pop = Wait-Win "VoiceClip - Recording" 4
if ($pop) {
    Capture-Element $pop (Join-Path $OutDir "recording-popup.png") 10 | Out-Null
} else {
    Write-Warning "Recording popup not found"
}
# Stop dictation
$shell.SendKeys("^%d")
Start-Sleep -Seconds 2

# ── 3. History popup (Ctrl+Alt+V) ─────────────────────────────────────────
Write-Host "[3/5] History popup..."
$shell.SendKeys("^%v")
Start-Sleep -Milliseconds 1200
$hist = Wait-Win "VoiceClip History" 4
if (-not $hist) { $hist = Wait-Win "History" 2 }
if ($hist) {
    Capture-Element $hist (Join-Path $OutDir "history-popup.png") 10 | Out-Null
    # Close popup by sending ESC
    $shell.SendKeys("{ESC}")
    Start-Sleep -Milliseconds 500
} else {
    Write-Warning "History popup not found"
}

# ── 4. Settings window ────────────────────────────────────────────────────
# Settings opens from tray context menu — easiest route is right-click the floating button
Write-Host "[4/5] Settings window..."
if ($fb) {
    $rect = $fb.Current.BoundingRectangle
    $cx = [int]($rect.X + $rect.Width / 2)
    $cy = [int]($rect.Y + $rect.Height / 2)

    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class M {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
    public const uint RBD = 0x0008;
    public const uint RBU = 0x0010;
}
'@ -ErrorAction SilentlyContinue

    [M]::SetCursorPos($cx, $cy) | Out-Null
    Start-Sleep -Milliseconds 200
    [M]::mouse_event([M]::RBD, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 120
    [M]::mouse_event([M]::RBU, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 1200

    # Find the Settings MenuItem via UIA and invoke it
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, "Settings")
    $settingsItem = $desktop.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if ($settingsItem) {
        $invoke = $settingsItem.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $invoke.Invoke()
    } else {
        Write-Warning "Settings menu item not found in context menu"
    }
    Start-Sleep -Seconds 2

    $set = Wait-Win "VoiceClip Settings" 5
    if (-not $set) { $set = Wait-Win "Settings" 3 }
    if ($set) {
        [W32]::SetForegroundWindow([IntPtr]$set.Current.NativeWindowHandle) | Out-Null
        Start-Sleep -Milliseconds 400
        Capture-Element $set (Join-Path $OutDir "settings.png") 12 | Out-Null
        $shell.SendKeys("{ESC}")
        Start-Sleep -Milliseconds 500
    } else {
        Write-Warning "Settings window not found"
    }
} else {
    Write-Warning "Skipped settings (no floating button)"
}

# ── 5. Tray icon (capture taskbar notification area) ──────────────────────
Write-Host "[5/5] Tray icon..."
# Capture the right-end of the taskbar (notification area)
$screenW = [W32]::GetSystemMetrics(0)
$screenH = [W32]::GetSystemMetrics(1)
# Standard Win11 taskbar height ~48px at 100% scale, notification area ~400px wide on the right
$trayW = 480
$trayH = 56
$trayX = $screenW - $trayW
$trayY = $screenH - $trayH
Capture-Region $trayX $trayY $trayW $trayH (Join-Path $OutDir "tray-icon.png")

# ── Cleanup ───────────────────────────────────────────────────────────────
Write-Host "[*] Stopping VoiceClip..."
Get-Process VoiceClip -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Done. Files in $OutDir :"
Get-ChildItem $OutDir -Filter *.png | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
