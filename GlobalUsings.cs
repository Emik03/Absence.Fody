#region Emik.MPL

// <copyright file="GlobalUsings.cs" company="Emik">
// Copyright (c) Emik. This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// </copyright>

#endregion

#region

global using Fody;
global using Mono.Cecil;
global using Mono.Cecil.Cil;
global using Mono.Cecil.Rocks;
global using IMonoProvider = Mono.Cecil.ICustomAttributeProvider;
global using MonoMethodBody = Mono.Cecil.Cil.MethodBody;
global using MonoNamedArgument = Mono.Cecil.CustomAttributeNamedArgument;
global using MonoSecurity = Mono.Cecil.SecurityAttribute;

#endregion
