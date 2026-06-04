using System.Globalization;

namespace JFToolkit.Dxf;

/// <summary>
/// Reads DXF files into <see cref="DxfDocument"/>.
/// Supports text-format DXF R2000+.
/// </summary>
internal static class DxfReader
{
    // Simple pushback: store lines that were read ahead
    private static readonly Queue<string> PushbackBuffer = new();

    private static void PushBack(string codeLine, string valueLine)
    {
        PushbackBuffer.Enqueue(codeLine);
        PushbackBuffer.Enqueue(valueLine);
    }

    private static string? ReadLine(StreamReader reader)
    {
        if (PushbackBuffer.Count > 0)
            return PushbackBuffer.Dequeue();
        return reader.ReadLine();
    }
    public static DxfDocument Read(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(stream);

        var doc = new DxfDocument();

        // State machine: track which section we're in
        string? currentSection = null;
        List<DxfEntity>? currentBlockEntities = null;
        string? currentBlockName = null;
        double blockBaseX = 0, blockBaseY = 0;

        while (TryReadPair(reader, out var pair))
        {
            // Section boundaries
            if (pair is { Code: 0, Value: "SECTION" })
            {
                if (TryReadPair(reader, out var name) && name.Code == 2)
                    currentSection = name.Value;
                continue;
            }

            if (pair is { Code: 0, Value: "ENDSEC" })
            {
                currentSection = null;
                continue;
            }

            if (pair is { Code: 0, Value: "EOF" })
                break;

            switch (currentSection)
            {
                case "HEADER":
                    doc.RawHeader ??= new List<DxfPair>();
                    doc.RawHeader.Add(pair);
                    // Also capture the next pair if this was a group code
                    if (pair.Code != 0)
                    {
                        if (TryReadPair(reader, out var valPair))
                            doc.RawHeader.Add(valPair);
                    }
                    break;

                case "TABLES":
                    // Pass-through: capture layer names from LAYER table
                    if (pair is { Code: 0, Value: "LAYER" })
                    {
                        var layer = ParseLayer(reader);
                        if (layer is not null)
                            doc.Layers[layer.Name] = layer;
                    }
                    break;

                case "BLOCKS":
                    if (pair is { Code: 0, Value: "BLOCK" })
                    {
                        // Start of a block definition
                        currentBlockEntities = [];
                        currentBlockName = null;
                        blockBaseX = blockBaseY = 0;
                    }
                    else if (pair is { Code: 0, Value: "ENDBLK" })
                    {
                        // End of block definition
                        if (currentBlockName is not null && currentBlockEntities is not null)
                        {
                            doc.Blocks[currentBlockName] = new DxfBlock
                            {
                                Name = currentBlockName,
                                BaseX = blockBaseX,
                                BaseY = blockBaseY,
                                Entities = currentBlockEntities
                            };
                        }
                        currentBlockEntities = null;
                        currentBlockName = null;
                    }
                    else if (currentBlockEntities is not null)
                    {
                        if (pair.Code == 2) // block name
                        {
                            currentBlockName = pair.Value;
                        }
                        else if (pair.Code == 10)
                        {
                            blockBaseX = ParseDouble(pair.Value);
                        }
                        else if (pair.Code == 20)
                        {
                            blockBaseY = ParseDouble(pair.Value);
                        }
                        else if (pair.Code == 0) // entity inside block
                        {
                            var entity = ParseEntity(pair.Value, reader);
                            if (entity is not null)
                                currentBlockEntities.Add(entity);
                        }
                    }
                    break;

                case "ENTITIES":
                    if (pair.Code == 0) // start of an entity
                    {
                        var entity = ParseEntity(pair.Value, reader);
                        if (entity is not null)
                            doc.Entities.Add(entity);
                    }
                    break;
            }
        }

        return doc;
    }

