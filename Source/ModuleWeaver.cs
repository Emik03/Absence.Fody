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
    public override void Execute()
    {
        Regex? ToRegex(string x)
        {
            if (!Go(() => new Regex(x), out var e, out var ok))
                return ok;

            WriteError($"Cannot parse regex (/{x}/) due to: {e}");
            return null;
        }

        var list = Config
           .Attributes("Except")
           .SelectMany(x => x.Value.Split())
           .Select(ToRegex)
           .Filter()
           .ToIList();

        new Walkies { ModuleDefinition.Assembly }
           .ForEach(x => WriteDebug($"Preserving {x}."))
           .Trim(ModuleDefinition.Assembly, list, x => WriteInfo($"Begone, {x}!"));
    }

    /// <inheritdoc />
    public override IEnumerable<string> GetAssembliesForScanning() => [];
}
#endif
