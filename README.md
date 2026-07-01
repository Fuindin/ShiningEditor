# ShiningEditor

Shining Editor is a Genesis / Mega Drive save state editor that lets you edit save states for **Shining in the Darkness**, **Shining Force**, **Shining Force II**, and **Shining Force CD**.

It works with save states created by the **Kega Fusion 3.64** emulator (`.gs0` / `.gsx`): a Genesis save state for Shining in the Darkness / Shining Force / Shining Force II, and a Sega CD save state for Shining Force CD. When you open a file the editor checks that it is a Fusion save state (`GST` header) of the expected size for the selected game, so you can't accidentally edit the wrong file.

You can modify gold, experience, and character stats. The Shining Force, Shining Force II, and Shining Force CD panels also display each character's current items and magic.

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

## Backups

Before the first edit of a session the editor automatically writes a `<file>.bak` copy of the save state next to the original, so the pre-edit state can be recovered. Even so, it's a good idea to keep your own backup of anything important before editing.
