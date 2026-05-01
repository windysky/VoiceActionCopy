# Screenshots

The main `README.md` references the following PNG files in this folder. Drop captures here with these exact filenames so the README renders correctly:

| Filename | What to capture | Suggested size |
|----------|-----------------|----------------|
| `floating-button.png` | The blue/red floating button visible on top of a Notepad window. Show both the floating button and Notepad in the same shot. | ~800px wide |
| `recording-popup.png` | The recording popup showing live partial results ("Listening… [interim text]") | ~600px wide |
| `history-popup.png` | History popup with several past dictation entries | ~600px wide |
| `settings.png` | Settings window — make sure the **Microphone** dropdown row is visible | ~600px wide |
| `tray-icon.png` | The system tray with the VoiceClip mic icon visible (right-click context menu open is a nice touch) | crop tightly |

## How to capture

1. **Win + Shift + S** opens Windows Snipping Tool → "Rectangle".
2. Save each capture into this folder with the filename above.
3. PNG, no compression artifacts.

## After adding screenshots

```powershell
git add assets/screenshots/*.png
git commit -m "docs: add README screenshots"
git push
```
