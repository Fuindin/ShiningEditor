namespace ShiningEditor.Game;

/// <summary>Shining Force (Genesis). 30 force members, 0x28-byte records starting at 0xC115.</summary>
internal sealed class ShiningForceGame : GameDefinition
{
    private const int TableBase = 0xC115;
    private const int RecordSize = 0x28;

    private static readonly string[] Names =
    {
        "Max (Hero)", "Mae", "Pelle", "Ken", "Vankar", "Earnest", "Arthur", "Gort", "Luke",
        "Guntz", "Anri", "Alef", "Tao", "Domingo", "Lowe", "Khris", "Torasu", "Gong",
        "Diane", "Hans", "Lyle", "Amon", "Balbaroy", "Kokichi", "Bleu", "Adam", "Zylo",
        "Musashi", "Hanzou", "Jogurt",
    };

    public override string Title => "Shining Force";
    public override string MenuText => "Shining &Force";
    public override int GoldOffset => 0xC107;
    public override int GoldWidth => 3;
    public override string CoverResource => "covers.sf.jpg";

    // Guardiana royal blue.
    public override GameAccent Accent { get; } =
        new(Color.FromArgb(0x2E, 0x5C, 0xD6), Color.FromArgb(0x0C, 0x1E, 0x66), Color.FromArgb(0x9C, 0xC4, 0xFF));

    public override IReadOnlyList<FieldDef> Fields { get; } = new FieldDef[]
    {
        new("level",   "LV",      "Level",                1, Editable: false),
        new("exp",     "EXP",     "Experience (100 = level up)", 1),
        new("hpCur",   "HP",      "Current hit points",   1),
        new("hpMax",   "MAX HP",  "Maximum hit points",   1),
        new("mpCur",   "MP",      "Current magic points", 1),
        new("mpMax",   "MAX MP",  "Maximum magic points", 1),
        new("attack",  "ATTACK",  "Attack",               1),
        new("defense", "DEFENSE", "Defense",              1),
        new("agility", "AGILITY", "Agility",              1),
        new("move",    "MOVE",    "Movement",             1),
    };

    public override IReadOnlyList<CharacterDef> BuildCharacters(SaveState save)
    {
        var list = new List<CharacterDef>(Names.Length);
        for (int i = 0; i < Names.Length; i++)
        {
            int b = TableBase + i * RecordSize;
            var o = new Dictionary<string, int>
            {
                ["level"] = b + 0x00, ["attack"] = b + 0x01, ["defense"] = b + 0x02,
                ["agility"] = b + 0x03, ["move"] = b + 0x04, ["exp"] = b + 0x06,
                ["hpMax"] = b + 0x08, ["hpCur"] = b + 0x0A, ["mpCur"] = b + 0x0B, ["mpMax"] = b + 0x0C,
            };
            list.Add(new CharacterDef(Names[i], o,
                new[] { b + 0x0F, b + 0x10, b + 0x11, b + 0x12 },
                new[] { b + 0x13, b + 0x14, b + 0x15, b + 0x16 },
                b));
        }

        return list;
    }

    public override IReadOnlyList<SlotEntry> Items(SaveState save, CharacterDef c) =>
        FromTable(save, c.ItemOffsets, ItemTables.ShiningForceItems);

    public override IReadOnlyList<SlotEntry> Spells(SaveState save, CharacterDef c) =>
        FromTable(save, c.MagicOffsets, ItemTables.ShiningForceMagic);
}
