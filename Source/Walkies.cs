#region Emik.MPL

// <copyright file="Walkies.cs" company="Emik">
// Copyright (c) Emik. This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// </copyright>

#endregion

#if !NET20
namespace Absence.Fody;

#region

using static Enumerable;
using static String;
using static StringComparer;

#endregion

/// <summary>Provides an iteration of members that come from tokens.</summary>
sealed partial class Walkies : IReadOnlyCollection<object>
{
    /// <summary>The separator between each element.</summary>
    internal const string Between = "\n    ";

    const string
        Generated = "System.Runtime.CompilerServices.CompilerGeneratedAttribute",
        Main = "Program.Main",
        MeansImplicitUse = "JetBrains.Annotations.MeansImplicitUseAttribute",
        Module = "<Module>",
        ImplicitMain = "Program.<Main>$",
        IsExternalInit = "System.Runtime.CompilerServices.IsExternalInit",
        Program = nameof(Program),
        UsedImplicitly = "JetBrains.Annotations.UsedImplicitlyAttribute";

    readonly HashSet<string> _except;

    [ProvidesContext]
    readonly HashSet<object> _hash = new();

    [UsedImplicitly]
    Action<string> _logger = _ => { };

    /// <summary>Initializes a new instance of the <see cref="Walkies" /> class.</summary>
    /// <param name="except">The list of types to exempt from filtering.</param>
    internal Walkies(IEnumerable<string>? except = null) =>
        _except = new(except?.SplitBy(IsNullOrWhiteSpace)[false] ?? Empty<string>(), Ordinal);

    /// <inheritdoc />
    public int Count => _hash.Count;

    /// <inheritdoc />
    public IEnumerator<object> GetEnumerator() => _hash.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => _hash.GetEnumerator();

    /// <inheritdoc />
    public override string ToString() => Join(Between, _hash);

    /// <summary>Invokes a method that displays the current state of the object.</summary>
    /// <param name="logger">The delegate to invoke.</param>
    /// <returns>Itself.</returns>
    internal Walkies Display(Action<string> logger)
    {
        _except.For(x => logger($"Configured to keep \"{x}\"."));
        _logger = logger;

        return this;
    }

    /// <summary>Invokes a method that displays the current state of the object.</summary>
    /// <param name="logger">The delegate to invoke.</param>
    /// <returns>Itself.</returns>
    internal Walkies DisplayHash(Action<string> logger)
    {
        logger(ToString());
        _logger = logger;

        return this;
    }

    /// <summary>Mutates the argument by removing items not listed in the current set.</summary>
    /// <param name="mutate">The collection to mutate by filtering.</param>
    /// <returns>A new iteration of items that have been removed.</returns>
    internal IEnumerable<MemberReference> Mutate(ICollection<TypeDefinition> mutate)
    {
        var (used, unused) = mutate.SplitBy(AnyEqual);
        var nestedUnused = used.Select(x => x.NestedTypes).SelectMany(Mutate);
        unused.Select(mutate.Remove).Enumerate();

        return unused.Concat(nestedUnused);
    }

    static bool AnyPublic(params object?[] members) =>
        members.Any(x => x is IList list ? list.Cast<object>().Any(IsPublic) : IsPublic(x));

    static bool AnyImplicit(IMonoProvider i) => i.CustomAttributes?.Any(IsImplicit) ?? false;

    static bool IsImplicit(ICustomAttribute i) =>
        i.AttributeType?.FullName is Generated or MeansImplicitUse or UsedImplicitly;

    static bool IsPublic(object? member) =>
        member switch
        {
            IMonoProvider p when AnyImplicit(p) => true,
            ICustomAttribute i when IsImplicit(i) => true,
            MemberReference
            {
                FullName:
                ImplicitMain or
                IsExternalInit or
                Main or
                Module or
                Program,
            } => true,
            MemberReference m when m.FullName.EndsWith("ProcessedByFody") => true,
            EventDefinition e => AnyPublic(e.AddMethod, e.InvokeMethod, e.OtherMethods, e.RemoveMethod),
            FieldDefinition f => f.IsPublic,
            MethodDefinition m => m.IsPublic || m.IsConstructor || m != m.GetBaseMethod(),
            PropertyDefinition p => AnyPublic(p.GetMethod, p.OtherMethods, p.SetMethod),
            TypeDefinition t => t.IsPublic || t.IsNestedPublic,
            null => false,
            _ => true,
        };

    bool AnyEqual(TypeDefinition obj) =>
        _hash.Contains(obj) || new[] { obj.FullName, obj.Name, obj.Namespace }.Any(_except.Contains);

    bool Has([NotNullWhen(false)] object? x) => x is null || !_hash.Add(x);
}
#endif
