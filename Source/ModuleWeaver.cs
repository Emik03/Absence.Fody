#region Emik.MPL

// <copyright file="ModuleWeaver.cs" company="Emik">
// Copyright (c) Emik. This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// </copyright>

#endregion

namespace Absence.Fody;

#region

using static Enumerable;

#endregion

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

        var walked = new Walkies(except).Display(WriteInfo).Walk(asm);

        modules
           .Select(x => x.Types)
           .SelectMany(walked.Mutate)
           .For(x => WriteInfo($"Begone, {x}!"));
    }

    /// <inheritdoc />
    public override IEnumerable<string> GetAssembliesForScanning() => Empty<string>();
}
