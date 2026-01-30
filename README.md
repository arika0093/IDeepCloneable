# IDeepCloneable
[![NuGet Version](https://img.shields.io/nuget/v/IDeepCloneable?style=flat-square&logo=NuGet&color=0080CC)](https://www.nuget.org/packages/IDeepCloneable/) ![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/arika0093/IDeepCloneable/test.yaml?branch=main&label=Test&style=flat-square) 

Automatic implementation of the `IDeepCloneable<T>` interface via source generators. Suitable for both library authors and users.

## Overview
Provides automatic generation of the `DeepClone()` method and `IDeepCloneable<T>` implementation for partial types marked with `[DeepCloneable]`.  

This library supports the following three usage scenarios:

1. [For end users](#quick-start-for-end-users): Simply add the `[DeepCloneable]` attribute to your model class and the `DeepClone()` method will be generated automatically.
2. [For library authors](#usage-for-library-authors): Use the `IDeepCloneable<T>` interface to call `DeepClone()` without reflection.
3. [For advanced library authors](#customize-for-library-authors): Integrate the generator to automatically implement `IDeepCloneable<T>` for types marked with your own custom attributes.

### Quick Start (for end users)
Install the NuGet package [IDeepCloneable](https://www.nuget.org/packages/IDeepCloneable/) to your project.

```bash
dotnet add package IDeepCloneable
```

Then mark a partial type with the `[DeepCloneable]` attribute.

```csharp
[DeepCloneable] // <- add this attribute
public partial class Person // <- make it partial
{
    public string Name { get; set; }
    public int Age { get; set; }
}
```

That's it! The `DeepClone()` method will be automatically generated and the generated partial type will implement `IDeepCloneable<Person>`.

```csharp
// generated code (sample)
partial class Person : IDeepCloneable<Person>
{
    public Person DeepClone()
    {
        return new Person
        {
            Name = this.Name,
            Age = this.Age,
        };
    }
}
```

And you can use it like this:

```csharp
var person1 = new Person { Name = "Alice", Age = 30 };
var person2 = person1.DeepClone();
```

### Usage for library authors
Library authors can use the `IDeepCloneable<T>` interface to perform `DeepClone()` without reflection.

First, install the NuGet package [IDeepCloneable](https://www.nuget.org/packages/IDeepCloneable/) to your project.

```bash
dotnet add package IDeepCloneable
```

Then, you can check if a type implements `IDeepCloneable<T>` and call the `DeepClone()` method accordingly.

```csharp
public void RegisterCloneMethod<T>()
{
    Func<T, T> cloneFunc = null;

    bool isDeepCloneable = typeof(IDeepCloneable<T>).IsAssignableFrom(typeof(T));
    if(isDeepCloneable) {
        cloneFunc = value => ((IDeepCloneable<T>)value).DeepClone();
    }
    else {
        // fallback implementation
    }
}

// or using generic constraints
public void RegisterCloneMethod<T>() where T : IDeepCloneable<T>
{
    Func<T, T> cloneFunc = value => value.DeepClone();
}
```

This completes the setup. Library users do not need to introduce `IDeepCloneable`; they only need to apply `[DeepCloneable]`.

```csharp
// user side
[DeepCloneable]
public partial class MyModel { /* ... */ }

// call library method
library.RegisterCloneMethod<MyModel>();
```

## Benefits
While there are many similar libraries available, this library's key feature is that it generates the `DeepClone()` method as an implementation of the `IDeepCloneable<T>` interface.

By doing this:
* Library authors can use `DeepClone()` without reflection (NativeAOT friendly)
* Users are relieved of the burden of manual implementation

## Performance
Performance is a concern, right? In benchmarks for [medium-sized models](./benchmark/IDeepCloneable.Benchmark/TestDataGenerator.cs), it shows comparable results to major libraries.

| Method                    | Mean        | Ratio | Gen0   | Gen1   | Allocated |
|-------------------------- |------------:|------:|-------:|-------:|----------:|
| IDeepCloneable            |    912.7 ns |  1.00 | 0.2890 | 0.0048 |   4.73 KB |
| Mapperly                  |    996.6 ns |  1.09 | 0.2880 | 0.0038 |   4.73 KB |
| FastCloner_SourceGen      |  1,147.7 ns |  1.26 | 0.2880 | 0.0038 |   4.73 KB |
| AutoMapper                |  3,139.2 ns |  3.44 | 0.3433 | 0.0038 |   5.65 KB |
| FastCloner_Reflection     |  8,772.5 ns |  9.61 | 0.8392 | 0.0153 |  13.79 KB |
| SystemTextJson_Reflection | 31,857.2 ns | 34.90 | 1.2207 |      - |  20.59 KB |

Detailed results can be found in [benchmark/results](benchmark/results) and [Benchmark source code](benchmark/IDeepCloneable.Benchmark/).

## Features (for end users)
### Shallow Clone
By default, all properties are deeply cloned. However, you can specify that certain properties should be shallowly cloned by using the `[ShallowClone]` attribute.

```csharp
[DeepCloneable]
public partial class Person
{
    public string Name { get; set; }

    [ShallowClone] // <- add this attribute
    public Address Address { get; set; }
}
```

### Ignore Clone
If you want to exclude certain properties from being cloned, you can use the `[IgnoreClone]` attribute.

```csharp
[DeepCloneable]
public partial class Person
{
    public string Name { get; set; }

    [IgnoreClone] // <- add this attribute
    public int TempId { get; set; }
}
```

### Circular Reference
By default, This library supports object graphs with circular references.

```csharp
[DeepCloneable]
public partial class Node
{
    public string Name { get; set; }
    public Node Next { get; set; }
}

var node1 = new Node { Name = "Node1" };
var node2 = new Node { Name = "Node2" };
node1.Next = node2;
node2.Next = node1;

var clonedNode1 = node1.DeepClone();
// clonedNode1 != node1
// clonedNode1.Next != node2
// clonedNode1.Next.Next == clonedNode1
```

### Generic Types
We also support classes that have generic types as members.
If a member type implements `IDeepCloneable<T>`, it will be deep-cloned automatically.
Otherwise, you must generate a `DeepClone` implementation for that type by applying the `[GenerateDeepCloneable]` attribute.

```csharp
[DeepCloneable]
[GenerateDeepCloneable(typeof(MyCloneClass))] // <- add this attribute
[GenerateDeepCloneable(typeof(MyOtherCloneWantClass))]
public partial class GenericBox<T>
{
    public T? Value { get; set; } // you can use generic type T here
}

public class MyCloneClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MyOtherCloneWantClass
{
    public DateTime Timestamp { get; set; }
}
```

### Manual Implementation
If you want to provide your own implementation of the `DeepClone()` method, you can do so by implementing the `IDeepCloneable<T>` interface directly.

```csharp
// no attribute needed
public class Person : IDeepCloneable<Person>
{
    public string Name { get; set; }
    public int Age { get; set; }

    public Person DeepClone()
    {
        // your custom implementation
    }
}
```

## Customize (For library authors)
### Using only the generation logic
You might want to enable the same functionality for your own library-specific attribute, instead of the obvious `[DeepCloneable]` attribute name. Here’s how you can do it.

First, create a source generator project and add the `IDeepCloneable.Generator.Source` NuGet package.
It’s also recommended to add the [Polyfill library](https://github.com/SimonCropp/Polyfill).  

<details>
<summary>Example .csproj for source generator project</summary>

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>netstandard2.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
        <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
        <IsRoslynComponent>true</IsRoslynComponent>
        <AnalyzerLanguage>cs</AnalyzerLanguage>
        <IncludeBuildOutput>false</IncludeBuildOutput>
        <DevelopmentDependency>true</DevelopmentDependency>
        <IncludeSymbols>false</IncludeSymbols>
        <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
        <SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>
    </PropertyGroup>
    <ItemGroup>
        <None Include="$(OutputPath)\$(AssemblyName).dll" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
    </ItemGroup>
    <ItemGroup>
        <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.4.0" />
        <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.11.0">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="IDeepCloneable.Generator.Source" Version="*">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
        <PackageReference Include="Polyfill" Version="*">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>
</Project>
```

</details>

> [!TIP]
> If you’re familiar with Source Generators, you might wonder if it’s okay to add third-party libraries. Both `IDeepCloneable.Generator.Source` and `Polyfill` are [source-only NuGet packages](https://andrewlock.net/creating-source-only-nuget-packages/), so they work fine in source generator libraries.


Next, add code like the following to your source generator:

```csharp
using IDeepCloneable.Generator;

// Generator
[Generator]
internal class MyCloneableGenerator : CloneableGeneratorCore<MyGeneratorOptions>;

// Options
internal class MyGeneratorOptions : CloneableGeneratorOptionsCore
{
    // The DeepClone implementation will be generated for types with this attribute.
    public override string AttributeMetadataName => "MyLibrary.MyLibraryAttribute";

    // Specify the interface name that generated types should implement. If empty, only a partial class is generated.
    public override string ImplementedInterfaceName => "global::MyLibrary.IMyLibraryClass";

    // There are other options you can override as needed.
}
```

Finally, define the required attribute and interface in your library:

```csharp
namespace MyLibrary;

// Marker Attribute
public sealed class MyLibraryAttribute : Attribute;

// Your Interface
public interface IMyLibraryClass<T>
{
    T DeepClone(); // add it
}
```

## Library Structure
This consists of two libraries.

### IDeepCloneable
This is the library that defines the `IDeepCloneable<T>` interface and the `[DeepCloneable]` marker attribute.  
To allow users of third-party libraries to use it without worrying about `IDeepCloneable`, it is defined directly under the global namespace.

```csharp
public sealed class DeepCloneableAttribute : Attribute;
public interface IDeepCloneable<T>
{
    T DeepClone();
}
```

### IDeepCloneable.Generator.Source
This library provides the source code for the source generator.
It is intended for library authors who want to create their own attributes and interfaces that automatically implement `DeepClone`.

## FAQ
### What is DeepClone?
DeepClone (also commonly referred to as DeepCopy) refers to the operation of creating a complete copy of an object.

For example, if you simply assign an object, reference-type properties are not copied, and both variables will point to the same instance.
```csharp
var person1 = new Person
{
    Name = "Alice",
    Address = new Address { City = "Wonderland" }
};
var person2 = person1; // shallow copy
person2.Address.City = "New City";

// person1.Address.City is now "New City"
```

Additionally, in the following example, the `Address` property is a shallow copy, which is insufficient.
```csharp
var person3 = new Person
{
    Name = person1.Name,
    Address = person1.Address // shallow copy
};
person3.Address.City = "Another City";
// person1.Address.City is now "Another City"
```

To avoid this, you would need to manually copy everything.
```csharp
var person4 = new Person
{
    Name = person1.Name,
    Address = new Address { City = person1.Address.City }
};
person4.Address.City = "Different City";
// person1.Address.City remains unchanged
```

Writing this every time is tedious. Instead, you can use the `DeepClone()` method.

```csharp
public class Person
{
    public string Name { get; set; }
    public Address Address { get; set; }

    public Person DeepClone()
    {
        return new Person
        {
            Name = this.Name,
            Address = new Address
            {
                City = this.Address.City
            }
        };
    }
}

var person5 = person1.DeepClone();
person5.Address.City = "Cloned City";
// person1.Address.City remains unchanged
```

While this works, it is still a bit cumbersome. With this library, you can automatically generate the implementation of the `DeepClone()` method.

```csharp
[DeepCloneable] // <- add this attribute
public partial class Person // <- make it partial
{
    public string Name { get; set; }
    public Address Address { get; set; }
}

var person6 = person1.DeepClone();
person6.Address.City = "Auto Cloned City";
// person1.Address.City remains unchanged
```

### Why not use `ICloneable`?
This library implements its own `IDeepCloneable<T>` interface instead of the standard `System.ICloneable` for the following reasons:

* The behavior of `ICloneable.Clone()` is ambiguous—it is unclear whether it performs a shallow or deep copy.
* `ICloneable.Clone()` is non-generic, so you must cast the return value.

For these reasons, even as early as 2004, the use of `ICloneable` was not recommended. [Reference](https://learn.microsoft.com/en-us/archive/blogs/brada/should-we-obsolete-icloneable-the-slar-on-system-icloneable)

