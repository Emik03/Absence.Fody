// SPDX-License-Identifier: MPL-2.0
#if NETSTANDARD2_0 || NET6_0 // ReSharper disable RedundantBlankLines

global using Fody;
global using Mono.Cecil;
global using Mono.Cecil.Cil;
global using Mono.Cecil.Rocks;
global using IMonoProvider = Mono.Cecil.ICustomAttributeProvider;
global using MonoMethodBody = Mono.Cecil.Cil.MethodBody;
global using MonoNamedArgument = Mono.Cecil.CustomAttributeNamedArgument;
global using MonoSecurity = Mono.Cecil.SecurityAttribute;

#endif
