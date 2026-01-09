```
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3
```

| Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Mapperly                  |  1.128 us | 0.0050 us | 0.0045 us |  0.96 |    0.01 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
| IDeepCloneable            |  1.170 us | 0.0099 us | 0.0083 us |  1.00 |    0.01 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
| Manual                    |  1.190 us | 0.0063 us | 0.0056 us |  1.02 |    0.01 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
| FastCloner_SourceGen      |  1.299 us | 0.0059 us | 0.0056 us |  1.11 |    0.01 | 0.2880 | 0.0038 |   4.73 KB |        1.00 |
| AutoMapper                |  3.720 us | 0.0137 us | 0.0128 us |  3.18 |    0.02 | 0.3433 | 0.0038 |   5.65 KB |        1.19 |
| MemoryPack                |  8.382 us | 0.0418 us | 0.0391 us |  7.16 |    0.06 | 0.9003 | 0.0153 |   14.8 KB |        3.13 |
| FastCloner_Reflection     | 10.236 us | 0.0614 us | 0.0575 us |  8.75 |    0.08 | 0.8392 | 0.0153 |  13.79 KB |        2.91 |
| MessagePack               | 15.783 us | 0.0741 us | 0.0657 us | 13.48 |    0.11 | 0.8240 | 0.0305 |  13.48 KB |        2.85 |
| SystemTextJson_SourceGen  | 27.342 us | 0.0772 us | 0.0722 us | 23.36 |    0.17 | 1.2207 |      - |  20.28 KB |        4.28 |
| SystemTextJson_Reflection | 33.114 us | 0.0818 us | 0.0639 us | 28.29 |    0.20 | 1.2207 |      - |  20.59 KB |        4.35 |
| NewtonsoftJson            | 50.827 us | 0.1674 us | 0.1484 us | 43.43 |    0.32 | 2.1362 | 0.1221 |  35.73 KB |        7.55 |