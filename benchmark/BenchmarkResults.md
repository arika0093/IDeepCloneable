```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.7462/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3


```
| Method                    | Mean        | Error       | StdDev      | Median      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------------:|------------:|------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| IDeepCloneable            |    912.7 ns |     5.07 ns |     3.96 ns |    911.3 ns |  1.00 |    0.01 | 0.2890 | 0.0048 |   4.73 KB |        1.00 |
| Manual                    |    953.3 ns |     5.64 ns |     4.71 ns |    954.6 ns |  1.04 |    0.01 | 0.2890 | 0.0048 |   4.73 KB |        1.00 |
| Mapperly                  |    996.6 ns |    14.92 ns |    13.96 ns |    996.9 ns |  1.09 |    0.02 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
| FastCloner_SourceGen      |  1,147.7 ns |    22.47 ns |    30.00 ns |  1,136.9 ns |  1.26 |    0.03 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
| AutoMapper                |  3,139.2 ns |    62.60 ns |    58.56 ns |  3,124.7 ns |  3.44 |    0.06 | 0.3433 | 0.0038 |   5.65 KB |        1.19 |
| MemoryPack                |  7,825.3 ns |    67.83 ns |    63.45 ns |  7,856.4 ns |  8.57 |    0.08 | 0.9003 | 0.0153 |   14.8 KB |        3.13 |
| FastCloner_Reflection     |  8,772.5 ns |    94.85 ns |    79.20 ns |  8,763.9 ns |  9.61 |    0.09 | 0.8392 | 0.0153 |  13.79 KB |        2.91 |
| MessagePack               | 15,678.7 ns |    95.43 ns |    89.27 ns | 15,667.7 ns | 17.18 |    0.12 | 0.8240 | 0.0305 |  13.48 KB |        2.85 |
| SystemTextJson_SourceGen  | 27,303.5 ns |   243.37 ns |   215.74 ns | 27,257.1 ns | 29.92 |    0.26 | 1.2207 |      - |  20.28 KB |        4.28 |
| SystemTextJson_Reflection | 31,857.2 ns |   627.26 ns |   697.20 ns | 31,591.2 ns | 34.90 |    0.76 | 1.2207 |      - |  20.59 KB |        4.35 |
| NewtonsoftJson            | 54,908.2 ns | 1,607.95 ns | 4,664.94 ns | 52,899.3 ns | 60.16 |    5.09 | 2.1362 | 0.1221 |  35.73 KB |        7.55 |
