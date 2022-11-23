# Absence.Fody

[![NuGet package](https://img.shields.io/nuget/v/Absence.Fody.svg?logo=NuGet)](https://www.nuget.org/packages/Absence.Fody)

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
    <Absence Exclude="Foo DoNotExcludeMe
                      Do.Not.Exclude.Me" />
</Weavers>
```

Types may either be fully qualified, or not at all. Partially qualified names are not supported due to potential ambiguity.

## Example

What you write:

```csharp
public class Public 
{
    public class NestedPublic { }
    
    [ImplicitlyUsed]
    class NestedPrivateWithAttribute { }
    
    class NestedPrivate { }
}

[ImplicitlyUsed]
class InternalWithAttribute
{
    public class NestedPublic { }
    
    [ImplicitlyUsed]
    class NestedPrivateWithAttribute { }
    
    class NestedPrivate { }
}

class Internal 
{
    public class NestedPublic { }
    
    [ImplicitlyUsed]
    class NestedPrivateWithAttribute { }
    
    class NestedPrivate { }
}
```

What gets compiled:

```csharp
public class Public 
{
    public class NestedPublic { }
    
    [ImplicitlyUsed]
    class NestedPrivateWithAttribute { }
}

[ImplicitlyUsed]
class InternalWithAttribute
{
    public class NestedPublic { }
    
    [ImplicitlyUsed]
    class NestedPrivateWithAttribute { }
}
```

## Contribute

Issues and pull requests are welcome to help this repository be the best it can be.

## License

This repository falls under the [MPL-2 license](https://www.mozilla.org/en-US/MPL/2.0/).
