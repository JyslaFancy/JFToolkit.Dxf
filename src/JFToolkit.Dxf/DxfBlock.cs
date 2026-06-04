namespace JFToolkit.Dxf;

/// <summary>
/// A block definition — a named collection of entities that can be
/// inserted into a drawing via <see cref="DxfInsert"/>.
/// </summary>
public class DxfBlock
{
    /// <summary>Block name (case-insensitive in AutoCAD).</summary>
    public string Name { get; set; } = "";

    /// <summary>Base/insertion point of the block.</summary>
    public double BaseX { get; set; }
    public double BaseY { get; set; }

    /// <summary>Layer the block definition lives on.</summary>
    public string Layer { get; set; } = "0";

    /// <summary>Entities that make up this block.</summary>
    public List<DxfEntity> Entities { get; set; } = [];

    internal void Write(StreamWriter w)
    {
        w.WriteLine("  0\nBLOCK");
        w.WriteLine($"  8\n{Layer}");
        w.WriteLine($"  2\n{Name}");
        w.WriteLine($" 70\n     0");  // flags: 0 = non-attribute block
        w.WriteLine($" 10\n{BaseX}");
        w.WriteLine($" 20\n{BaseY}");
        w.WriteLine($"  3\n{Name}");  // block name repeated
        w.WriteLine("  1\n");          // xref path (empty)

        foreach (var entity in Entities)
            entity.Write(w);

        w.WriteLine("  0\nENDBLK");
    }
}
