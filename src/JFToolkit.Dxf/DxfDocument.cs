namespace JFToolkit.Dxf;

/// <summary>
/// Represents a complete DXF drawing document.
/// </summary>
public class DxfDocument
{
    /// <summary>Block definitions (templates/symbols). Keyed by name (case-insensitive).</summary>
    public Dictionary<string, DxfBlock> Blocks { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Entities in the main drawing space.</summary>
    public List<DxfEntity> Entities { get; } = [];

    /// <summary>Layer definitions. Keyed by name.</summary>
    public Dictionary<string, DxfLayer> Layers { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Raw HEADER pairs for pass-through preservation.</summary>
    internal List<DxfPair>? RawHeader { get; set; }

    // --- Public API ---

    /// <summary>
    /// Load a DXF file from disk.
    /// </summary>
    public static DxfDocument Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"DXF file not found: {path}");

        return DxfReader.Read(path);
    }

    /// <summary>
    /// Save the document to a DXF file.
    /// </summary>
    public void Save(string path)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var writer = new StreamWriter(stream);
        Write(writer);
    }

    /// <summary>
    /// Import a block definition from another document into this one.
    /// Duplicate names are skipped (first-come wins).
    /// </summary>
    public void ImportBlock(DxfBlock block)
    {
        if (!Blocks.ContainsKey(block.Name))
            Blocks[block.Name] = block;
    }

    /// <summary>
    /// Ensure a layer exists in the document. Creates it if missing.
    /// </summary>
    public void EnsureLayer(string name, int color = 7)
    {
        if (!Layers.ContainsKey(name))
            Layers[name] = new DxfLayer { Name = name, Color = color };
    }

    // --- Write ---

    internal void Write(StreamWriter writer)
    {
        // HEADER section (pass-through or minimal)
        writer.WriteLine("  0\nSECTION");
        writer.WriteLine("  2\nHEADER");
        writer.WriteLine("  9\n$ACADVER");
        writer.WriteLine("  1\nAC1015");    // AutoCAD 2000
        if (RawHeader is not null)
        {
            var skipNext = false;
            foreach (var pair in RawHeader)
            {
                // Skip header vars we already write
                if (pair.Code == 9 && pair.Value == "$ACADVER")
                {
                    skipNext = true;
                    continue;
                }
                if (skipNext)
                {
                    skipNext = false;
                    continue;
                }
                writer.WriteLine($"{pair.Code,3}\n{pair.Value}");
            }
        }
        writer.WriteLine("  0\nENDSEC");

        // TABLES section — LAYER table
        writer.WriteLine("  0\nSECTION");
        writer.WriteLine("  2\nTABLES");

        writer.WriteLine("  0\nTABLE");
        writer.WriteLine("  2\nLAYER");
        writer.WriteLine($" 70\n     {Layers.Count}"); // max entry count

        // Always include layer "0"
        if (!Layers.ContainsKey("0"))
            Layers["0"] = new DxfLayer { Name = "0" };
        foreach (var layer in Layers.Values)
            layer.Write(writer);

        writer.WriteLine("  0\nENDTAB");
        writer.WriteLine("  0\nENDSEC");

        // BLOCKS section
        writer.WriteLine("  0\nSECTION");
        writer.WriteLine("  2\nBLOCKS");
        foreach (var block in Blocks.Values)
            block.Write(writer);
        writer.WriteLine("  0\nENDSEC");

        // ENTITIES section
        writer.WriteLine("  0\nSECTION");
        writer.WriteLine("  2\nENTITIES");
        foreach (var entity in Entities)
            entity.Write(writer);
        writer.WriteLine("  0\nENDSEC");

        // EOF
        writer.WriteLine("  0\nEOF");
    }
}
