// SPDX-License-Identifier: MPL-2.0
#if NETSTANDARD2_0
namespace Absence.Fody;

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
        Regex? ToRegex(ReadOnlyMemory<char> x)
        {
            if (!Go(() => new Regex(x.Trim().ToString().OrEmpty()), out var e, out var ok))
                return ok;

            WriteError($"Cannot parse regex (/{x}/) due to: {e}");
            return null;
        }

        var list = Config
           .Attributes(Except)
           .SelectMany(x => x.Value.SplitWhitespace())
           .Select(ToRegex)
           .Filter()
           .ToIList();

        var walkies = new Walkies { ModuleDefinition.Assembly };
        walkies.For(x => WriteDebug($"Preserving {x}."));
        walkies.Trim(ModuleDefinition, list, x => WriteInfo($"Begone, {x}!"));
    }

    /// <inheritdoc />
    public override IEnumerable<string> GetAssembliesForScanning() => [];
}
#endif
