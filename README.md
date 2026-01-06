# IDeepCloneable
[![NuGet Version](https://img.shields.io/nuget/v/IDeepCloneable?style=flat-square&logo=NuGet&color=0080CC)](https://www.nuget.org/packages/IDeepCloneable/) ![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/arika0093/IDeepCloneable/test.yaml?branch=main&label=Test&style=flat-square) 

Automatic implementation of the `IDeepCloneable<T>` interface via source generators. For library authors.

## Overview
Provides automatic generation of the `DeepClone()` method for types implementing `IDeepCloneable<T>`.
This works not only for `IDeepCloneable<T>` itself, but also for interfaces and abstract classes that inherit from it.

## How to use
Install the NuGet package [IDeepCloneable](https://www.nuget.org/packages/IDeepCloneable/) to your project.

```bash
dotnet add package IDeepCloneable
```

Then create a partial class that implements `IDeepCloneable<T>`.

```csharp
using IDeepCloneable;

public partial class Person : IDeepCloneable<Person>
{
  public string Name { get; set; }
  public int Age { get; set; }
}
```

That's it! The `DeepClone()` method will be automatically generated.

```csharp
var person1 = new Person { Name = "Alice", Age = 30 };
var person2 = person1.DeepClone();
person2.ShouldNotBeSameAs(person1);
```

You can also use interfaces or abstract classes.

```csharp
using IDeepCloneable;

// -------
// interface example
public interface IPerson : IDeepCloneable<IPerson>
{
  string Name { get; }
  int Age { get; }
}

// and then
public partial class Person : IPerson
{
  public string Name { get; set; }
  public int Age { get; set; }
  // -> DeepClone() will be generated!
}

// -------
// abstract class example
public abstract partial class PersonBase : IDeepCloneable<PersonBase>
{
    public string Name { get; set; }
    public int Age { get; set; }
    // public abstract PersonBase DeepClone(); <- this declare is automatically added
}

public partial class Person : PersonBase
{
    public string Address { get; set; }
    // -> Person.DeepClone() also will be generated
}
```

## Why this library?
### Problem
#### How to use DeepClone in your library?
There are many libraries that implement DeepCopy. Why is this library necessary?

Traditional libraries implement copy methods by adding some kind of attribute to specific types.

```csharp
[DeepCloneable]
public partial class Person { /* ... */ }

// or
[DeepCloneable<Person>]
public partial class PersonCloneGenerator;
```

This approach is not bad, but the automatically generated code cannot be accessed from the **another library side**.  
In other words, if the library wants to call the `Clone` method, it has to choose one of the following approaches:

#### 1. Implement a generic `Clone` using reflection

<details>
<summary>Example Code</summary>

```csharp
// 3rd-party library
public static T DeepClone<T>(T obj)
{
  // Copy using, for example, JsonSerialize/Deserialize
  var json = JsonSerializer.Serialize(obj);
  return JsonSerializer.Deserialize<T>(json);
}

// Nothing is required on the user side
```

</details>

This approach uses reflection, which results in poor performance. Also, NativeAOT cannot be used.

#### 2. Have the user implement some `ICloneable` interface

<details>
<summary>Example Code</summary>

```csharp
// 3rd-party library
public interface ICloneable<T>
{
  T Clone();
}

public void SomeMethod<T>(T obj) where T : ICloneable<T>
{
  var clone = obj.Clone();
  // ...
}

// user-side
public partial class Person : ICloneable<Person>
{
  public Person Clone()
  {
  // Must be implemented manually
  }
}
```

</details>

This method allows free implementation and use of any library, but it is obviously tedious.

#### 3. Have the user specify a method for cloning

<details>
<summary>Example Code</summary>

```csharp
// 3rd-party library
public class MethodConfig
{
  private Func<T, T> _cloneFunc;

  public MethodConfig SetCloneFunc<T>(Func<T, T> cloneFunc)
  {
  _cloneFunc = cloneFunc;
  return this;
  }
}

// user-side
var config = new MethodConfig()
  .SetCloneFunc<Person>(person => 
  {
  // Must be implemented manually
  });
```

</details>

This method also allows free implementation, but again, manual implementation is required.

### Solution
Add "IDeepCloneable" as a dependency on the **library side**.

#### Library Authors
On the library side, define abstract classes or interfaces that implement `IDeepCloneable<T>`.
Then, check for `IDeepCloneable<T>` and call the `DeepClone()` method.

```csharp
using IDeepCloneable;

// Optionally: If you want to define your own interface or abstract class on the library side
public interface ILibraryModel<T> : IDeepCloneable<T>
{
  // Define properties and methods required by the library
}

public class LibraryConfiguration<T>
{
  private Func<T, T> _cloneFunc;

  public LibraryConfiguration()
  {
    // Check if T implements IDeepCloneable<T>
    // Or, you can add a type constraint: where T : IDeepCloneable<T>
    if(typeof(IDeepCloneable<T>).IsAssignableFrom(typeof(T)))
    {
      _cloneFunc = obj => obj.DeepClone();
    }
    else
    {
      // fallback implementation
    }
  }
}
```

#### Users of the Library
Library users simply inherit the abstract class or interface defined above and add the `partial` keyword.

```csharp
using YourLibrary;

public partial class MyModel : ILibraryModel<MyModel>
{
  public string Name { get; set; }
  public int Age { get; set; }
}
```

### Library Structure
This consists of two libraries.

### IDeepCloneable
This is the library that defines the `IDeepCloneable<T>` interface. It contains only the [following content](src/IDeepCloneable/IDeepCloneable.cs).

```csharp
namespace IDeepCloneable;
public interface IDeepCloneable<T>
{
    T DeepClone();
}
```

Additionally, it will automatically reference the `IDeepCloneable.Generator`.

### IDeepCloneable.Generator
This is the source generator library that automatically generates the `IDeepCloneable<T>.DeepClone()` method.
There is no need to directly reference this library.