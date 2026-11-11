using System.Text;

namespace ShiningEditor.Game;

/// <summary>
/// Shining Force CD (Sega CD). Up to 27 force slots of 0x38 bytes from 0xD522; ally
/// names are read from the save itself, and item names depend on the current book.
/// </summary>
internal sealed class ShiningForceCDGame : GameDefinition
{
    private const int TableBase = 0xD522;
    private const int SlotSize = 0x38;
    private const int SlotCount = 27;
    private const int PlayerNameOffset = 0xD518;
    private const int NameLength = 10;
    private const int BookOffset = 0xE0E1;

    private Dictionary<byte, string> _itemNames = new();
    private List<string> _classTable = new();
    private int _book;

    public override string Title => "Shining Force CD";
    public override string MenuText => "Shining Force &CD";
    public override bool IsSegaCd => true;
    public override int GoldOffset => 0xE078;
    public override int GoldWidth => 4;
    public override string CoverResource => "covers.sfcd.jpg";

    // Crimson of the Sega CD box art.
    public override GameAccent Accent { get; } =
        new(Color.FromArgb(0xB8, 0x30, 0x40), Color.FromArgb(0x48, 0x0C, 0x18), Color.FromArgb(0xFF, 0xA8, 0xB0));

    public override IReadOnlyList<FieldDef> Fields { get; } = new FieldDef[]
    {
        new("level",      "LV",       "Level",                    1, Editable: false),
        new("exp",        "EXP",      "Experience (100 = level up)", 1),
        new("hpCur",      "HP",       "Current hit points",       2),
        new("hpMax",      "MAX HP",   "Maximum hit points",       2),
        new("mpCur",      "MP",       "Current magic points",     1),
        new("mpMax",      "MAX MP",   "Maximum magic points",     1),
        new("attackBase", "ATTACK",   "Base attack",              1),
        new("attackEq",   "ATTACK+",  "Attack with equipment",    1),
        new("defenseBase","DEFENSE",  "Base defense",             1),
        new("defenseEq",  "DEFENSE+", "Defense with equipment",   1),
        new("agilityBase","AGILITY",  "Base agility",             1),
        new("agilityEq",  "AGILITY+", "Agility with equipment",   1),
        new("moveBase",   "MOVE",     "Base movement",            1),
        new("moveEq",     "MOVE+",    "Movement with equipment",  1),
    };

    public override string? MagicNote => "Spells are shown by their raw id; level groups follow the usual Shining encoding.";

    public override void Prepare(SaveState save)
    {
        byte raw = save.Byte(BookOffset);
        _book = raw is >= 1 and <= 4 ? raw : 0;
        int group = _book switch { 1 or 2 => 1, 3 or 4 => 2, _ => 0 };
        _itemNames = ItemTables.BuildShiningForceCDItems(group);
        _classTable = LoadClassTable(save.Data);
    }

    public override string? InfoLine(SaveState save) => _book switch
    {
        1 => "Book 1: Towards the Root of Evil",
        2 => "Book 2: The Evil God Awakes",
        3 => "Book 3: A New Challenge",
        4 => "Book 4: The Last Battle",
        _ => "Unknown book",
    };

    public override IReadOnlyList<CharacterDef> BuildCharacters(SaveState save)
    {
        var list = new List<CharacterDef>();
        string hero = save.Ascii(PlayerNameOffset, NameLength);
        list.Add(Make(string.IsNullOrWhiteSpace(hero) ? "HERO" : $"{hero} (Hero)", TableBase));

        // A slot's name is stored 0x0A bytes before its stat block.
        for (int slot = 1; slot < SlotCount; slot++)
        {
            int b = TableBase + slot * SlotSize;
            string name = save.Ascii(b - 0x0A, NameLength);
            if (!string.IsNullOrWhiteSpace(name))
            {
                list.Add(Make(name, b));
            }
        }

        return list;
    }

