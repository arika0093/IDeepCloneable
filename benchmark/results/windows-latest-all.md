```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.32230/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v3


```
| Method                    | Mean        | Error       | StdDev      | Median      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------------:|------------:|------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| IDeepCloneable            |    953.4 ns |    12.77 ns |    11.32 ns |    948.9 ns |  1.00 |    0.02 | 0.2890 | 0.0048 |   4.73 KB |        1.00 |
| Manual                    |    959.2 ns |     7.10 ns |     6.30 ns |    958.7 ns |  1.01 |    0.01 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
| FastCloner_SourceGen      |    970.0 ns |    11.89 ns |     9.93 ns |    968.0 ns |  1.02 |    0.02 | 0.2890 | 0.0048 |   4.73 KB |        1.00 |
| Mapperly                  |    972.0 ns |     8.06 ns |     7.14 ns |    972.1 ns |  1.02 |    0.01 | 0.2890 | 0.0048 |   4.73 KB |        1.00 |
| AutoMapper                |  3,128.6 ns |    22.77 ns |    21.30 ns |  3,132.8 ns |  3.28 |    0.04 | 0.3433 | 0.0038 |   5.65 KB |        1.19 |
| FastCloner_Reflection     |  7,672.7 ns |    83.30 ns |    77.92 ns |  7,672.5 ns |  8.05 |    0.12 | 1.0681 | 0.0458 |  17.51 KB |        3.70 |
| MemoryPack                |  7,720.7 ns |    55.77 ns |    52.17 ns |  7,726.5 ns |  8.10 |    0.11 | 0.9003 | 0.0153 |   14.8 KB |        3.13 |
| MessagePack               | 15,572.4 ns |    89.60 ns |    83.81 ns | 15,557.0 ns | 16.34 |    0.20 | 0.8240 | 0.0305 |  13.48 KB |        2.85 |
| SystemTextJson_SourceGen  | 28,513.1 ns |   569.24 ns | 1,396.36 ns | 28,335.3 ns | 29.91 |    1.49 | 1.2207 | 0.0610 |  20.28 KB |        4.28 |
| SystemTextJson_Reflection | 32,345.9 ns |   473.25 ns |   442.68 ns | 32,437.3 ns | 33.93 |    0.59 | 1.2207 |      - |  20.59 KB |        4.35 |
| NewtonsoftJson            | 53,205.5 ns | 1,063.76 ns | 3,069.19 ns | 51,678.8 ns | 55.81 |    3.27 | 2.1362 | 0.1221 |  35.73 KB |        7.55 |
