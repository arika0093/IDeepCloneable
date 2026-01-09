```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.7462/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3


```
| Method                    | Mean        | Error     | StdDev      | Median      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| Manual                    |    962.9 ns |  18.79 ns |    26.95 ns |    951.6 ns |  0.97 |    0.03 | 0.2890 | 0.0048 |   4.73 KB |        1.00 |
| Mapperly                  |    966.2 ns |  17.45 ns |    15.47 ns |    964.7 ns |  0.98 |    0.02 | 0.2890 | 0.0048 |   4.73 KB |        1.00 |
| IDeepCloneable            |    988.5 ns |  19.31 ns |    19.83 ns |    989.0 ns |  1.00 |    0.03 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
| FastCloner_SourceGen      |  1,103.1 ns |  14.30 ns |    13.37 ns |  1,099.9 ns |  1.12 |    0.03 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
| AutoMapper                |  3,240.0 ns |  64.01 ns |    71.15 ns |  3,232.9 ns |  3.28 |    0.10 | 0.3433 | 0.0038 |   5.65 KB |        1.19 |
| MemoryPack                |  7,664.3 ns | 101.55 ns |    90.02 ns |  7,646.5 ns |  7.76 |    0.18 | 0.9003 | 0.0153 |   14.8 KB |        3.13 |
| FastCloner_Reflection     |  8,647.2 ns |  97.25 ns |    90.97 ns |  8,614.0 ns |  8.75 |    0.19 | 0.8392 | 0.0153 |  13.79 KB |        2.91 |
| MessagePack               | 15,371.2 ns | 192.81 ns |   170.92 ns | 15,320.9 ns | 15.56 |    0.35 | 0.8240 | 0.0305 |  13.48 KB |        2.85 |
| SystemTextJson_SourceGen  | 26,478.7 ns | 317.83 ns |   297.30 ns | 26,479.0 ns | 26.80 |    0.60 | 1.2207 | 0.0610 |  20.28 KB |        4.28 |
| SystemTextJson_Reflection | 33,309.3 ns | 638.93 ns | 1,660.66 ns | 32,719.3 ns | 33.71 |    1.80 | 1.2207 |      - |  20.59 KB |        4.35 |
| NewtonsoftJson            | 51,030.2 ns | 673.69 ns |   597.21 ns | 50,815.4 ns | 51.64 |    1.17 | 2.1362 | 0.1221 |  35.73 KB |        7.55 |
