// SPDX-License-Identifier: MPL-2.0
namespace Absence.Fody;

/// <summary>Provides the method for trimming an assembly.</summary>
sealed class Walkies : IEqualityComparer<IMemberDefinition>, ICollection<IMemberDefinition>
{
#if DEBUG
    readonly Stopwatch _timer = Stopwatch.StartNew();
#endif
    [ProvidesContext]
    readonly HashSet<IMemberDefinition> _used;

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

        if (type is { Properties: { } typeProperties } && item is { Properties: { } properties })
            typeProperties
               .CartesianProduct(properties)
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
    /// <param name="onTrim">The action to invoke on each trimmed item.</param>
    public void Trim(AssemblyDefinition? item, Action<IMemberDefinition> onTrim)
#if DEBUG
    {
#else
        =>
#endif // ReSharper disable once BadPreprocessorIndent
        item?.Modules?.Lazily(x => Trim(x, onTrim)).Enumerate();
#if DEBUG
        _timer.Elapsed.ToConciseString().Debug();
        _timer.Stop();
    }
#endif
    /// <summary>
    /// Trims the module off of types, events, methods, and properties not included in this collection.
    /// </summary>
    /// <param name="item">The module to trim.</param>
    /// <param name="onTrim">The action to invoke on each trimmed item.</param>
    public void Trim(ModuleDefinition? item, Action<IMemberDefinition> onTrim)
    {
        void Trims<T>(ICollection<T?>? collection)
            where T : IMemberDefinition
        {
            void Process(T? next)
            {
                if (next is null)
                    return;

                if (!Contains(next))
                {
                    onTrim(next);
                    collection.Remove(next);
                }
                else if (next is TypeDefinition x)
                {
                    Trims(x.Events);
                    Trims(x.Methods);
                    Trims(x.Properties);
                    Trims(x.NestedTypes);
                }
            }

            collection?.ToArray().Lazily(Process).Enumerate();
        }

        Trims(item?.Types);
    }

    /// <inheritdoc />
    bool IEqualityComparer<IMemberDefinition>.Equals(IMemberDefinition? x, IMemberDefinition? y)
    {
        static bool MethodsEqual(MethodReference? x, MethodReference? y) =>
            x?.Parameters?.Count != y?.Parameters?.Count && x?.GenericParameters?.Count != y?.GenericParameters?.Count;

        static bool PropertiesEqual(PropertyReference? x, PropertyReference? y) =>
            x?.Parameters?.Count != y?.Parameters?.Count;

        static bool TypesEqual(TypeDefinition? x, TypeDefinition? y)
        {
            for (; x is not null && y is not null; x = x.DeclaringType, y = y.DeclaringType)
                if (x.Name != y.Name ||
                    x.Namespace != y.Namespace ||
                    x.GenericParameters?.Count != y.GenericParameters?.Count)
                    return false;

            return true;
        }

        return x == y ||
            x?.Name == y?.Name &&
            x?.GetType() == y?.GetType() &&
            TypesEqual(x as TypeDefinition, y as TypeDefinition) &&
            TypesEqual(x?.DeclaringType, y?.DeclaringType) &&
            MethodsEqual(x as MethodDefinition, y as MethodDefinition) &&
            PropertiesEqual(x as PropertyDefinition, y as PropertyDefinition);
    }

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
        var hash = 0;

        for (; obj is not null; obj = obj.DeclaringType)
            hash = HashCode.Combine(obj.GetType(), obj.Name, hash);

        return hash;
    }

    /// <inheritdoc />
    [Pure]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    [Pure]
    public IEnumerator<IMemberDefinition> GetEnumerator() => _used.GetEnumerator();

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
        item is { IsAbstract: true } or { IsNewSlot: true } or { IsPublic: true } or
            { IsConstructor: true, IsStatic: true } or { IsVirtual: true } or { Overrides.Count: > 0 } ||
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

    [MustUseReturnValue]
    static IMemberDefinition? Resolve(MemberReference? item)
    {
        static bool MethodsEqual(MethodReference x, MethodReference y) =>
            x.Name == y.Name &&
            x.Parameters?.Count == y.Parameters?.Count &&
            x.GenericParameters?.Count == y.GenericParameters?.Count;

        static bool PropertiesEqual(PropertyReference x, PropertyReference y) =>
            x.Name == y.Name && x.Parameters?.Count == y.Parameters?.Count;

        static bool TypesEqual(TypeReference x, TypeReference y) =>
            x.Name == y.Name && x.GenericParameters?.Count == y.GenericParameters?.Count;

        static TypeDefinition? Resolving(MemberReference? item) =>
            item?.DeclaringType is { } declaring ?
                Resolving(declaring) is var resolved && item is TypeReference inner
                    ? resolved?.NestedTypes?.FirstOrDefault(x => TypesEqual(x, inner))
                    : resolved :
                item is TypeReference outer ? item.Module?.Types?.FirstOrDefault(x => TypesEqual(x, outer)) : null;

        return item is not IMemberDefinition definition && Resolving(item) is var ret
            ? item switch
            {
                EventReference vent => ret?.Events?.FirstOrDefault(x => x.Name == vent.Name),
                FieldReference field => ret?.Fields?.FirstOrDefault(x => x.Name == field.Name),
                MethodReference method => ret?.Methods?.FirstOrDefault(x => MethodsEqual(x, method)),
                PropertyReference property => ret?.Properties?.FirstOrDefault(x => PropertiesEqual(x, property)),
                _ => ret,
            }
            : definition;
    }

    void AddDirectly(IMemberDefinition? item)
    {
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
}
