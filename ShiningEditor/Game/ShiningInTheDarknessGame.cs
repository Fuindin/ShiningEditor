namespace ShiningEditor.Game;

/// <summary>Shining in the Darkness (Genesis). Each stat lives in its own table, one entry per hero.</summary>
internal sealed class ShiningInTheDarknessGame : GameDefinition
{
    public override string Title => "Shining in the Darkness";
    public override string MenuText => "Shining in the &Darkness";
    public override bool HasInventory => false;
    public override int GoldOffset => 0x3B1C;
    public override int GoldWidth => 4;
    public override string CoverResource => "covers.sitd.jpg";

    // Torch-lit dungeon violet.
    public override GameAccent Accent { get; } =
        new(Color.FromArgb(0x5A, 0x3C, 0xB4), Color.FromArgb(0x1C, 0x10, 0x4A), Color.FromArgb(0xB8, 0x9C, 0xFF));

    public override IReadOnlyList<FieldDef> Fields { get; } = new FieldDef[]
    {
        new("level",  "LV",      "Level",              2, Editable: false),
        new("exp",    "EXP",     "Experience",         4),
        new("hpCur",  "HP",      "Current hit points", 2),
        new("hpMax",  "MAX HP",  "Maximum hit points", 2),
        new("mpCur",  "MP",      "Current magic points", 2),
        new("mpMax",  "MAX MP",  "Maximum magic points", 2),
        new("attack", "ATTACK",  "Attack",             2),
        new("defense","DEFENSE", "Defense",            2, Editable: false),
        new("iq",     "IQ",      "Intelligence",       2),
        new("speed",  "SPEED",   "Speed",              2, Editable: false),
        new("luck",   "LUCK",    "Luck",               2),
    };

    public override IReadOnlyList<CharacterDef> BuildCharacters(SaveState save) => new[]
    {
        // Hiro is a pure fighter: no MP fields.
        Make("Hiro", level: 0x3B50, exp: 0x3B56, hpCur: 0x3B20, hpMax: 0x3B2C, mpCur: null,   mpMax: null,
             iq: 0x3B4A, speed: 0x398A, luck: 0x3990, attack: 0x3B38, defense: 0x3978),
        Make("Milo", level: 0x3B52, exp: 0x3B5A, hpCur: 0x3B22, hpMax: 0x3B2E, mpCur: 0x3B28, mpMax: 0x3B34,
             iq: 0x3B4C, speed: 0x398C, luck: 0x3992, attack: 0x3B3A, defense: 0x397A),
        Make("Pyra", level: 0x3B54, exp: 0x3B5E, hpCur: 0x3B24, hpMax: 0x3B30, mpCur: 0x3B2A, mpMax: 0x3B36,
             iq: 0x3B4E, speed: 0x398E, luck: 0x3994, attack: 0x3B3C, defense: 0x397C),
    };

    private static CharacterDef Make(string name, int level, int exp, int hpCur, int hpMax, int? mpCur, int? mpMax,
        int iq, int speed, int luck, int attack, int defense)
    {
        var o = new Dictionary<string, int>
        {
            ["level"] = level, ["exp"] = exp, ["hpCur"] = hpCur, ["hpMax"] = hpMax,
            ["iq"] = iq, ["speed"] = speed, ["luck"] = luck, ["attack"] = attack, ["defense"] = defense,
        };
        if (mpCur is int mc && mpMax is int mm)
        {
            o["mpCur"] = mc;
            o["mpMax"] = mm;
        }

        return new CharacterDef(name, o, Array.Empty<int>(), Array.Empty<int>());
    }

    public override string Subtitle(SaveState save, CharacterDef c) => c.Name switch
    {
        "Hiro" => "Fighter",
        "Milo" => "Priest",
        "Pyra" => "Wizard",
        _ => Title,
    };

    // Inventory and spell locations aren't mapped for this game yet.
    public override IReadOnlyList<SlotEntry> Items(SaveState save, CharacterDef c) => Array.Empty<SlotEntry>();
    public override IReadOnlyList<SlotEntry> Spells(SaveState save, CharacterDef c) => Array.Empty<SlotEntry>();
}
