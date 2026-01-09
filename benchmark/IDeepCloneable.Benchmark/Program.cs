using BenchmarkDotNet.Running;
using IDeepCloneable.Benchmark;

BenchmarkRunner.Run<CloneBenchmarks>(args: args);
