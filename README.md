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

Detailed results can be found in [BenchmarkResults.md](benchmark/BenchmarkResults.md) and [Benchmark source code](benchmark/IDeepCloneable.Benchmark/).

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

### Why not use `ICloneable`?
This library implements its own `IDeepCloneable<T>` interface instead of the standard `System.ICloneable` for the following reasons:

* The behavior of `ICloneable.Clone()` is ambiguous—it is unclear whether it performs a shallow or deep copy.
* `ICloneable.Clone()` is non-generic, so you must cast the return value.

For these reasons, even as early as 2004, the use of `ICloneable` was not recommended. [Reference](https://learn.microsoft.com/en-us/archive/blogs/brada/should-we-obsolete-icloneable-the-slar-on-system-icloneable)

