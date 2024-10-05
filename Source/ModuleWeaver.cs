// SPDX-License-Identifier: MPL-2.0
#if NETSTANDARD2_0
namespace Absence.Fody;

/// <summary>This weaver removes unused members within an assembly.</summary>
[CLSCompliant(false)]
public sealed class ModuleWeaver : BaseModuleWeaver
{
    /// <inheritdoc />
    public override bool ShouldCleanReference => false;

    /// <inheritdoc />
    public override void Execute() =>
        new Walkies { ModuleDefinition.Assembly }.Trim(ModuleDefinition, x => WriteInfo($"Begone, {x}!"));

    /// <inheritdoc />
    public override IEnumerable<string> GetAssembliesForScanning() => [];
}
#endif
