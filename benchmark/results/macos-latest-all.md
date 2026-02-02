```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.7.3 (24G419) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.225.61305), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.2 (10.0.2, 10.0.225.61305), Arm64 RyuJIT armv8.0-a


```
| Method                    | Mean        | Error     | StdDev      | Median      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| IDeepCloneable            |    988.8 ns |  19.38 ns |    54.02 ns |    983.9 ns |  1.00 |    0.08 | 0.7725 | 0.0124 |   4.73 KB |        1.00 |
| Mapperly                  |    991.6 ns |  35.39 ns |   102.68 ns |    949.9 ns |  1.01 |    0.12 | 0.7725 | 0.0114 |   4.73 KB |        1.00 |
| Manual                    |  1,083.8 ns |  36.79 ns |   107.31 ns |  1,049.5 ns |  1.10 |    0.12 | 0.7725 | 0.0114 |   4.73 KB |        1.00 |
| FastCloner_SourceGen      |  1,102.0 ns |  26.24 ns |    76.96 ns |  1,106.3 ns |  1.12 |    0.10 | 0.7725 | 0.0114 |   4.73 KB |        1.00 |
| AutoMapper                |  2,136.0 ns |  49.51 ns |   140.44 ns |  2,082.3 ns |  2.17 |    0.18 | 0.9193 | 0.0153 |   5.65 KB |        1.19 |
| MemoryPack                |  5,265.9 ns | 104.02 ns |    81.21 ns |  5,285.8 ns |  5.34 |    0.29 | 2.4109 | 0.0687 |   14.8 KB |        3.13 |
| MessagePack               | 10,682.3 ns | 484.25 ns | 1,420.23 ns | 10,469.7 ns | 10.83 |    1.55 | 2.1973 | 0.0610 |  13.48 KB |        2.85 |
| FastCloner_Reflection     | 11,479.8 ns | 751.72 ns | 2,204.66 ns | 11,532.0 ns | 11.64 |    2.31 | 2.8534 | 0.1221 |  17.51 KB |        3.70 |
| SystemTextJson_SourceGen  | 19,700.0 ns | 825.26 ns | 2,433.29 ns | 19,296.7 ns | 19.98 |    2.68 | 3.2959 | 0.1221 |  20.28 KB |        4.28 |
| SystemTextJson_Reflection | 21,437.0 ns | 687.20 ns | 2,015.45 ns | 21,289.1 ns | 21.74 |    2.34 | 3.2959 | 0.1221 |  20.59 KB |        4.35 |
| NewtonsoftJson            | 35,123.5 ns | 294.74 ns |   230.12 ns | 35,027.1 ns | 35.62 |    1.90 | 5.3711 |      - |  35.73 KB |        7.55 |
