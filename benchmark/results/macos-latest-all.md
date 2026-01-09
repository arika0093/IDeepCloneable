```

BenchmarkDotNet v0.15.8, macOS Sequoia 15.7.3 (24G419) [Darwin 24.6.0]
Apple M1 (Virtual), 1 CPU, 3 logical and 3 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a


```
| Method                    | Mean        | Error     | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| Mapperly                  |    882.8 ns |  16.63 ns |    17.08 ns |  0.86 |    0.07 | 0.7725 | 0.0124 |   4.73 KB |        1.00 |
| Manual                    |    949.0 ns |  18.55 ns |    17.35 ns |  0.93 |    0.08 | 0.7725 | 0.0114 |   4.73 KB |        1.00 |
| IDeepCloneable            |  1,031.1 ns |  30.13 ns |    87.42 ns |  1.01 |    0.12 | 0.7725 | 0.0124 |   4.73 KB |        1.00 |
| FastCloner_SourceGen      |  1,081.5 ns |  31.59 ns |    92.65 ns |  1.06 |    0.13 | 0.7725 | 0.0114 |   4.73 KB |        1.00 |
| AutoMapper                |  2,495.5 ns | 120.21 ns |   352.54 ns |  2.44 |    0.40 | 0.9193 | 0.0153 |   5.65 KB |        1.19 |
| MemoryPack                |  5,331.3 ns | 106.63 ns |   311.04 ns |  5.21 |    0.53 | 2.4109 | 0.0687 |   14.8 KB |        3.13 |
| FastCloner_Reflection     |  7,509.3 ns | 210.09 ns |   616.16 ns |  7.33 |    0.86 | 2.2430 | 0.0610 |  13.79 KB |        2.91 |
| MessagePack               |  9,301.7 ns | 184.21 ns |   434.21 ns |  9.09 |    0.87 | 2.1973 | 0.0610 |  13.48 KB |        2.85 |
| SystemTextJson_SourceGen  | 17,315.8 ns | 473.73 ns | 1,374.37 ns | 16.91 |    1.95 | 3.2959 | 0.1221 |  20.28 KB |        4.28 |
| SystemTextJson_Reflection | 18,829.0 ns | 365.26 ns |   535.39 ns | 18.39 |    1.62 | 3.2959 | 0.1221 |  20.59 KB |        4.35 |
| NewtonsoftJson            | 32,287.8 ns | 642.17 ns | 1,090.46 ns | 31.54 |    2.84 | 5.3711 |      - |  35.73 KB |        7.55 |
