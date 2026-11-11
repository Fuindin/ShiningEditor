using System.Text;

namespace ShiningEditor.Game;

/// <summary>
/// A Kega Fusion save state held entirely in memory. Every edit made in the UI
/// is written into <see cref="Data"/> straight away, so changes to any number of
/// characters accumulate here until <see cref="Save"/> flushes them to disk in a
/// single write. Multi-byte values are big-endian (68000 byte order).
/// </summary>
internal sealed class SaveState
{
    // Kega Fusion save states begin with the ASCII signature "GST". The file size
    // distinguishes the console: Genesis (SitD / SF / SF2) vs Sega CD (SFCD).
    private static readonly byte[] GstMagic = { 0x47, 0x53, 0x54 };
    public const long GenesisSize = 140408;
    public const long SegaCdSize = 1120235;

    private long _diskLength;
    private DateTime _diskWriteTimeUtc;
    private bool _backupMade;

    public string Path { get; }
    public byte[] Data { get; }
    public GameDefinition Game { get; }

    /// <summary>Path of the .bak written before the first save this session, if any.</summary>
    public string? BackupPath { get; private set; }

    private SaveState(string path, byte[] data, GameDefinition game)
    {
        Path = path;
        Data = data;
        Game = game;
        var info = new FileInfo(path);
        _diskLength = info.Length;
        _diskWriteTimeUtc = info.LastWriteTimeUtc;
    }

    /// <summary>Loads and validates a save state for <paramref name="game"/>.</summary>
    /// <exception cref="InvalidDataException">The file is not a matching Fusion save state.</exception>
    public static SaveState Open(string path, GameDefinition game)
    {
        byte[] data = File.ReadAllBytes(path);

        if (data.Length < GstMagic.Length || !data.AsSpan(0, GstMagic.Length).SequenceEqual(GstMagic))
        {
            throw new InvalidDataException(
                "This is not a Kega Fusion save state (its header is missing the \"GST\" signature).");
        }

        long expected = game.IsSegaCd ? SegaCdSize : GenesisSize;
        if (data.Length != expected)
        {
            string console = game.IsSegaCd ? "Sega CD" : "Genesis";
            throw new InvalidDataException(
                $"This does not look like a {console} save state for {game.Title}.\n\n" +
                $"Expected size: {expected:N0} bytes\n" +
                $"Selected file: {data.Length:N0} bytes\n\n" +
                "Make sure you picked a Kega Fusion save state for this game.");
        }

        return new SaveState(path, data, game);
    }

    // ---- Reads / writes ---------------------------------------------------------

    public bool InRange(int offset, int width) => offset >= 0 && width > 0 && offset + width <= Data.Length;

    /// <summary>Reads an unsigned big-endian value of 1–4 bytes.</summary>
    public long Read(int offset, int width)
    {
        if (!InRange(offset, width))
        {
            return 0;
        }

        long v = 0;
        for (int i = 0; i < width; i++)
        {
            v = (v << 8) | Data[offset + i];
        }

        return v;
    }

    /// <summary>Writes an unsigned big-endian value of 1–4 bytes. Returns true if any byte changed.</summary>
    public bool Write(int offset, int width, long value)
    {
        if (!InRange(offset, width))
        {
            Log.Write($"Refused out-of-range write at offset 0x{offset:X} ({width} bytes).");
            return false;
        }

        long max = MaxFor(width);
        value = Math.Clamp(value, 0, max);
        bool changed = false;
        for (int i = width - 1; i >= 0; i--)
        {
            byte b = (byte)(value & 0xFF);
            if (Data[offset + i] != b)
            {
                Data[offset + i] = b;
                changed = true;
            }

            value >>= 8;
        }

        return changed;
    }

    public byte Byte(int offset) => offset >= 0 && offset < Data.Length ? Data[offset] : (byte)0;

    /// <summary>Reads a zero-terminated ASCII string of at most <paramref name="length"/> bytes.</summary>
    public string Ascii(int offset, int length)
    {
        if (!InRange(offset, length))
        {
            return "";
        }

        var span = Data.AsSpan(offset, length);
        int end = span.IndexOf((byte)0);
        if (end >= 0)
        {
            span = span[..end];
        }

        return Encoding.ASCII.GetString(span).Trim();
    }

    public static long MaxFor(int width) => width >= 8 ? long.MaxValue : (1L << (8 * width)) - 1;

    // ---- Disk -------------------------------------------------------------------

    /// <summary>
    /// True when the file on disk has changed since it was loaded or last saved —
    /// e.g. the emulator wrote a new state to the same slot while the editor was open.
    /// </summary>
    public bool ChangedOnDisk()
    {
        try
        {
            var info = new FileInfo(Path);
            return !info.Exists || info.Length != _diskLength || info.LastWriteTimeUtc != _diskWriteTimeUtc;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Writes every pending edit back to the original file. Before the first save of
    /// the session the untouched file is copied to <c>&lt;file&gt;.bak</c>.
    /// </summary>
    public void Save()
    {
        if (!_backupMade && File.Exists(Path))
        {
            string bak = Path + ".bak";
            File.Copy(Path, bak, overwrite: true);
            BackupPath = bak;
            _backupMade = true;
        }

        File.WriteAllBytes(Path, Data);

        var info = new FileInfo(Path);
        _diskLength = info.Length;
        _diskWriteTimeUtc = info.LastWriteTimeUtc;
    }
}

/// <summary>Best-effort error log next to the executable (viewable from Help ▸ View Error Log).</summary>
internal static class Log
{
    public static string FilePath => System.IO.Path.Combine(AppContext.BaseDirectory, "errorlog.txt");

    public static void Write(string message)
    {
        try
        {
            File.AppendAllText(FilePath, $"{message} Added: {DateTime.Now}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never throw.
        }
    }
}
