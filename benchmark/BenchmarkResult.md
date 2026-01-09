# Benchmark Results

## CloneBenchmarks

| Method                    | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Mapperly                  |  1.297 μs | 0.0377 μs | 0.1104 μs |  1.241 μs |  0.92 |    0.09 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
| Manual                    |  1.302 μs | 0.0319 μs | 0.0919 μs |  1.273 μs |  0.93 |    0.08 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
| IDeepCloneable            |  1.406 μs | 0.0282 μs | 0.0743 μs |  1.397 μs |  1.00 |    0.07 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
| FastCloner_SourceGen      |  1.441 μs | 0.0341 μs | 0.1001 μs |  1.423 μs |  1.03 |    0.09 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
| AutoMapper                |  4.312 μs | 0.1103 μs | 0.3093 μs |  4.265 μs |  3.07 |    0.27 | 0.3433 |      - |   5.65 KB |        1.19 |
| MemoryPack                |  9.516 μs | 0.2650 μs | 0.7474 μs |  9.318 μs |  6.78 |    0.63 | 0.9003 | 0.0153 |   14.8 KB |        3.13 |
| FastCloner_Reflection     | 11.113 μs | 0.2195 μs | 0.5466 μs | 11.033 μs |  7.92 |    0.56 | 0.8392 | 0.0153 |  13.79 KB |        2.91 |
| MessagePack               | 17.547 μs | 0.3997 μs | 1.1597 μs | 17.201 μs | 12.51 |    1.04 | 0.8240 | 0.0305 |  13.48 KB |        2.85 |
| SystemTextJson_SourceGen  | 30.132 μs | 0.8869 μs | 2.4724 μs | 28.950 μs | 21.48 |    2.07 | 1.2207 |      - |  20.28 KB |        4.28 |
| SystemTextJson_Reflection | 34.843 μs | 0.6084 μs | 0.9472 μs | 34.444 μs | 24.84 |    1.43 | 1.2207 |      - |  20.59 KB |        4.35 |
| NewtonsoftJson            | 57.356 μs | 1.3482 μs | 3.9541 μs | 56.575 μs | 40.90 |    3.49 | 2.1362 | 0.1221 |  35.73 KB |        7.55 |

## Benchmark Environment

```
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat) (container)
AMD EPYC 7763 3.24GHz, 1 CPU, 2 logical cores and 1 physical core
.NET SDK 10.0.100
```
