// Makes all types/members public in the input assembly, writes to output dir.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

var (inPath, outDir) = (args[0], args[1]);
if (args.Length > 0 && args[0] == "--list")
{
    var asm2 = AssemblyDefinition.ReadAssembly(args[1]);
    foreach (var t in asm2.MainModule.Types)
        Console.WriteLine($"{t.FullName} IsPublic={t.IsPublic} IsNestedPublic={t.IsNestedPublic}");
    return;
}
Directory.CreateDirectory(outDir);

var rp = new ReaderParameters { ReadWrite = false };
var asm = AssemblyDefinition.ReadAssembly(inPath, rp);

foreach (var type in asm.MainModule.Types)
{
    MakePublic(type);
    foreach (var nested in type.NestedTypes) MakePublic(nested);
}

void MakePublic(TypeDefinition t)
{
    if (t.IsNested)
    {
        t.IsNestedPublic = true;
    }
    else
    {
        t.IsPublic = true;
    }
    var eventNames = new HashSet<string>(t.Events.Select(e => e.Name));
    foreach (var m in t.Methods)
        if (!m.IsPublic) m.IsPublic = true;
    foreach (var f in t.Fields)
        if (!f.IsPublic && !eventNames.Contains(f.Name)) f.IsPublic = true;
    foreach (var nested in t.NestedTypes) MakePublic(nested);
}

var outPath = Path.Combine(outDir, Path.GetFileName(inPath));
asm.Write(outPath);
Console.WriteLine($"Publicized {inPath} -> {outPath}");
