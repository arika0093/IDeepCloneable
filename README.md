# IDeepCloneable
[![NuGet Version](https://img.shields.io/nuget/v/IDeepCloneable?style=flat-square&logo=NuGet&color=0080CC)](https://www.nuget.org/packages/IDeepCloneable/) ![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/arika0093/IDeepCloneable/test.yaml?branch=main&label=Test&style=flat-square) 

Automatic implementation of the `IDeepCloneable<T>` interface via source generators. Suitable for both library authors and users.

## Overview
Provides automatic generation of the `DeepClone()` method and `IDeepCloneable<T>` implementation for partial types marked with `[DeepCloneable]`.

### Quick Start (for users)
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
using IDeepCloneable;

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
using IDeepCloneable;

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

### Benefits
While there are many similar libraries available, this library's key feature is that it generates the `DeepClone()` method as an implementation of the `IDeepCloneable<T>` interface.

By doing this:
* Library authors can use `DeepClone()` without reflection (NativeAOT friendly)
* Users are relieved of the burden of manual implementation

### Why not use `ICloneable`?
This library implements its own `IDeepCloneable<T>` interface instead of the standard `System.ICloneable` for the following reasons:

* The behavior of `ICloneable.Clone()` is ambiguous—it is unclear whether it performs a shallow or deep copy.
* `ICloneable.Clone()` is non-generic, so you must cast the return value.

For these reasons, even as early as 2004, the use of `ICloneable` was not recommended. [Reference](https://learn.microsoft.com/en-us/archive/blogs/brada/should-we-obsolete-icloneable-the-slar-on-system-icloneable)

## Customize
As you can see from the generated code, you can simply implement the `IDeepCloneable<T>.DeepClone()` method yourself.

```csharp
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

Additionally, it will automatically reference the `IDeepCloneable.Generator`.

### IDeepCloneable.Generator
This is the source generator library that automatically generates the `IDeepCloneable<T>.DeepClone()` method.
There is no need to directly reference this library.

## Benchmark Summary

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat) (container)
AMD EPYC 7763 3.24GHz, 1 CPU, 2 logical cores and 1 physical core
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3


 Method                    | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
-------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
 Mapperly                  |  1.325 μs | 0.0265 μs | 0.0674 μs |  1.314 μs |  0.71 |    0.07 | 0.2880 | 0.0038 |   4.73 KB |        0.83 |
 Manual                    |  1.360 μs | 0.0271 μs | 0.0761 μs |  1.349 μs |  0.73 |    0.08 | 0.2880 | 0.0038 |   4.73 KB |        0.83 |
 FastCloner_SourceGen      |  1.569 μs | 0.0313 μs | 0.0681 μs |  1.554 μs |  0.84 |    0.08 | 0.2880 | 0.0038 |   4.73 KB |        0.83 |
 IDeepCloneable            |  1.886 μs | 0.0613 μs | 0.1760 μs |  1.873 μs |  1.01 |    0.13 | 0.3490 | 0.0057 |   5.72 KB |        1.00 |
 AutoMapper                |  4.353 μs | 0.0838 μs | 0.1857 μs |  4.335 μs |  2.33 |    0.23 | 0.3433 |      - |   5.65 KB |        0.99 |
 MemoryPack                |  9.578 μs | 0.1894 μs | 0.4426 μs |  9.536 μs |  5.12 |    0.51 | 0.9003 | 0.0153 |   14.8 KB |        2.59 |
 FastCloner_Reflection     | 11.529 μs | 0.2278 μs | 0.5501 μs | 11.532 μs |  6.16 |    0.62 | 0.8392 | 0.0153 |  13.79 KB |        2.41 |
 MessagePack               | 17.769 μs | 0.4060 μs | 1.1844 μs | 17.600 μs |  9.50 |    1.05 | 0.8240 | 0.0305 |  13.48 KB |        2.36 |
 SystemTextJson_SourceGen  | 29.829 μs | 0.5786 μs | 1.4832 μs | 29.326 μs | 15.95 |    1.61 | 0.9766 |      - |  20.28 KB |        3.55 |
 SystemTextJson_Reflection | 36.723 μs | 0.7343 μs | 0.6869 μs | 36.416 μs | 19.63 |    1.76 | 1.2207 |      - |  20.59 KB |        3.60 |
 NewtonsoftJson            | 57.806 μs | 1.2501 μs | 3.5867 μs | 57.145 μs | 30.90 |    3.32 | 2.0752 | 0.1221 |  35.73 KB |        6.25 |
