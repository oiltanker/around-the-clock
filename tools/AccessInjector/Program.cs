// Injects [assembly: IgnoresAccessChecksTo("<game assembly>")] for every
// referenced Timberborn.*/AroundTheClock.* assembly, defining the attribute type
// locally. All injected references must reuse the target module's own core
// library scope — importing from the writing runtime leaks System.Private.CoreLib.
// Usage: AccessInjector <dll> [dll...]
using Mono.Cecil;
using Mono.Cecil.Cil;

foreach (var path in args) {
    var bytes = File.ReadAllBytes(path);
    var asm = AssemblyDefinition.ReadAssembly(new MemoryStream(bytes));
    var module = asm.MainModule;

    // Core-library assembly ref already used by this module (mscorlib or netstandard).
    var coreRef = module.AssemblyReferences.FirstOrDefault(r => r.Name == "mscorlib")
                  ?? module.AssemblyReferences.FirstOrDefault(r => r.Name == "netstandard");
    if (coreRef == null) throw new InvalidOperationException($"{path}: no core assembly reference found");

    TypeReference objectType = module.GetTypeReferences().FirstOrDefault(t => t.FullName == "System.Object")
        ?? new TypeReference("System", "Object", module, coreRef, false);

    // Define the attribute type if missing.
    var attrType = module.GetType("System.Runtime.CompilerServices", "IgnoresAccessChecksToAttribute");
    if (attrType == null) {
        attrType = new TypeDefinition(
            "System.Runtime.CompilerServices", "IgnoresAccessChecksToAttribute",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            objectType);
        var field = new FieldDefinition("_assemblyName", FieldAttributes.Private, module.TypeSystem.String);
        var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
        ctor.Parameters.Add(new ParameterDefinition("assemblyName", ParameterAttributes.None, module.TypeSystem.String));
        // object ctor with the same core-library scope
        var objCtor = new MethodReference(".ctor", module.TypeSystem.Void, objectType) { HasThis = true };
        var il = ctor.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, objCtor);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, field);
        il.Emit(OpCodes.Ret);
        attrType.Fields.Add(field);
        attrType.Methods.Add(ctor);
        module.Types.Add(attrType);
    }

    var attrCtor = module.ImportReference(attrType.Methods.Single(c => c.IsConstructor));

    var targets = asm.MainModule.AssemblyReferences
        .Where(r => r.Name.StartsWith("Timberborn") || r.Name.StartsWith("AroundTheClock"))
        .Select(r => r.Name)
        .Distinct()
        .ToList();

    foreach (var target in targets) {
        if (asm.CustomAttributes.Any(a => a.Constructor.DeclaringType.Name == "IgnoresAccessChecksToAttribute"
                                          && (string)a.ConstructorArguments[0].Value == target)) continue;
        var attr = new CustomAttribute(attrCtor);
        attr.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, target));
        asm.CustomAttributes.Add(attr);
    }

    using (var fs = File.Create(path)) asm.Write(fs);
    Console.WriteLine($"{Path.GetFileName(path)}: IgnoresAccessChecksTo -> {string.Join(", ", targets)}");
}
return 0;
