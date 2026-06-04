# JFToolkit.Dxf

**Zero-dependency .NET library for reading and writing DXF files — focused on
block extraction and insertion. Open a template, extract blocks, paste them
into plot frames at specific positions.**

[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Target](https://img.shields.io/badge/targets-.NET%208%20%7C%20.NET%209-512bd4)]()

## The problem

You have a DXF symbol library with 100+ P&ID blocks (pumps, valves, tanks).
You have plot frames (A3, A4 with title blocks). You need to assemble
drawing sheets by pasting the right symbols at the right coordinates.

Doing this in AutoCAD by hand: open template, copy block, switch to
drawing, paste, position... for every sheet. A P&ID might have 30 sheets.

This library makes it one operation per block.

## Install

```bash
dotnet add package JFToolkit.Dxf
```

Zero dependencies. No AutoCAD SDK. Pure C# string I/O.

## Usage

### Your workflow: extract blocks, insert at coordinates

```csharp
using JFToolkit.Dxf;

// Open your symbol library and plot frame
var template = DxfDocument.Load("pandid_symbols.dxf");
var drawing  = DxfDocument.Load("a3_landscape.dxf");

// Pull out the blocks you need
drawing.ImportBlock(template.Blocks["PUMP_CENTRIFUGAL"]);
drawing.ImportBlock(template.Blocks["VALVE_GATE_50"]);
drawing.ImportBlock(template.Blocks["TANK_VERTICAL"]);

// Paste at position
drawing.Entities.Add(new DxfInsert
{
    BlockName = "PUMP_CENTRIFUGAL",
    X = 120, Y = 340
});
drawing.Entities.Add(new DxfInsert
{
    BlockName = "TANK_VERTICAL",
    X = 100, Y = 150,
    ScaleX = 2.0, ScaleY = 2.0  // double size
});

drawing.Save("sheet_03.dxf");
```

### Inspect what's in a file

```csharp
var doc = DxfDocument.Load("mystery.dxf");

Console.WriteLine($"Blocks: {doc.Blocks.Count}");
foreach (var (name, block) in doc.Blocks)
    Console.WriteLine($"  {name}: {block.Entities.Count} entities");

Console.WriteLine($"Inserts: {doc.Entities.OfType<DxfInsert>().Count()}");
```

## Supported entities (v0.1)

| Entity | Read | Write | Notes |
|--------|------|-------|-------|
| INSERT | ✅ | ✅ | Block reference at position with scale/rotation |
| LINE | ✅ | ✅ | |
| CIRCLE | ✅ | ✅ | |
| ARC | ✅ | ✅ | |
| TEXT | ✅ | ✅ | Single-line text |
| MTEXT | ✅ | — | Read-only (converted to plain text) |
| LWPOLYLINE | ✅ | ✅ | Lightweight polyline (2D vertex array) |
| BLOCK | ✅ | ✅ | Block definitions (import/export) |

## What it does NOT do (by design)

- No DIMENSION, HATCH, SPLINE, or 3D entities
- No binary DXF (text format only)
- No block creation from scratch (blocks come from templates)
- No header variable editing (pass-through only)
- No pre-R2000 DXF versions

## Requirements

- .NET 8 or .NET 9
- Any OS (pure text I/O, no Windows-specific dependencies)

## License

MIT — use it anywhere, commercial or personal.
