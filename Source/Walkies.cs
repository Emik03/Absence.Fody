// SPDX-License-Identifier: MPL-2.0
namespace Absence.Fody;

/// <summary>Provides the method for trimming an assembly.</summary>
sealed class Walkies : IEqualityComparer<IMemberDefinition>,
    IEqualityComparer<ParameterDefinition?>,
    ICollection<IMemberDefinition>
{
    [ProvidesContext]
    readonly HashSet<IMemberDefinition> _used;
#if DEBUG
    readonly Stopwatch _timer = Stopwatch.StartNew();
#endif
    public Walkies() => _used = new(comparer: this);

    /// <inheritdoc />
    bool ICollection<IMemberDefinition>.IsReadOnly => false;

    /// <inheritdoc />
    public int Count => _used.Count;

    /// <inheritdoc cref="ICollection{T}.Add"/>
    public void Add(AssemblyDefinition? item)
    {
        item?.Modules.OrEmpty().Lazily(Add).Enumerate();
        item?.CustomAttributes.OrEmpty().Lazily(Add).Enumerate();
    }

    /// <inheritdoc cref="ICollection{T}.Add"/>
    public void Add(CustomAttribute? item)
    {
        if (item?.Constructor is MethodDefinition constructor)
            Add(constructor);

        if (item?.AttributeType is not TypeDefinition type)
            return;

        Add(type);

        if (type is { Properties: { } typeProps } && item is { Properties: { } props })
            typeProps
               .CartesianProduct(props)
               .Where(x => x.First?.Name == x.Second.Name)
               .Select(First)
               .Lazily(Add)
               .Enumerate();
    }

    /// <inheritdoc cref="ICollection{T}.Add"/>
    public void Add(IGenericInstance? item)
    {
        while (item is not null)
        {
            if (item.GenericArguments is { } arguments)
                foreach (var argument in arguments)
                {
                    Add(Resolve(argument));
                    Add(argument as IGenericInstance);
                }

            switch (item)
            {
                case GenericInstanceMethod { ElementMethod: var next }:
                    Add(Resolve(next));
                    item = next as IGenericInstance;
                    continue;
                case GenericInstanceType { ElementType: var next }:
                    Add(Resolve(next));
                    item = next as IGenericInstance;
                    continue;
                default: return;
            }
        }
    }

    /// <inheritdoc />
    public void Add(IMemberDefinition? item)
    {
        for (; item is not null; item = item.DeclaringType)
        {
            AddDirectly(item);

            if (item is not TypeDefinition { Methods: { } methods })
                continue;

            foreach (var method in methods)
                if (method is { IsConstructor: true, IsStatic: true })
                    AddDirectly(method);
        }
    }

    /// <inheritdoc cref="ICollection{T}.Add"/>
    public void Add(InterfaceImplementation? item)
    {
        Add(Resolve(item?.InterfaceType));
        Add(item?.InterfaceType as IGenericInstance);
        item?.CustomAttributes.OrEmpty().Lazily(Add).Enumerate();
    }

    /// <inheritdoc cref="ICollection{T}.Add"/>
    public void Add(GenericParameter? item)
    {
        item?.Constraints.OrEmpty().Lazily(Add).Enumerate();
        item?.CustomAttributes.OrEmpty().Lazily(Add).Enumerate();
    }

    /// <inheritdoc cref="ICollection{T}.Add"/>
    public void Add(GenericParameterConstraint? item)
    {
        Add(Resolve(item?.ConstraintType));
        Add(item?.ConstraintType as IGenericInstance);
        item?.CustomAttributes.OrEmpty().Lazily(Add).Enumerate();
    }

    /// <inheritdoc cref="ICollection{T}.Add"/>
    public void Add(Instruction? item)
    {
        Add(item?.Operand as IGenericInstance);
        Add(Resolve(item?.Operand as MemberReference));
    }

    /// <inheritdoc cref="ICollection{T}.Add"/>
    public void Add(ModuleDefinition? x)
    {
        Add(x?.EntryPoint);
        x?.CustomAttributes.OrEmpty().Lazily(Add).Enumerate();
        x?.Types.OrEmpty().Where(IsPublic).Lazily(Add).Enumerate();
    }

    /// <inheritdoc cref="ICollection{T}.Add"/>
    public void Add(ParameterDefinition? item)
    {
        Add(Resolve(item?.ParameterType));
        Add(item?.ParameterType as IGenericInstance);
        item?.CustomAttributes.OrEmpty().Lazily(Add).Enumerate();
    }

    /// <inheritdoc cref="ICollection{T}.Add"/>
    public void Add(VariableDefinition? item)
    {
        Add(Resolve(item?.VariableType));
        Add(item?.VariableType as IGenericInstance);
    }

    /// <inheritdoc />
    void ICollection<IMemberDefinition>.Clear() => _used.Clear();

    /// <inheritdoc />
    void ICollection<IMemberDefinition>.CopyTo(IMemberDefinition[] array, int arrayIndex) =>
        _used.CopyTo(array, arrayIndex);

    /// <summary>
    /// Trims the assembly off of types, events, methods, and properties not included in this collection.
    /// </summary>
    /// <param name="item">The assembly to trim.</param>
    /// <param name="except">The names of types, events, methods, and properties to exclude.</param>
    /// <param name="onTrim">The action to invoke on each trimmed item.</param>
    public void Trim(AssemblyDefinition? item, ICollection<Regex> except, Action<IMemberDefinition> onTrim)
#if DEBUG
    {
#else
        =>
#endif // ReSharper disable once BadPreprocessorIndent
        item?.Modules?.Lazily(x => Trim(x, except, onTrim)).Enumerate();
#if DEBUG
        _timer.Elapsed.ToConciseString().Debug();
        _timer.Stop();
    }
#endif
    /// <summary>
    /// Trims the module off of types, events, methods, and properties not included in this collection.
    /// </summary>
    /// <param name="item">The module to trim.</param>
    /// <param name="except">The names of types, events, methods, and properties to exclude.</param>
    /// <param name="onTrim">The action to invoke on each trimmed item.</param>
    public void Trim(ModuleDefinition? item, ICollection<Regex> except, Action<IMemberDefinition> onTrim)
    {
        static IEnumerable<IList<ImportTarget?>?>? Targets(MethodDefinition x) =>
            x.DebugInformation?.Scope?.Import
               .FindPathToNull(x => x.Parent)
               .Select(x => x.Targets);

        void TrimAll<T>(ICollection<T?>? collection)
            where T : IMemberDefinition
        {
            void TrimNext(T? x)
            {
                if (IsIgnored(x)) { }
                else if (!Contains(x))
                {
                    onTrim(x);
                    collection.Remove(x);
                }
                else if (x is TypeDefinition { Events: var e, Methods: var m, NestedTypes: var n, Properties: var p })
                {
                    TrimAll(e);
                    TrimAll(m);
                    TrimAll(n);
                    TrimAll(p);
                }
            }

            collection?.ToArray().Lazily(TrimNext).Enumerate();
        }

        bool HasTrimmed(ImportTarget? x) =>
            x?.Type?.Module?.Assembly?.FullName == item.Assembly?.FullName &&
            _used.OfType<TypeDefinition>().Any(y => x?.Type?.Name == y.Name && x.Type?.Namespace == y.Namespace);

        bool IsIgnored([NotNullWhen(false)] IMemberDefinition? next)
        {
            if (next is null || except.Count is 0)
                return false;

            var name = next.Name.OrEmpty();

            for (var i = next.DeclaringType; i is not null; i = i.DeclaringType)
                name = $"{i.Name}.{name}";

            if (next.DeclaringType is { DeclaringType.Namespace: [_, ..] space })
                name = $"{space}.{name}";

            return except.Any(x => x.IsMatch(name));
        }

        TrimAll(item?.Types);
        item?.GetTypes().ManyOrEmpty(x => x.Methods).ManyOrEmpty(Targets).Lazily(x => x.Retain(HasTrimmed)).Enumerate();
    }

    /// <inheritdoc />
    bool IEqualityComparer<IMemberDefinition>.Equals(IMemberDefinition? x, IMemberDefinition? y)
    {
        bool MethodsEqual(MethodReference? x, MethodReference? y) =>
            x is null
                ? y is null
                : y is not null &&
                x.GenericParameters?.Count == y.GenericParameters?.Count &&
                x.Parameters.OrEmpty().SequenceEqual(y.Parameters.OrEmpty(), this);

        bool PropertiesEqual(PropertyReference? x, PropertyReference? y) =>
            x is null ? y is null : y is not null && x.Parameters.OrEmpty().SequenceEqual(y.Parameters.OrEmpty(), this);

        static bool TypesEqual(TypeDefinition? x, TypeDefinition? y, bool recurse)
        {
            for (; x is not null && y is not null; x = recurse ? x.DeclaringType : null, y = y.DeclaringType)
                if (x.Name != y.Name ||
                    x.Namespace != y.Namespace ||
                    x.GenericParameters?.Count != y.GenericParameters?.Count)
                    return false;

            return true;
        }

        return x == y ||
            x?.Name == y?.Name &&
            x?.GetType() == y?.GetType() &&
            TypesEqual(x?.DeclaringType, y?.DeclaringType, true) &&
            TypesEqual(x as TypeDefinition, y as TypeDefinition, false) &&
            MethodsEqual(x as MethodDefinition, y as MethodDefinition) &&
            PropertiesEqual(x as PropertyDefinition, y as PropertyDefinition);
    }

    /// <inheritdoc />
    bool IEqualityComparer<ParameterDefinition?>.Equals(ParameterDefinition? x, ParameterDefinition? y) =>
        x?.ParameterType?.Name == y?.ParameterType?.Name ||
        x?.ParameterType is GenericParameter { Position: var px } &&
        y?.ParameterType is GenericParameter { Position: var py } &&
        px == py;

    /// <inheritdoc />
    [Pure]
    public bool Contains([NotNullWhen(true)] IMemberDefinition? item) =>
        item switch
        {
            not null when _used.Contains(item) => true,
            EventDefinition { AddMethod: var a, InvokeMethod: var i, RemoveMethod: var r, OtherMethods: var o } =>
                _used.Contains(a) || _used.Contains(i) || _used.Contains(r) || o.OrEmpty().Any(_used.Contains),
            PropertyDefinition { GetMethod: var g, SetMethod: var s, OtherMethods: var o } =>
                _used.Contains(g) || _used.Contains(s) || o.OrEmpty().Any(_used.Contains),
            TypeDefinition { Methods: { } m } => m.Any(_used.Contains),
            _ => false,
        };

    /// <inheritdoc />
    bool ICollection<IMemberDefinition>.Remove([NotNullWhen(true)] IMemberDefinition? item) =>
        item is not null && _used.Remove(item);

    /// <inheritdoc />
    [Pure]
    int IEqualityComparer<IMemberDefinition>.GetHashCode(IMemberDefinition? obj)
    {
        int hash = Prime();

        for (; obj is not null; obj = obj.DeclaringType)
        {
            hash ^= unchecked(obj.GetType().GetHashCode() * Prime());
            hash ^= unchecked(StringComparer.Ordinal.GetHashCode(obj.Name.OrEmpty()) * Prime());
        }

        return hash;
    }

    /// <inheritdoc />
    int IEqualityComparer<ParameterDefinition?>.GetHashCode(ParameterDefinition? obj) =>
        StringComparer.Ordinal.GetHashCode(obj?.ParameterType?.Name ?? "");

    /// <inheritdoc />
    [Pure]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    [Pure]
    public IEnumerator<IMemberDefinition> GetEnumerator() => _used.GetEnumerator();

    /// <inheritdoc cref="List{T}.ForEach"/>
    public Walkies ForEach(Action<IMemberDefinition> action)
    {
        foreach (var member in this)
            action(member);

        return this;
    }

    [Pure]
    static bool IsImplicitlyUse([NotNullWhen(true)] CustomAttribute? x) =>
        x is { AttributeType.FullName: { } name } &&
        (name is "JetBrains.Annotations.UsedImplicitlyAttribute" ||
            name == typeof(CompilerGeneratedAttribute).FullName) ||
        x is { AttributeType: IMonoProvider { CustomAttributes: { } attributes } } &&
        attributes.Any(x => x.AttributeType?.FullName is "JetBrains.Annotations.MeansImplicitUseAttribute");

    [Pure]
    static bool IsPublic([NotNullWhen(true)] EventDefinition? item) =>
        item is not null &&
        (IsPublic(item.AddMethod) ||
            IsPublic(item.InvokeMethod) ||
            IsPublic(item.RemoveMethod) ||
            item.OtherMethods.OrEmpty().Any(IsPublic) ||
            item.CustomAttributes.OrEmpty().Any(IsImplicitlyUse));

    [Pure]
    static bool IsPublic([NotNullWhen(true)] IMemberDefinition? item) =>
        item is { DeclaringType: var declaring } &&
        (declaring is null || IsPublic(declaring)) &&
        (item.CustomAttributes.OrEmpty().Any(IsImplicitlyUse) ||
            item.DeclaringType
               .FindPathToNull(x => x.DeclaringType)
               .ManyOrEmpty(x => x.CustomAttributes)
               .Any(x => x.AttributeType?.FullName == typeof(CompilerGeneratedAttribute).FullName));

    [Pure]
    static bool IsPublic([NotNullWhen(true)] MethodDefinition? item) =>
        item is { IsAbstract: true } or { IsNewSlot: true } or { IsPublic: true } or { IsVirtual: true } or
            { Overrides.Count: > 0 } ||
        item is { IsConstructor: true } &&
        (item is { IsStatic: true } || item.DeclaringType?.Methods?.Count(x => x.IsConstructor) is 1) ||
        IsPublic((IMemberDefinition?)item);

    [Pure]
    static bool IsPublic([NotNullWhen(true)] PropertyDefinition? item) =>
        item is not null &&
        (IsPublic(item.GetMethod) ||
            IsPublic(item.SetMethod) ||
            item.OtherMethods.OrEmpty().Any(IsPublic) ||
            IsPublic((IMemberDefinition?)item));

    [Pure]
    static bool IsPublic([NotNullWhen(true)] TypeDefinition? item) =>
        item is { IsPublic: true } or { IsNestedPublic: true } ||
        item is
        {
            FullName: "<Module>" or
            "System.Runtime.CompilerServices.IsExternalInit" or
            [.., '_', 'P', 'r', 'o', 'c', 'e', 's', 's', 'e', 'd', 'B', 'y', 'F', 'o', 'd', 'y'],
        } ||
        IsPublic((IMemberDefinition?)item);

    void AddDirectly(IMemberDefinition? item)
    {
        if (item is { DeclaringType: { } declaring })
            AddDirectly(declaring);

        while (item is not null && _used.Add(item) && item.CustomAttributes.OrEmpty().For(Add) is var _)
            switch (item)
            {
                case EventDefinition { EventType: var next }:
                    item = Resolve(next);
                    continue;
                case FieldDefinition { FieldType: var next }:
                    item = Resolve(next);
                    continue;
                case MethodDefinition { MethodReturnType: { CustomAttributes: var attr, ReturnType: var next } } method:
                    attr.OrEmpty().Lazily(Add).Enumerate();
                    method.Parameters.OrEmpty().Lazily(Add).Enumerate();
                    method.Body?.Variables.OrEmpty().Lazily(Add).Enumerate();
                    method.GenericParameters.OrEmpty().Lazily(Add).Enumerate();
                    method.Body?.Instructions.OrEmpty().Lazily(Add).Enumerate();
                    method.Overrides.OrEmpty().OfType<IMemberDefinition>().Lazily(Add).Enumerate();
                    item = Resolve(next);
                    continue;
                case PropertyDefinition { PropertyType: var next }:
                    item = Resolve(next);
                    continue;
                case TypeDefinition { BaseType: var next } type:
                    type.Fields.OrEmpty().Lazily(Add).Enumerate();
                    type.Interfaces.OrEmpty().Lazily(Add).Enumerate();
                    type.GenericParameters.OrEmpty().Lazily(Add).Enumerate();
                    type.Events.OrEmpty().Where(IsPublic).Lazily(Add).Enumerate();
                    type.Methods.OrEmpty().Where(IsPublic).Lazily(Add).Enumerate();
                    type.Properties.OrEmpty().Where(IsPublic).Lazily(Add).Enumerate();
                    type.NestedTypes.OrEmpty().Where(IsPublic).Lazily(Add).Enumerate();
                    item = Resolve(next);
                    continue;
                default: return;
            }
    }

    [MustUseReturnValue]
    IMemberDefinition? Resolve(MemberReference? item)
    {
        bool MethodsEqual(MethodReference x, MethodReference y) =>
            x.Name == y.Name && SequencesEqual(x.Parameters, y.Parameters);

        bool PropertiesEqual(PropertyReference x, PropertyReference y) =>
            x.Name == y.Name && SequencesEqual(x.Parameters, y.Parameters);

        bool SequencesEqual(IEnumerable<ParameterDefinition?>? x, IEnumerable<ParameterDefinition?>? y) =>
            x.OrEmpty().SequenceEqual(y.OrEmpty(), this);

        static TypeDefinition? Resolving(MemberReference? item) =>
            item?.DeclaringType is { } declaring ?
                Resolving(declaring) is var resolved && item is TypeReference inner
                    ? resolved?.NestedTypes?.FirstOrDefault(x => x.Name == inner.Name)
                    : resolved :
                item is TypeReference outer ? item.Module?.Types?.FirstOrDefault(x => x.Name == outer.Name) : null;

        return item is not IMemberDefinition definition && Resolving(item) is var ret
            ? item switch
            {
                EventReference vent => ret?.Events.OrEmpty().FirstOrDefault(x => x.Name == vent.Name),
                FieldReference field => ret?.Fields.OrEmpty().FirstOrDefault(x => x.Name == field.Name),
                MethodReference method => ret?.Methods.OrEmpty().FirstOrDefault(x => MethodsEqual(x, method)),
                PropertyReference props => ret?.Properties.OrEmpty().FirstOrDefault(x => PropertiesEqual(x, props)),
                _ => ret,
            }
            : definition;
    }
}
