// Injects [assembly: IgnoresAccessChecksTo("<game assembly>")] for every
// referenced Timberborn.*/SecondShift.* assembly, defining the attribute type
// locally (Unity Mono does not ship it). Usage: AccessInjector <dll> [dll...]
using Mono.Cecil;
using Mono.Cecil.Cil;

foreach (var path in args) {
    var bytes = File.ReadAllBytes(path);
    var asm = AssemblyDefinition.ReadAssembly(new MemoryStream(bytes));
    var module = asm.MainModule;

    // Define the attribute type if missing.
    var attrType = module.GetType("System.Runtime.CompilerServices", "IgnoresAccessChecksToAttribute");
    if (attrType == null) {
        attrType = new TypeDefinition(
            "System.Runtime.CompilerServices", "IgnoresAccessChecksToAttribute",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            module.ImportReference(typeof(Attribute)));
        var field = new FieldDefinition("_assemblyName", FieldAttributes.Private, module.TypeSystem.String);
        var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
        ctor.Parameters.Add(new ParameterDefinition("assemblyName", ParameterAttributes.None, module.TypeSystem.String));
        var il = ctor.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, module.ImportReference(typeof(object).GetConstructor(Type.EmptyTypes)));
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
        .Where(r => r.Name.StartsWith("Timberborn") || r.Name.StartsWith("SecondShift"))
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

    var writer = new WriterParameters(); // strong-name: none
    using (var fs = File.Create(path)) asm.Write(fs);
    Console.WriteLine($"{Path.GetFileName(path)}: IgnoresAccessChecksTo -> {string.Join(", ", targets)}");
}
return 0;
