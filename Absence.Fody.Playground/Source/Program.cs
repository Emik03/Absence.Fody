﻿// SPDX-License-Identifier: MPL-2.0
// ReSharper disable once RedundantUsingDirective.Global
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnusedAttribute = Absence.Fody.Playground.UnusedAttribute; // ReSharper disable once CheckNamespace
#pragma warning disable 1591, RCS1102
namespace Absence.Fody.Playground;

public class Good1 // ReSharper disable ClassNeverInstantiated.Local UnusedMember.Local UnusedType.Local
{
    public class Good2
    {
        public static void Good3<[UsedImplicitly] T>() { }

        [UsedImplicitly]
        static void Good4<T>() { }

        static void Bad1() { }
    }

    [UsedImplicitly]
    class Good5
    {
        public static void Good6() { }

        [UsedImplicitly]
        static void Good7() { }

        static void Bad2() { }
    }

    class Bad3
    {
        public static void Bad4() { }

        [UsedImplicitly]
        static void Bad5() { }

        static void Bad6() { }
    }
}

[UsedImplicitly]
class Good8
{
    public class Good9
    {
        public static void Good10()
        {
            Good15<int>();
            _ = new Good16();
        }

        [UsedImplicitly]
        static void Good11() { }

        static void Bad7() { }
    }

    [UsedImplicitly]
    class Good12
    {
        public static void Good13() { }

        [UsedImplicitly]
        static void Good14() { }

        static void Bad8() { }
    }

    class Bad9
    {
        public static void Bad10() { }

        [UsedImplicitly]
        static void Bad11() { }

        static void Bad12() { }
    }

    static void Good15<[UsedImplicitly] T>() { }

    static void Bad13<[UsedImplicitly] T>() { }

    sealed class Good16
    {
        internal static List<KeyValuePair<string, Good17>> s_field = [];

        [Good18.Good19]
        internal struct Good17
        {
            sealed class Good18 : Attribute
            {
                internal sealed class Good19 : Attribute;
            }
        }
    }
}

class Bad14
{
    public class Bad15
    {
        public static void Bad16() { }

        [UsedImplicitly]
        static void Bad17() { }

        static void Bad18() { }
    }

    [UsedImplicitly]
    class Bad19
    {
        public static void Bad20() { }

        [UsedImplicitly]
        static void Bad21() { }

        static void Bad22() { }
    }

    class Bad23
    {
        public static void Bad24() { }

        [UsedImplicitly]
        static void Bad25() { }

        static void Bad26() { }
    }
}

/// <summary>This attribute should be inlined.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property)]
sealed partial class UnusedAttribute : Attribute;
