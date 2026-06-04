namespace JFToolkit.Dxf;

/// <summary>
/// A DXF layer definition. Layers control visibility, color, and line type.
/// </summary>
public class DxfLayer
{
    public string Name { get; set; } = "0";
    public int Color { get; set; } = 7; // 7 = white/black (default)

    internal void Write(StreamWriter w)
    {
        w.WriteLine("  0\nLAYER");
        w.WriteLine($"  2\n{Name}");
        w.WriteLine($" 70\n     0");  // flags: 0 = thawed, on, not locked
        w.WriteLine($" 62\n     {Color}");
        w.WriteLine($"  6\nCONTINUOUS");
    }
}
