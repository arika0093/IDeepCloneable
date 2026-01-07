# IDeepCloneable.Benchmark
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
