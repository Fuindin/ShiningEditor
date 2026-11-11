namespace ShiningEditor.Game;

/// <summary>One stat row on the STATUS tab.</summary>
/// <param name="Key">Lookup key into a character's offsets (e.g. "hpMax").</param>
/// <param name="Label">Short in-game style label (e.g. "MAX HP").</param>
/// <param name="Width">Size in bytes (big-endian).</param>
/// <param name="Editable">False = shown for reference only.</param>
internal sealed record FieldDef(string Key, string Label, string Description, int Width, bool Editable = true)
{
    public long Max => SaveState.MaxFor(Width);
}

/// <summary>A party member: a name plus the absolute file offset of each field.</summary>
internal sealed class CharacterDef
{
    public CharacterDef(string name, Dictionary<string, int> offsets, int[] itemOffsets, int[] magicOffsets, int baseOffset = -1)
    {
        Name = name;
        Offsets = offsets;
        ItemOffsets = itemOffsets;
        MagicOffsets = magicOffsets;
        BaseOffset = baseOffset;
    }

    public string Name { get; }
    public Dictionary<string, int> Offsets { get; }
    public int[] ItemOffsets { get; }
    public int[] MagicOffsets { get; }
    public int BaseOffset { get; }
}

/// <summary>An inventory / spell slot resolved to a display name.</summary>
internal readonly record struct SlotEntry(int Raw, string Name, bool Empty, bool Equipped = false, bool Unknown = false);

/// <summary>Colours that give each game its own identity on top of the shared theme.</summary>
internal sealed record GameAccent(Color PlateTop, Color PlateBottom, Color Glow);

/// <summary>Everything the editor needs to know about one game's save-state layout.</summary>
internal abstract class GameDefinition
{
    public abstract string Title { get; }
    public abstract string MenuText { get; }
    public virtual bool IsSegaCd => false;

    /// <summary>False when inventory / spell locations aren't mapped (hides those tabs).</summary>
    public virtual bool HasInventory => true;

    public abstract int GoldOffset { get; }
    public abstract int GoldWidth { get; }

    public abstract IReadOnlyList<FieldDef> Fields { get; }

    /// <summary>Logical name of the embedded box-art image.</summary>
    public abstract string CoverResource { get; }
    public abstract GameAccent Accent { get; }

    /// <summary>Builds the party for a loaded save (SFCD reads names from the file).</summary>
    public abstract IReadOnlyList<CharacterDef> BuildCharacters(SaveState save);

    /// <summary>Called once after a file loads, before any lookups.</summary>
    public virtual void Prepare(SaveState save) { }

    public abstract IReadOnlyList<SlotEntry> Items(SaveState save, CharacterDef c);
    public abstract IReadOnlyList<SlotEntry> Spells(SaveState save, CharacterDef c);

    /// <summary>Extra line in the header (e.g. the SFCD book).</summary>
    public virtual string? InfoLine(SaveState save) => null;

    /// <summary>Text under the name on the name plate (class, record, …).</summary>
    public virtual string Subtitle(SaveState save, CharacterDef c) => Title;

    public virtual string? MagicNote => null;

    public FieldDef? Field(string key) => Fields.FirstOrDefault(f => f.Key == key);

    /// <summary>Reads a stat for a character, or null when that character doesn't have it.</summary>
    public long? Get(SaveState save, CharacterDef c, string key) =>
        c.Offsets.TryGetValue(key, out int off) && Field(key) is { } f ? save.Read(off, f.Width) : null;

    // ---- Helpers for subclasses -------------------------------------------------

    protected static string Hex2(int raw) => $"0x{raw:X2}";

    protected static bool IsEmptyName(string name) =>
        name is "Empty" or "Blank Space" or "Nothing" || name.StartsWith("Empty", StringComparison.Ordinal);

    /// <summary>Looks up each slot in a table; unknown ids are skipped (matches the original editor).</summary>
    protected static IReadOnlyList<SlotEntry> FromTable(SaveState save, int[] offsets, IReadOnlyDictionary<int, string> table)
    {
        var list = new List<SlotEntry>(offsets.Length);
        foreach (int off in offsets)
        {
            int raw = save.Byte(off);
            if (table.TryGetValue(raw, out string? name))
            {
                list.Add(new SlotEntry(raw, name, IsEmptyName(name)));
            }
        }

        return list;
    }
}

/// <summary>Registry of supported games, in menu order.</summary>
internal static class Games
{
    public static readonly IReadOnlyList<GameDefinition> All = new GameDefinition[]
    {
        new ShiningInTheDarknessGame(),
        new ShiningForceGame(),
        new ShiningForce2Game(),
        new ShiningForceCDGame(),
    };
}
