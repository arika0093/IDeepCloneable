# IDeepCloneable.Benchmark

This project contains performance benchmarks comparing different deep cloning approaches for complex object models in .NET.

## Benchmark Methods

The benchmarks compare the following approaches:

1. **IDeepCloneable** - This library's code-generated deep clone implementation
2. **FastCloner** - Fast deep cloning using reflection and caching
3. **AutoMapper** - Popular object mapping library
4. **Manual Deep Copy** - Handwritten deep copy implementation (baseline for "best possible" performance)
5. **MemoryPack** - High-performance binary serialization (serialize/deserialize approach)
6. **MessagePack** - Efficient binary serialization (serialize/deserialize approach)
7. **System.Text.Json** - .NET's built-in JSON serializer (with reflection)
8. **System.Text.Json (Source Gen)** - .NET's built-in JSON serializer with source generation (no reflection)
9. **Newtonsoft.Json** - Popular JSON serialization library (serialize/deserialize approach)

## Running the Benchmarks

To run the benchmarks:

```bash
cd benchmark/IDeepCloneable.Benchmark
dotnet run -c Release
```

For a quick dry run (single iteration):

```bash
dotnet run -c Release -- --dry
```

For specific benchmarks:

```bash
dotnet run -c Release -- --filter *IDeepCloneable*
```

## Test Model

The benchmark uses a complex JSON-like object model (`ComplexModel`) that includes:

- Nested objects (up to 3 levels deep)
- Collections (Lists and Dictionaries)
- Various data types (strings, numbers, dates, enums)
- Nullable reference types

This model represents a realistic scenario for deep cloning operations.

## Results

Results will vary depending on your system, but in general:

- **Manual implementations** are the fastest (as expected)
- **Code-generated approaches** (IDeepCloneable, FastCloner) are very fast
- **Reflection-based approaches** (AutoMapper) are moderately fast
- **Serialization-based approaches** are slower due to serialization overhead
  - Binary serializers (MemoryPack, MessagePack) are faster than JSON
  - Source-generated serializers are faster than reflection-based ones

See BenchmarkDotNet results for detailed performance metrics including memory allocations.
