# IDeepCloneable
[![NuGet Version](https://img.shields.io/nuget/v/IDeepCloneable?style=flat-square&logo=NuGet&color=0080CC)](https://www.nuget.org/packages/IDeepCloneable/) ![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/arika0093/IDeepCloneable/test.yaml?branch=main&label=Test&style=flat-square) 

Automatic implementation of the `IDeepCloneable<T>` interface via source generators. For library authors.

## Overview
Provides automatic generation of the `DeepClone()` method and `IDeepCloneable<T>` implementation for partial types marked with `[DeepCloneable]`.

### Benefits
While there are many similar libraries available, this library's key feature is that it generates the `DeepClone()` method as an implementation of the `IDeepCloneable<T>` interface.

By doing this:
* Library authors can use `DeepClone()` without reflection (NativeAOT friendly)
* Users are relieved of the burden of manual implementation

```csharp
// 3rd-party library side
public void RegisterCloneMethod<T>()
{
    Func<T, T> cloneFunc = null;

    if(typeof(IDeepCloneable<T>).IsAssignableFrom(typeof(T))) {
        cloneFunc = obj => obj.DeepClone();
    }
    else {
        // fallback implementation
    }
}

// user side
[DeepCloneable]
public partial class MyModel { /* ... */ }
```

## How to use
### Basic Usage
Install the NuGet package [IDeepCloneable](https://www.nuget.org/packages/IDeepCloneable/) to your project.

```bash
dotnet add package IDeepCloneable
```

Then mark a partial type with the `[DeepCloneable]` attribute.

```csharp
using IDeepCloneable;

[DeepCloneable]
public partial class Person
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
person2.ShouldNotBeSameAs(person1);
```

### Customize
As you can see from the generated code, you can simply implement the `IDeepCloneable<T>.DeepClone()` method yourself.

```csharp
using IDeepCloneable;

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

### Library Structure
This consists of two libraries.

### IDeepCloneable
This is the library that defines the `IDeepCloneable<T>` interface and the `[DeepCloneable]` marker attribute.

```csharp
namespace IDeepCloneable;
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