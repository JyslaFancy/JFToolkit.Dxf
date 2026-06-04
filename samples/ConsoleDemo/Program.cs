using JFToolkit.Dxf;

// ──────────────────────────────────────────────
// JFToolkit.Dxf — Console Demo
// Shows the block extraction + insertion workflow
// ──────────────────────────────────────────────

Console.WriteLine("JFToolkit.Dxf Demo");
Console.WriteLine("==================");
Console.WriteLine();

// ── Workflow 1: Your exact use case ──
Console.WriteLine("--- Block extraction + insertion ---");

var template = DxfDocument.Load(@"C:\Symbols\pandid_symbols.dxf");
var drawing  = DxfDocument.Load(@"C:\Templates\a3_landscape.dxf");

// Extract blocks from the symbol library
var pumpBlock  = template.Blocks["PUMP_CENTRIFUGAL"];
var valveBlock = template.Blocks["VALVE_GATE_50"];
var tankBlock  = template.Blocks["TANK_VERTICAL_1000L"];

Console.WriteLine($"Template contains {template.Blocks.Count} blocks:");
foreach (var name in template.Blocks.Keys)
    Console.WriteLine($"  → {name} ({template.Blocks[name].Entities.Count} entities)");

// Copy them into the drawing
drawing.ImportBlock(pumpBlock);
drawing.ImportBlock(valveBlock);
drawing.ImportBlock(tankBlock);

// Paste at specific positions (Norwegian P&ID coordinates)
drawing.Entities.Add(new DxfInsert { BlockName = "PUMP_CENTRIFUGAL",   X = 120, Y = 340 });
drawing.Entities.Add(new DxfInsert { BlockName = "PUMP_CENTRIFUGAL",   X = 320, Y = 340 });
drawing.Entities.Add(new DxfInsert { BlockName = "VALVE_GATE_50",      X = 215, Y = 310 });
drawing.Entities.Add(new DxfInsert { BlockName = "TANK_VERTICAL_1000L",X = 100, Y = 150 });
drawing.Entities.Add(new DxfInsert { BlockName = "TANK_VERTICAL_1000L",X = 300, Y = 150 });

drawing.Save(@"C:\Output\pandid_sheet_03.dxf");
Console.WriteLine("✓ Saved pandid_sheet_03.dxf");

// ── Workflow 2: Read back and inspect ──
Console.WriteLine();
Console.WriteLine("--- Read-back ---");
var loaded = DxfDocument.Load(@"C:\Output\pandid_sheet_03.dxf");
Console.WriteLine($"Blocks: {loaded.Blocks.Count}");
Console.WriteLine($"Entities: {loaded.Entities.Count}");
Console.WriteLine($"Layers: {loaded.Layers.Count}");

foreach (var entity in loaded.Entities)
{
    if (entity is DxfInsert ins)
        Console.WriteLine($"  INSERT {ins.BlockName} at ({ins.X}, {ins.Y})");
}

// ── Workflow 3: Rotated/scaled insert ──
Console.WriteLine();
Console.WriteLine("--- Scaled + rotated ---");
drawing = DxfDocument.Load(@"C:\Templates\a3_landscape.dxf");
drawing.ImportBlock(valveBlock);

drawing.Entities.Add(new DxfInsert
{
    BlockName = "VALVE_GATE_50",
    X = 400, Y = 200,
    Rotation = 90,    // rotated 90° (vertical pipe)
    ScaleX = 1.5,
    ScaleY = 1.5       // 1.5x size (DN80 on DN50 symbol)
});

drawing.Save(@"C:\Output\valve_scaled.dxf");
Console.WriteLine("✓ Saved valve_scaled.dxf");

Console.WriteLine();
Console.WriteLine("Done.");