    private static DxfEntity? ParseEntity(string type, StreamReader reader)
    {
        // Collect all pairs for this entity until next 0 code
        var pairs = new List<DxfPair>();
        while (true)
        {
            // Peek: read the next line, check if it's code 0
            var codeLine = ReadLine(reader);
            if (codeLine is null) break;

            var valueLine = ReadLine(reader);
            if (valueLine is null) break;

            if (!int.TryParse(codeLine.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
                break;

            if (code == 0)
            {
                // This is the next entity — push back both lines
                PushBack(codeLine, valueLine);
                break;
            }

            pairs.Add(new DxfPair(code, valueLine));
        }

        return type.ToUpperInvariant() switch
        {
            "INSERT" => ParseInsert(pairs),
            "LINE" => ParseLine(pairs),
            "CIRCLE" => ParseCircle(pairs),
            "ARC" => ParseArc(pairs),
            "TEXT" => ParseText(pairs),
            "LWPOLYLINE" => ParseLwPolyline(pairs),
            "MTEXT" => ParseMText(pairs),   // pass-through as LwPolyline-like; we capture the text
            _ => null // skip unknown entity types
        };
    }

    private static DxfInsert ParseInsert(List<DxfPair> pairs)
    {
        var insert = new DxfInsert();
        foreach (var p in pairs)
        {
            switch (p.Code)
            {
                case 2: insert.BlockName = p.Value; break;
                case 8: insert.Layer = p.Value; break;
                case 5: insert.Handle = p.Value; break;
                case 10: insert.X = ParseDouble(p.Value); break;
                case 20: insert.Y = ParseDouble(p.Value); break;
                case 30: insert.Z = ParseDouble(p.Value); break;
                case 41: insert.ScaleX = ParseDouble(p.Value); break;
                case 42: insert.ScaleY = ParseDouble(p.Value); break;
                case 43: insert.ScaleZ = ParseDouble(p.Value); break;
                case 50: insert.Rotation = ParseDouble(p.Value); break;
            }
        }
        return insert;
    }

    private static DxfLine ParseLine(List<DxfPair> pairs)
    {
        var line = new DxfLine();
        foreach (var p in pairs)
        {
            switch (p.Code)
            {
                case 8: line.Layer = p.Value; break;
                case 5: line.Handle = p.Value; break;
                case 10: line.X1 = ParseDouble(p.Value); break;
                case 20: line.Y1 = ParseDouble(p.Value); break;
                case 11: line.X2 = ParseDouble(p.Value); break;
                case 21: line.Y2 = ParseDouble(p.Value); break;
            }
        }
        return line;
    }

    private static DxfCircle ParseCircle(List<DxfPair> pairs)
    {
        var circle = new DxfCircle();
        foreach (var p in pairs)
        {
            switch (p.Code)
            {
                case 8: circle.Layer = p.Value; break;
                case 5: circle.Handle = p.Value; break;
                case 10: circle.CenterX = ParseDouble(p.Value); break;
                case 20: circle.CenterY = ParseDouble(p.Value); break;
                case 40: circle.Radius = ParseDouble(p.Value); break;
            }
        }
        return circle;
    }

    private static DxfArc ParseArc(List<DxfPair> pairs)
    {
        var arc = new DxfArc();
        foreach (var p in pairs)
        {
            switch (p.Code)
            {
                case 8: arc.Layer = p.Value; break;
                case 5: arc.Handle = p.Value; break;
                case 10: arc.CenterX = ParseDouble(p.Value); break;
                case 20: arc.CenterY = ParseDouble(p.Value); break;
                case 40: arc.Radius = ParseDouble(p.Value); break;
                case 50: arc.StartAngle = ParseDouble(p.Value); break;
                case 51: arc.EndAngle = ParseDouble(p.Value); break;
            }
        }
        return arc;
    }

    private static DxfText ParseText(List<DxfPair> pairs)
    {
        var text = new DxfText();
        foreach (var p in pairs)
        {
            switch (p.Code)
            {
                case 8: text.Layer = p.Value; break;
                case 5: text.Handle = p.Value; break;
                case 1: text.Value = p.Value; break;
                case 10: text.X = ParseDouble(p.Value); break;
                case 20: text.Y = ParseDouble(p.Value); break;
                case 40: text.Height = ParseDouble(p.Value); break;
                case 50: text.Rotation = ParseDouble(p.Value); break;
            }
        }
        return text;
    }

    private static DxfText ParseMText(List<DxfPair> pairs)
    {
        // Treat MTEXT as regular text for pass-through
        var text = new DxfText();
        foreach (var p in pairs)
        {
            switch (p.Code)
            {
                case 8: text.Layer = p.Value; break;
                case 5: text.Handle = p.Value; break;
                case 1: text.Value = StripMTextFormatting(p.Value); break;
                case 10: text.X = ParseDouble(p.Value); break;
                case 20: text.Y = ParseDouble(p.Value); break;
                case 40: text.Height = ParseDouble(p.Value); break;
                case 50: text.Rotation = ParseDouble(p.Value); break;
            }
        }
        return text;
    }

    private static string StripMTextFormatting(string mtext)
    {
        // Remove common MText formatting codes like \A1;\fArial|b0|i0;
        // Keep the text content. This is a simple pass — handles typical cases.
        var result = mtext;
        // Strip formatting sequences like \f...|...;
        while (true)
        {
            var start = result.IndexOf("\\f", StringComparison.Ordinal);
            if (start < 0) break;
            var end = result.IndexOf(';', start);
            if (end < 0) break;
            result = result.Remove(start, end - start + 1);
        }
        // Strip \A1; style alignment codes
        result = result.Replace("\\A1;", "").Replace("\\A0;", "");
        // Handle paragraph breaks
        result = result.Replace("\\P", "\n");
        return result.Trim();
    }

    private static DxfLwPolyline ParseLwPolyline(List<DxfPair> pairs)
    {
        var pline = new DxfLwPolyline();
        int vertexCount = 0;
        double currentX = 0, currentY = 0, currentBulge = 0;

        foreach (var p in pairs)
        {
            switch (p.Code)
            {
                case 8: pline.Layer = p.Value; break;
                case 5: pline.Handle = p.Value; break;
                case 70: pline.IsClosed = (int.TryParse(p.Value, out var f) && (f & 1) != 0); break;
                case 90: vertexCount = int.TryParse(p.Value, out var vc) ? vc : 0; break;
                case 10:
                    // New vertex starting — if we already have one, store it
                    if (currentX != 0 || currentY != 0)
                    {
                        pline.Vertices.Add(new LwPolylineVertex(currentX, currentY, currentBulge));
                        currentBulge = 0;
                    }
                    currentX = ParseDouble(p.Value);
                    break;
                case 20:
                    currentY = ParseDouble(p.Value);
                    break;
                case 42:
                    currentBulge = ParseDouble(p.Value);
                    break;
            }
        }

        // Store the last vertex
        if (vertexCount > 0 || currentX != 0 || currentY != 0)
            pline.Vertices.Add(new LwPolylineVertex(currentX, currentY, currentBulge));

        return pline;
    }

    private static DxfLayer? ParseLayer(StreamReader reader)
    {
        var pairs = new List<DxfPair>();
        // Read pairs until next 0 code
        while (true)
        {
            var codeLine = ReadLine(reader);
            if (codeLine is null) break;

            var valueLine = ReadLine(reader);
            if (valueLine is null) break;

            if (!int.TryParse(codeLine.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
                break;

            if (code == 0)
            {
                PushBack(codeLine, valueLine);
                break;
            }

            pairs.Add(new DxfPair(code, valueLine));
        }

        var layer = new DxfLayer();
        foreach (var p in pairs)
        {
            switch (p.Code)
            {
                case 2: layer.Name = p.Value; break;
                case 62: layer.Color = int.TryParse(p.Value, out var c) ? c : 7; break;
            }
        }
        return layer;
    }

    // --- Low-level I/O ---

    private static bool TryReadPair(StreamReader reader, out DxfPair pair)
    {
        pair = default;
        var codeLine = ReadLine(reader);
        if (codeLine is null) return false;

        var valueLine = ReadLine(reader);
        if (valueLine is null) return false;

        if (!int.TryParse(codeLine.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
            return false;

        pair = new DxfPair(code, valueLine);
        return true;
    }

    private static double ParseDouble(string s)
    {
        if (double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            return d;
        return 0;
    }
}
