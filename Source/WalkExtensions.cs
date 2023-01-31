// SPDX-License-Identifier: MPL-2.0
namespace Absence.Fody;

/// <summary>Provides the method for trimming an assembly.</summary>
static class WalkExtensions
{
    /// <inheritdoc cref="Trim(AssemblyDefinition, Action{string}, IEnumerable{string})" />
    internal static AssemblyDefinition Trim(this AssemblyDefinition asm, Action<string> logger) =>
        asm.Trim(logger, null);

    /// <summary>Trims an assembly off of unused types.</summary>
    /// <param name="asm">The assembly to trim.</param>
    /// <param name="logger">The logger to invoke with.</param>
    /// <param name="except">The types to preserve.</param>
    /// <returns>The parameter <paramref name="asm"/>.</returns>
    internal static AssemblyDefinition Trim(
        this AssemblyDefinition asm,
        Action<string> logger,
        IEnumerable<string>? except
    )
    {
        var walked = new Walkies(except).Display(logger).Walk(asm);

        asm.Modules
           .Select(x => x.Types)
           .SelectMany(walked.Mutate)
           .For(x => logger($"Begone, {x}!"));

        return asm;
    }
}
