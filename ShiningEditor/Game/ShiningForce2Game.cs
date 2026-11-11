namespace ShiningEditor.Game;

/// <summary>Shining Force II (Genesis). 20 force members, 0x38-byte records starting at 0x10C83.</summary>
internal sealed class ShiningForce2Game : GameDefinition
{
    private const int TableBase = 0x10C83;
    private const int RecordSize = 0x38;

    private static readonly string[] Names =
    {
        "Bowie (Hero)", "Sarah", "Chester", "Jaha", "Kazin", "Slade", "Kiwi", "Peter",
        "May", "Gerhalt", "Luke", "Rohde", "Rick", "Elric", "Eric", "Karna", "Randolf",
        "Tyrin", "Janet", "Higins",
    };

    public override string Title => "Shining Force II";
    public override string MenuText => "Shining Force &II";
    public override int GoldOffset => 0x11A7A;
    public override int GoldWidth => 2;
    public override string CoverResource => "covers.sf2.jpg";

    // Grans Island sea teal.
    public override GameAccent Accent { get; } =
        new(Color.FromArgb(0x1E, 0x86, 0xC8), Color.FromArgb(0x08, 0x2C, 0x5E), Color.FromArgb(0x8E, 0xE0, 0xFF));

    public override IReadOnlyList<FieldDef> Fields { get; } = new FieldDef[]
    {
        new("level",      "LV",         "Level",                    1, Editable: false),
        new("exp",        "EXP",        "Experience (100 = level up)", 1),
        new("hpCur",      "HP",         "Current hit points",       2),
        new("hpMax",      "MAX HP",     "Maximum hit points",       2),
        new("mpCur",      "MP",         "Current magic points",     1),
        new("mpMax",      "MAX MP",     "Maximum magic points",     1),
        new("attackBase", "ATTACK",     "Base attack",              1),
        new("attackEq",   "ATTACK+",    "Attack with equipment",    1),
        new("defenseBase","DEFENSE",    "Base defense",             1),
        new("defenseEq",  "DEFENSE+",   "Defense with equipment",   1),
        new("agilityBase","AGILITY",    "Base agility",             1),
        new("agilityEq",  "AGILITY+",   "Agility with equipment",   1),
        new("moveBase",   "MOVE",       "Base movement",            1),
        new("moveEq",     "MOVE+",      "Movement with equipment",  1),
        new("kills",      "KILLS",      "Enemies defeated",         2, Editable: false),
        new("defeats",    "DEFEATS",    "Times defeated",           2, Editable: false),
    };

    public override IReadOnlyList<CharacterDef> BuildCharacters(SaveState save)
    {
        var list = new List<CharacterDef>(Names.Length);
        for (int i = 0; i < Names.Length; i++)
        {
            int b = TableBase + i * RecordSize;
            var o = new Dictionary<string, int>
            {
                ["level"] = b + 0x00, ["hpMax"] = b + 0x01, ["hpCur"] = b + 0x03,
                ["mpMax"] = b + 0x05, ["mpCur"] = b + 0x06,
                ["attackBase"] = b + 0x07, ["attackEq"] = b + 0x08,
                ["defenseBase"] = b + 0x09, ["defenseEq"] = b + 0x0A,
                ["agilityBase"] = b + 0x0B, ["agilityEq"] = b + 0x0C,
                ["moveBase"] = b + 0x0D, ["moveEq"] = b + 0x0E,
                ["exp"] = b + 0x25, ["kills"] = b + 0x27, ["defeats"] = b + 0x2B,
            };

            // Preserved from the original data: Bowie's 2nd item slot is at +0x15,
            // unlike every other character's +0x18.
            int item2 = i == 0 ? b + 0x15 : b + 0x18;
            list.Add(new CharacterDef(Names[i], o,
                new[] { b + 0x16, item2, b + 0x1A, b + 0x1C },
                new[] { b + 0x1D, b + 0x1E, b + 0x1F, b + 0x20 },
                b));
        }

        return list;
    }

    public override string Subtitle(SaveState save, CharacterDef c) =>
        $"Kills {save.Read(c.Offsets["kills"], 2):N0}   ·   Defeats {save.Read(c.Offsets["defeats"], 2):N0}";

    public override IReadOnlyList<SlotEntry> Items(SaveState save, CharacterDef c) =>
        FromTable(save, c.ItemOffsets, ItemTables.ShiningForce2Items);

    public override IReadOnlyList<SlotEntry> Spells(SaveState save, CharacterDef c) =>
        FromTable(save, c.MagicOffsets, ItemTables.ShiningForce2Magic);
}
