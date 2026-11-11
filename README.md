# ShiningEditor

Shining Editor is a Genesis / Mega Drive save state editor for **Shining in the Darkness**, **Shining Force**, **Shining Force II**, and **Shining Force CD**.

It works with save states created by the **Kega Fusion 3.64** emulator (`.gs0` / `.gsx`): a Genesis save state for Shining in the Darkness / Shining Force / Shining Force II, and a Sega CD save state for Shining Force CD. When you open a file the editor checks that it is a Fusion save state (`GST` header) of the expected size for the selected game, so you can't accidentally edit the wrong file.

## Features

- A themed interface modelled on the games' own menus — navy windows with silver frames, gold headings, HP / MP gauges on each force member's card, and the game's box art. Each game has its own accent colour.
- Edit gold, experience, HP / MP and stats for every character.
- **Edit as many characters as you like before saving.** Every change is kept in memory as you move between characters; edited characters are marked ● in the list, and a banner lists everything that's pending. **Save** writes all of it at once.
- View each character's items and magic (Shining Force, Shining Force II and Shining Force CD).
- Warns before closing, switching games or opening another file with unsaved changes, and if the emulator has rewritten the file since you opened it.
- Drag a save state onto the window to open it.

## Using it

1. Pick the game from the **GAME** box (or the **Game** menu).
2. **File ▸ Open…** your Kega Fusion save state.
3. Choose a character on the left and edit the gold-labelled rows on the **STATUS** tab. Grey rows are shown for reference only.
4. Repeat for any other characters, and edit **GOLD** at the top.
5. Click **Save** (Ctrl+S), then load the state in Kega Fusion.

## Backups

The first time you save in a session, the editor copies the untouched save state to `<file>.bak` next to the original, so the pre-edit state can always be recovered. Even so, it's a good idea to keep your own backup of anything important.

## Requirements

- Windows
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (to run)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (to build)

## Running

Download / extract `ShiningEditor.zip` from the repository root and run `ShiningEditor.exe`.

## Building

The project is an SDK-style .NET 10 (`net10.0-windows`) Windows Forms app.

```
dotnet build -c Release
```

or open `ShiningEditor.sln` in Visual Studio 2022+.

## Project layout

- `Game/` — save-state buffer (`SaveState.cs`), one definition per game with its offsets and fields, and the item / magic name tables.
- `MainForm.cs` / `Theme.cs` — the UI.
