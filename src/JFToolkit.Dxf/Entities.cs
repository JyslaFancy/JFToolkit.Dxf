namespace JFToolkit.Dxf;

/// <summary>
/// A tagged pair from a DXF file: group code and its value.
/// </summary>
internal readonly record struct DxfPair(int Code, string Value);

/// <summary>
/// Base class for all DXF entities.
/// </summary>
public abstract class DxfEntity
{
    public string Layer { get; set; } = "0";
    public string? Handle { get; set; }

    /// <summary>Write this entity's group codes to the output.</summary>
    internal abstract void Write(StreamWriter writer);
}

/// <summary>
/// A reference to a BLOCK inserted at a position in the drawing.
/// </summary>
public class DxfInsert : DxfEntity
{
    public string BlockName { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double ScaleX { get; set; } = 1.0;
    public double ScaleY { get; set; } = 1.0;
    public double ScaleZ { get; set; } = 1.0;
    public double Rotation { get; set; }  // degrees

    internal override void Write(StreamWriter w)
    {
        w.WriteLine("  0\nINSERT");
        if (Handle is not null) w.WriteLine($"  5\n{Handle}");
        w.WriteLine($"  8\n{Layer}");
        w.WriteLine($"  2\n{BlockName}");
        w.WriteLine($" 10\n{X}");
        w.WriteLine($" 20\n{Y}");
        w.WriteLine($" 30\n{Z}");
        if (ScaleX != 1.0) w.WriteLine($" 41\n{ScaleX}");
        if (ScaleY != 1.0) w.WriteLine($" 42\n{ScaleY}");
        if (ScaleZ != 1.0) w.WriteLine($" 43\n{ScaleZ}");
        if (Rotation != 0.0) w.WriteLine($" 50\n{Rotation}");
    }
}

/// <summary>
/// A line from (X1,Y1) to (X2,Y2).
/// </summary>
public class DxfLine : DxfEntity
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }

    internal override void Write(StreamWriter w)
    {
        w.WriteLine("  0\nLINE");
        if (Handle is not null) w.WriteLine($"  5\n{Handle}");
        w.WriteLine($"  8\n{Layer}");
        w.WriteLine($" 10\n{X1}");
        w.WriteLine($" 20\n{Y1}");
        w.WriteLine($" 11\n{X2}");
        w.WriteLine($" 21\n{Y2}");
    }
}

/// <summary>
/// A circle defined by center point and radius.
/// </summary>
public class DxfCircle : DxfEntity
{
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Radius { get; set; }

    internal override void Write(StreamWriter w)
    {
        w.WriteLine("  0\nCIRCLE");
        if (Handle is not null) w.WriteLine($"  5\n{Handle}");
        w.WriteLine($"  8\n{Layer}");
        w.WriteLine($" 10\n{CenterX}");
        w.WriteLine($" 20\n{CenterY}");
        w.WriteLine($" 40\n{Radius}");
    }
}

/// <summary>
/// An arc (partial circle) defined by center, radius, start/end angles.
/// </summary>
public class DxfArc : DxfEntity
{
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Radius { get; set; }
    public double StartAngle { get; set; }  // degrees
    public double EndAngle { get; set; }    // degrees

    internal override void Write(StreamWriter w)
    {
        w.WriteLine("  0\nARC");
        if (Handle is not null) w.WriteLine($"  5\n{Handle}");
        w.WriteLine($"  8\n{Layer}");
        w.WriteLine($" 10\n{CenterX}");
        w.WriteLine($" 20\n{CenterY}");
        w.WriteLine($" 40\n{Radius}");
        w.WriteLine($" 50\n{StartAngle}");
        w.WriteLine($" 51\n{EndAngle}");
    }
}

/// <summary>
/// A single-line text entity.
/// </summary>
public class DxfText : DxfEntity
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Height { get; set; } = 2.5;
    public string Value { get; set; } = "";
    public double Rotation { get; set; }

    internal override void Write(StreamWriter w)
    {
        w.WriteLine("  0\nTEXT");
        if (Handle is not null) w.WriteLine($"  5\n{Handle}");
        w.WriteLine($"  8\n{Layer}");
        w.WriteLine($" 10\n{X}");
        w.WriteLine($" 20\n{Y}");
        w.WriteLine($" 40\n{Height}");
        w.WriteLine($"  1\n{Value}");
        if (Rotation != 0.0) w.WriteLine($" 50\n{Rotation}");
    }
}

/// <summary>
/// A lightweight polyline (2D, vertex array).
/// </summary>
public class DxfLwPolyline : DxfEntity
{
    public List<LwPolylineVertex> Vertices { get; set; } = [];
    public bool IsClosed { get; set; }

    internal override void Write(StreamWriter w)
    {
        w.WriteLine("  0\nLWPOLYLINE");
        if (Handle is not null) w.WriteLine($"  5\n{Handle}");
        w.WriteLine($"  8\n{Layer}");
        w.WriteLine($" 90\n{Vertices.Count}");
        w.WriteLine($" 70\n{(IsClosed ? 1 : 0)}");

        foreach (var v in Vertices)
        {
            w.WriteLine($" 10\n{v.X}");
            w.WriteLine($" 20\n{v.Y}");
            if (v.Bulge != 0.0) w.WriteLine($" 42\n{v.Bulge}");
        }
    }
}

public record struct LwPolylineVertex(double X, double Y, double Bulge = 0.0);
