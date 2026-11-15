// Audits mod DLLs for member accesses legal only against publicized reference
// assemblies but illegal against the real (unpublicized) assemblies.
using Mono.Cecil;
using Mono.Cecil.Cil;

if (args.Length < 2) {
    Console.Error.WriteLine("usage: AccessAudit <probeDir> <searchDir...>");
    return 2;
}
var probeDir = args[0];
var searchDirs = args.Skip(1).ToList();

var parameters = new ReaderParameters();
var resolver = new DefaultAssemblyResolver();
foreach (var dir in searchDirs) resolver.AddSearchDirectory(dir);
parameters.AssemblyResolver = resolver;

var targetCache = new Dictionary<string, AssemblyDefinition>();
AssemblyDefinition ResolveTarget(string name) {
    if (targetCache.TryGetValue(name, out var cached)) return cached;
    var path = searchDirs.Select(d => Path.Combine(d, name + ".dll"))
                         .Where(File.Exists).FirstOrDefault();
    var asm = path == null ? null : AssemblyDefinition.ReadAssembly(path, parameters);
    targetCache[name] = asm;
    return asm;
}

int violations = 0;
var report = new List<string>();

foreach (var probePath in Directory.GetFiles(probeDir, "*.dll")) {
    var probe = AssemblyDefinition.ReadAssembly(probePath, parameters);
    report.Add($"=== {Path.GetFileName(probePath)} ===");
    foreach (var type in probe.Modules.SelectMany(m => m.GetTypes()))
      foreach (var method in type.Methods.Where(m => m.HasBody))
        foreach (var instr in method.Body.Instructions) {
          if (instr.Operand is not MemberReference and not TypeReference) continue;
          var verdict = Check(instr.Operand);
          if (verdict != null) { violations++; report.Add($"  {type.FullName}::{method.Name}: {verdict}"); }
        }
}
report.Add($"TOTAL violations: {violations}");
File.WriteAllLines(Path.Combine(probeDir, "access-audit.txt"), report);
Console.WriteLine(string.Join("\n", report));
return violations == 0 ? 0 : 1;

string Check(object operand) {
  string kind; MemberReference member; TypeReference declType;
  switch (operand) {
    case MethodReference mr: kind = "method"; member = mr; declType = mr.DeclaringType; break;
    case FieldReference fr: kind = "field"; member = fr; declType = fr.DeclaringType; break;
    case TypeReference tr: kind = "type"; member = null; declType = tr; break;
    default: return null;
  }
  var asmName = (declType.Scope as AssemblyNameReference)?.Name;
  if (asmName == null) return null;
  if (!asmName.StartsWith("Timberborn") && !asmName.StartsWith("SecondShift")) return null;
  if (ResolveTarget(asmName) == null) return null; // assembly not found in search dirs: skip
  var def = declType.Resolve();
  if (def == null) return null;
  if (!def.IsVisibleOutside()) return $"type '{declType.FullName}' not visible";
  if (member != null) {
    var memberDef = member.Resolve();
    bool memberPublic = memberDef switch {
      MethodDefinition md => md.IsPublic,
      FieldDefinition fd => fd.IsPublic,
      _ => true
    };
    if (!memberPublic) return $"{kind} '{member.FullName}' not public";
  }
  return null;
}

static class CecilExt {
  public static bool IsVisibleOutside(this TypeDefinition t) =>
      t.IsNested ? t.IsNestedPublic : t.IsPublic;
}