    private static CharacterDef Make(string name, int b)
    {
        var o = new Dictionary<string, int>
        {
            ["class"] = b + 0x00, ["level"] = b + 0x01,
            ["hpMax"] = b + 0x02, ["hpCur"] = b + 0x04,
            ["mpMax"] = b + 0x06, ["mpCur"] = b + 0x07,
            ["attackBase"] = b + 0x08, ["attackEq"] = b + 0x09,
            ["defenseBase"] = b + 0x0A, ["defenseEq"] = b + 0x0B,
            ["agilityBase"] = b + 0x0C, ["agilityEq"] = b + 0x0D,
            ["moveBase"] = b + 0x0E, ["moveEq"] = b + 0x0F,
            ["exp"] = b + 0x26,
        };

        // Items: the pair at +0x14..0x15 is skipped; the four slot ids follow.
        return new CharacterDef(name, o,
            new[] { b + 0x17, b + 0x19, b + 0x1B, b + 0x1D },
            new[] { b + 0x1E, b + 0x1F, b + 0x20, b + 0x21 },
            b);
    }

    public override string Subtitle(SaveState save, CharacterDef c)
    {
        int id = save.Byte(c.BaseOffset);
        if (_classTable.Count == 0)
        {
            return "Class ????";
        }

        return id < _classTable.Count ? $"Class {_classTable[id]}" : $"Class ID {id}";
    }

    public override IReadOnlyList<SlotEntry> Items(SaveState save, CharacterDef c)
    {
        var list = new List<SlotEntry>();
        foreach (int off in c.ItemOffsets)
        {
            byte raw = save.Byte(off);
            if (raw is 0xFF or 0x7F)
            {
                continue;
            }

            bool equipped = (raw & 0x80) != 0;
            byte baseId = (byte)(raw & 0x7F);
            if (_itemNames.TryGetValue(baseId, out string? name))
            {
                list.Add(new SlotEntry(raw, name, IsEmptyName(name), Equipped: equipped));
            }
            else if (_itemNames.TryGetValue(raw, out string? rawName))
            {
                list.Add(new SlotEntry(raw, rawName, IsEmptyName(rawName)));
            }
            else
            {
                list.Add(new SlotEntry(raw, $"Unknown item ({Hex2(raw)})", false, Unknown: true));
            }
        }

        return list;
    }

    public override IReadOnlyList<SlotEntry> Spells(SaveState save, CharacterDef c)
    {
        var list = new List<SlotEntry>();
        foreach (int off in c.MagicOffsets)
        {
            int raw = save.Byte(off);
            if (raw == 0x3F)
            {
                continue;
            }

            list.Add(ItemTables.ShiningForceCDMagic.TryGetValue(raw, out string? name)
                ? new SlotEntry(raw, name, IsEmptyName(name))
                : new SlotEntry(raw, $"Unknown magic ({Hex2(raw)})", false, Unknown: true));
        }

        return list;
    }

    // ---- Class table: length-prefixed strings found by signature in RAM ---------

    private static List<string> LoadClassTable(byte[] all)
    {
        // 04 "SDMN" 04 "HERO" 04 "KNTE" 04 "PLDN"
        byte[] sig =
        {
            0x04, (byte)'S', (byte)'D', (byte)'M', (byte)'N',
            0x04, (byte)'H', (byte)'E', (byte)'R', (byte)'O',
            0x04, (byte)'K', (byte)'N', (byte)'T', (byte)'E',
            0x04, (byte)'P', (byte)'L', (byte)'D', (byte)'N',
        };

        int start = all.AsSpan().IndexOf(sig);
        var list = new List<string>();
        if (start < 0)
        {
            return list;
        }

        int offset = start;
        for (int i = 0; i < 200 && offset < all.Length; i++)
        {
            byte len = all[offset];
            if (len == 0 || len > 20 || offset + 1 + len > all.Length)
            {
                break;
            }

            string s = Encoding.ASCII.GetString(all, offset + 1, len).TrimEnd('\0', ' ');
            if (string.IsNullOrWhiteSpace(s))
            {
                break;
            }

            list.Add(s);
            offset += 1 + len;
        }

        return list;
    }
}
