// SPDX-License-Identifier: MPL-2.0
#if NETSTANDARD2_0
namespace Absence.Fody;

using static Enumerable;

/// <summary>This weaver removes unused members within an assembly.</summary>
[CLSCompliant(false)]
public sealed class ModuleWeaver : BaseModuleWeaver
{
    const string Except = nameof(Except);

    /// <inheritdoc />
    public override bool ShouldCleanReference => false;

    /// <inheritdoc />
    public override void Execute()
    {
        WriteInfo(typeof(ModuleWeaver).Namespace);

        var asm = ModuleDefinition.Assembly;
        var modules = asm?.Modules;

        if (asm is null || modules is null)
            return;

        var except = Config
           .Attributes(Except)
           .SelectMany(x => x.Value.Split())
           .Select(x => x.Trim())
           .ToArray();

        asm.Trim(WriteInfo, except);
    }

    /// <inheritdoc />
    public override IEnumerable<string> GetAssembliesForScanning() => Empty<string>();
}
#endif
