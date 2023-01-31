// SPDX-License-Identifier: MPL-2.0
#if NETSTANDARD2_0
namespace Absence.Fody;

using static Enumerable;
using static String;
using static StringComparer;

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
        ProcessedByFody = nameof(ProcessedByFody),
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
    public override string ToString() => _hash.Stringify();

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

        var unusedMembers = used.SelectMany(Mutate).SelectMany(x => x);

        return unused.Concat(nestedUnused).Concat(unusedMembers);
    }

    void DeleteMePlease() { }

    /// <summary>Mutates the argument by removing items not listed in the current set.</summary>
    /// <param name="mutate">The collection to mutate by filtering.</param>
    /// <returns>A new iteration of items that have been removed.</returns>
    internal IEnumerable<IEnumerable<MemberReference>> Mutate(TypeDefinition mutate)
    {
        yield return Mutate(mutate.Events);
        yield return Mutate(mutate.Fields);
        yield return Mutate(mutate.Methods);
        yield return Mutate(mutate.Properties);
    }

    static bool AnyPublic(params object?[] members) =>
        members.Any(x => x is IList<object?> list ? list.Any(IsPublic) : IsPublic(x));

    static bool AnyImplicit(IMonoProvider i) => i.CustomAttributes?.Any(IsImplicit) ?? false;

    static bool IsImplicit(ICustomAttribute i) =>
        i.AttributeType?.FullName is Generated or MeansImplicitUse or UsedImplicitly;

    static bool IsPublic([NotNullWhen(true)] object? member) =>
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
            MemberReference m when m.FullName.EndsWith(ProcessedByFody) => true,
            EventDefinition e => AnyPublic(e.AddMethod, e.InvokeMethod, e.OtherMethods, e.RemoveMethod),
            FieldDefinition f => f.IsPublic,
            MethodDefinition m => m.IsPublic || m.IsConstructor || m != m.GetBaseMethod(),
            PropertyDefinition p => AnyPublic(p.GetMethod, p.OtherMethods, p.SetMethod),
            TypeDefinition t => t.IsPublic || t.IsNestedPublic,
            null => false,
            _ => true,
        };

    bool AnyEqual([NotNullWhen(false)] MemberReference? obj) =>
        obj is null ||
        IsPublic(obj) ||
        _except.Contains(obj.FullName) ||
        _except.Contains(obj.Name) ||
        _hash.Contains(obj) ||
        _hash.OfType<MemberReference>().Any(x => x.Name == obj.Name) ||
        obj.DeclaringType?.Namespace is { } name && _except.Contains(name) ||
        obj switch
        {
            EventDefinition e => AnyEqual(e.AddMethod, e.InvokeMethod, e.OtherMethods, e.RemoveMethod),
            PropertyDefinition p => AnyEqual(p.GetMethod, p.OtherMethods, p.SetMethod),
            TypeReference t => _except.Contains(t.Namespace),
            _ => false,
        };

    bool AnyEqual(params object?[] objs) =>
        objs.Any(x => x is IEnumerable<MemberReference?> more ? more.Any(AnyEqual) : AnyEqual(x as MemberReference));

    bool Has([NotNullWhen(false)] object? x) =>
        x is null || !_hash.Add(x) || x is Instruction y && !_hash.Add(y.Operand);

    IEnumerable<T> Mutate<T>(ICollection<T> mutate)
        where T : MemberReference =>
        mutate.SplitBy(AnyEqual)[false].For(x => mutate.Remove(x));
}
#endif
