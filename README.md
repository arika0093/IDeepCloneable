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
```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat) (container)
AMD EPYC 7763 3.24GHz, 1 CPU, 2 logical cores and 1 physical core
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3


```
| Method                    | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Mapperly                  |  1.280 μs | 0.0283 μs | 0.0825 μs |  1.267 μs |  0.94 |    0.09 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
| Manual                    |  1.363 μs | 0.0340 μs | 0.1002 μs |  1.345 μs |  1.00 |    0.10 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
| IDeepCloneable            |  1.367 μs | 0.0319 μs | 0.0935 μs |  1.348 μs |  1.00 |    0.09 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
| FastCloner_SourceGen      |  1.492 μs | 0.0303 μs | 0.0864 μs |  1.484 μs |  1.10 |    0.10 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
| AutoMapper                |  4.189 μs | 0.0832 μs | 0.2290 μs |  4.170 μs |  3.08 |    0.26 | 0.3433 |      - |   5.65 KB |        1.19 |
| MemoryPack                |  9.309 μs | 0.1914 μs | 0.5522 μs |  9.107 μs |  6.84 |    0.60 | 0.9003 | 0.0153 |   14.8 KB |        3.13 |
| FastCloner_Reflection     | 11.635 μs | 0.3546 μs | 0.9943 μs | 11.265 μs |  8.55 |    0.92 | 0.8392 | 0.0153 |  13.79 KB |        2.91 |
| MessagePack               | 18.525 μs | 0.7076 μs | 2.0417 μs | 17.891 μs | 13.61 |    1.74 | 0.8240 | 0.0305 |  13.48 KB |        2.85 |
| SystemTextJson_SourceGen  | 35.568 μs | 1.9284 μs | 5.3757 μs | 34.023 μs | 26.13 |    4.29 | 1.2207 |      - |  20.28 KB |        4.28 |
| SystemTextJson_Reflection | 43.192 μs | 2.4626 μs | 6.9055 μs | 40.847 μs | 31.73 |    5.46 | 0.9766 |      - |  20.59 KB |        4.35 |
| NewtonsoftJson            | 59.850 μs | 1.6627 μs | 4.7706 μs | 59.482 μs | 43.97 |    4.52 | 2.0752 | 0.1221 |  35.73 KB |        7.55 |
