```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v3


```
| Method         | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------- |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| IDeepCloneable | 1.133 μs | 0.0223 μs | 0.0367 μs |  1.00 |    0.04 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
