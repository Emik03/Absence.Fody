// <copyright file="Walkies.Walk.cs" company="Emik">
// Copyright (c) Emik. This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// </copyright>
namespace Absence.Fody;

/// <summary>Provides an iteration of members that come from tokens.</summary>
sealed partial class Walkies
{
    /// <summary>Calls the corresponding <c>Walk</c> method depending on the runtime type of the object.</summary>
    /// <param name="x">The object to walk through.</param>
    /// <returns>Itself.</returns>
    internal Walkies Match(object? x) =>
        x switch
        {
            IMethodSignature ms => Walk(ms),
            CustomAttributeArgument caa => Walk(caa),
            MonoNamedArgument can => Walk(can),
            AssemblyDefinition ad => Walk(ad),
            ConstantDebugInformation cdi => Walk(cdi),
            CustomAttribute ca => Walk(ca),
            ExceptionHandler eh => Walk(eh),
            ImportDebugInformation idi => Walk(idi),
            ImportTarget it => Walk(it),
            InterfaceImplementation ii => Walk(ii),
            Instruction i => Walk(i),
            MemberReference mr => Walk(mr),
            MethodDebugInformation mdi => Walk(mdi),
            ModuleReference md => Walk(md),
            MethodReturnType mrt => Walk(mrt),
            MonoMethodBody mb => Walk(mb),
            MonoSecurity sa => Walk(sa),
            SecurityDeclaration sd => Walk(sd),
            ParameterDefinition pad => Walk(pad),
            VariableDefinition vd => Walk(vd),
            _ when Has(x) => this,
            _ => this,
        };

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(IMethodSignature? x)
    {
        if (x is MethodReference y)
            return Walk(y);

        if (Has(x))
            return this;

        Walk(x.MethodReturnType);
        Walk(x.ReturnType);
        x.Parameters?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(CustomAttributeArgument x) => Has(x) ? this : Walk(x.Type);

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(MonoNamedArgument x) => Has(x) ? this : Walk(x.Argument);

    /// <summary>Steps through a definition, caching all items found along the way.</summary>
    /// <param name="x">The entry point.</param>
    /// <returns>Itself.</returns>
    internal Walkies Walk(AssemblyDefinition? x)
    {
        if (Has(x))
            return this;

        Walk(x.EntryPoint);
        Walk(x.MainModule);
        x.SecurityDeclarations?.Select(Walk).Enumerate();
        x.CustomAttributes?.Select(Walk).Enumerate();
        x.Modules?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(ConstantDebugInformation? x)
    {
        if (Has(x))
            return this;

        Walk(x.ConstantType);
        return Match(x.Value);
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(CustomAttribute? x)
    {
        if (Has(x))
            return this;

        Walk(x.AttributeType);
        Walk(x.Constructor);
        x.ConstructorArguments?.Select(Walk).Enumerate();
        x.Fields?.Select(Walk).Enumerate();
        x.Properties?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(EventDefinition? x)
    {
        if (Has(x))
            return this;

        Walk(x.AddMethod);
        Walk(x.DeclaringType);
        Walk(x.EventType);
        Walk(x.InvokeMethod);
        Walk(x.RemoveMethod);
        x.CustomAttributes?.Select(Walk).Enumerate();
        x.OtherMethods?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(ExceptionHandler? x)
    {
        if (Has(x))
            return this;

        Walk(x.CatchType);
        Walk(x.FilterStart);
        Walk(x.HandlerEnd);
        Walk(x.HandlerStart);
        Walk(x.TryEnd);
        Walk(x.TryStart);
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(FieldDefinition? x)
    {
        if (Has(x))
            return this;

        Match(x.Constant);
        Walk(x.DeclaringType);
        Walk(x.FieldType);
        Walk(x.Module);
        x.CustomAttributes?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(InterfaceImplementation? x)
    {
        if (Has(x))
            return this;

        Walk(x.InterfaceType);
        x.CustomAttributes?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(ImportDebugInformation? x)
    {
        if (Has(x))
            return this;

        Walk(x.Parent);
        x.Targets?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(ImportTarget? x) => Has(x) ? this : Walk(x.Type);

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(Instruction? x)
    {
        if (Has(x))
            return this;

        Match(x.Operand);
        Walk(x.Previous);
        Walk(x.Next);
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(MemberReference? x) =>
        x switch
        {
            IMethodSignature method => Walk(method),
            EventDefinition vent => Walk(vent),
            FieldDefinition field => Walk(field),
            PropertyDefinition property => Walk(property),
            TypeReference type => Walk(type),
            _ when Has(x) => this,
            _ => Walk(x.DeclaringType),
        };

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(MethodDebugInformation? x)
    {
        if (Has(x))
            return this;

        Walk(x.Scope);
        Walk(x.Method);
        Walk(x.StateMachineKickOffMethod);
        x.GetScopes()?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(MethodDefinition? x)
    {
        if (Has(x))
            return this;

        Walk(x.Body);
        Walk(x.DebugInformation);
        Walk(x.DeclaringType);
        Walk(x.MethodReturnType);
        Walk(x.Module);
        Walk(x.ReturnType);
        x.SecurityDeclarations?.Select(Walk).Enumerate();
        x.CustomAttributes?.Select(Walk).Enumerate();
        x.GenericParameters?.Select(Walk).Enumerate();
        x.Overrides?.Select(Walk).Enumerate();
        x.Parameters?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(MethodReference? x)
    {
        if (x is MethodDefinition y)
            return Walk(y);

        if (Has(x))
            return this;

        Walk(x.DeclaringType);
        Walk(x.MethodReturnType);
        Walk(x.Module);
        Walk(x.ReturnType);
        x.GenericParameters?.Select(Walk).Enumerate();
        x.Parameters?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(ModuleReference? x)
    {
        if (x is ModuleDefinition y)
            return Walk(y);

        Has(x);
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(MethodReturnType? x)
    {
        if (Has(x))
            return this;

        Match(x.Constant);
        Walk(x.Method);
        Walk(x.ReturnType);
        x.CustomAttributes?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(ModuleDefinition? x)
    {
        if (Has(x))
            return this;

        Walk(x.Assembly);
        Walk(x.EntryPoint);
        x.CustomAttributes?.Select(Walk).Enumerate();
        x.ModuleReferences?.Select(Walk).Enumerate();
        x.Types?.Where(IsPublic).Select(Walk).Enumerate();

        // x.GetMemberReferences()?.Where(IsPublic).Select(Walk).Enumerate();
        // x.GetTypeReferences()?.Where(IsPublic).Select(Walk).Enumerate();
        // x.GetTypes()?.Where(IsPublic).Select(Walk).Enumerate();

        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(MonoMethodBody? x)
    {
        if (Has(x))
            return this;

        Walk(x.Method);
        Walk(x.ThisParameter);
        x.ExceptionHandlers?.Select(Walk).Enumerate();
        x.Instructions?.Select(Walk).Enumerate();
        x.Variables?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(MonoSecurity? x)
    {
        if (Has(x))
            return this;

        Walk(x.AttributeType);
        x.Fields?.Select(Walk).Enumerate();
        x.Properties?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(SecurityDeclaration? x)
    {
        if (Has(x))
            return this;

        x.SecurityAttributes?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(TypeReference? x)
    {
        switch (x)
        {
            case TypeDefinition td: return Walk(td);
            case TypeSpecification ts: return Walk(ts);
            case var _ when Has(x): return this;
        }

        Walk(x.DeclaringType);
        Walk(x.Module);
        x.GenericParameters?.Select<GenericParameter, Walkies>(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(TypeDefinition? x)
    {
        if (Has(x))
            return this;

        Walk(x.BaseType);
        Walk(x.DeclaringType);
        Walk(x.Module);
        x.CustomAttributes?.Select(Walk).Enumerate();
        x.Events.Select(Walk).Enumerate();
        x.Fields.Select(Walk).Enumerate();
        x.GenericParameters?.Select(Walk).Enumerate();
        x.Interfaces?.Select(Walk).Enumerate();
        x.Methods.Select(Walk).Enumerate();
        x.SecurityDeclarations?.Select(Walk).Enumerate();
        x.Properties.Select(Walk).Enumerate();
        x.NestedTypes?.Where(IsPublic).Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(TypeSpecification? x)
    {
        if (Has(x))
            return this;

        Walk(x.DeclaringType);
        Walk(x.ElementType);
        Walk(x.Module);
        Walk(x.GetElementType());
        x.GenericParameters?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(PropertyDefinition? x)
    {
        if (Has(x))
            return this;

        Match(x.Constant);
        Walk(x.DeclaringType);
        Walk(x.GetMethod);
        Walk(x.Module);
        Walk(x.PropertyType);
        Walk(x.SetMethod);
        x.CustomAttributes?.Select(Walk).Enumerate();
        x.OtherMethods?.Select(Walk).Enumerate();
        x.Parameters?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(ParameterDefinition? x)
    {
        if (Has(x))
            return this;

        Match(x.Constant);
        Walk(x.Method);
        Walk(x.ParameterType);
        x.CustomAttributes?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(ScopeDebugInformation? x)
    {
        if (Has(x))
            return this;

        Walk(x.Import);
        x.Scopes?.Select(Walk).Enumerate();
        x.Constants?.Select(Walk).Enumerate();
        return this;
    }

    /// <inheritdoc cref="Walk(Mono.Cecil.AssemblyDefinition?)"/>
    internal Walkies Walk(VariableDefinition? x) => Has(x) ? this : Walk(x.VariableType);
}
