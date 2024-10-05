# Absence.Fody

[![NuGet package](https://img.shields.io/nuget/v/Absence.Fody.svg?color=50fa7b&logo=NuGet&style=for-the-badge)](https://www.nuget.org/packages/Absence.Fody)
[![License](https://img.shields.io/github/license/Emik03/Absence.Fody.svg?color=6272a4&style=for-the-badge)](https://github.com/Emik03/Absence.Fody/blob/main/LICENSE)

This is an add-in for [Fody](https://github.com/Fody/Fody) which lets you trim unused private/internal types during compile-time.

This project has a dependency to [Emik.Morsels](https://github.com/Emik03/Emik.Morsels), if you are building this project, refer to its [README](https://github.com/Emik03/Emik.Morsels/blob/main/README.md) first.

---

- [Installation](#installation)
- [Configuration](#configuration)
- [Example](#example)
- [Contribute](#contribute)
- [License](#license)

---

## Installation

- Install the NuGet packages [`Fody`](https://www.nuget.org/packages/Fody) and [`Absence.Fody`](https://www.nuget.org/packages/Absence.Fody). Installing `Fody` explicitly is needed to enable weaving.

  ```
  PM> Install-Package Fody
  PM> Install-Package Absence.Fody
  ```

- Add the `PrivateAssets="all"` metadata attribute to the `<PackageReference />` items of `Fody` and `Absence.Fody` in your project file, so they won't be listed as dependencies.

- If you already have a `FodyWeavers.xml` file in the root directory of your project, add the `<Absence />` tag there. This file will be created on the first build if it doesn't exist:

```xml
<Weavers>
    <Absence />
</Weavers>
```

See [Fody usage](https://github.com/Fody/Home/blob/master/pages/usage.md) for general guidelines, and [Fody Configuration](https://github.com/Fody/Home/blob/master/pages/configuration.md) for additional options.

## Configuration

You can add an `Except` attribute to exclude namespaces or types separated by any amount of whitespace:

```xml
<Weavers>
    <Absence Except="Foo DoNotExcludeMe
                     Do.Not.Exclude.Me" />
</Weavers>
```

Types may either be fully qualified, or not at all. Partially qualified names are not supported due to potential ambiguity.

## Example

What you write:

```csharp
public class Good1
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
        public static void Good10() => Good15<int>();

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
```

What gets compiled:

```csharp
public class Good1
{
    public class Good2
    {
        public static void Good3<[UsedImplicitly] T>() { }

        [UsedImplicitly]
        static void Good4<T>() { }
    }

    [UsedImplicitly]
    class Good5
    {
        public static void Good6() { }

        [UsedImplicitly]
        static void Good7() { }
    }
}

[UsedImplicitly]
class Good8
{
    public class Good9
    {
        public static void Good10() => Good15<int>();

        [UsedImplicitly]
        static void Good11() { }
    }

    [UsedImplicitly]
    class Good12
    {
        public static void Good13() { }

        [UsedImplicitly]
        static void Good14() { }
    }

    static void Good15<[UsedImplicitly] T>() { }
}
```

## Contribute

Issues and pull requests are welcome to help this repository be the best it can be.

## License

This repository falls under the [MPL-2 license](https://www.mozilla.org/en-US/MPL/2.0/).
