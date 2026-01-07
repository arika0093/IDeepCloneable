using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using MemoryPack;
using MessagePack;
using Newtonsoft.Json;
using Riok.Mapperly.Abstractions;

namespace IDeepCloneable.Benchmark;

/// <summary>
/// Benchmarks comparing different deep cloning approaches for complex object models.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class CloneBenchmarks
{
    private ComplexModel _model = null!;
    private IMapper _autoMapper = null!;
    private JsonSerializerOptions _systemTextJsonOptions = null!;
    private JsonSerializerOptions _systemTextJsonSourceGenOptions = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _model = TestDataGenerator.CreateSampleModel();
        
        // Setup AutoMapper
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ComplexModel, ComplexModel>();
            cfg.CreateMap<UserInfo, UserInfo>();
            cfg.CreateMap<ContactInfo, ContactInfo>();
            cfg.CreateMap<DataItem, DataItem>();
            cfg.CreateMap<SubItem, SubItem>();
            cfg.CreateMap<Settings, Settings>();
            cfg.CreateMap<AdvancedSettings, AdvancedSettings>();
        });
        _autoMapper = config.CreateMapper();
        
        // Setup System.Text.Json options
        _systemTextJsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        
        // Setup System.Text.Json with source generation
        _systemTextJsonSourceGenOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = BenchmarkJsonContext.Default,
        };
    }

    [Benchmark(Baseline = true)]
    public ComplexModel IDeepCloneable()
    {
        return _model.DeepClone();
    }

    [Benchmark]
    public ComplexModel Manual()
    {
        return ManualMapper.DeepCopy(_model);
    }

    [Benchmark]
    public ComplexModel FastCloner_Clone()
    {
        return FastCloner.FastCloner.DeepClone(_model)!;
    }

    [Benchmark]
    public ComplexModel AutoMapper()
    {
        return _autoMapper.Map<ComplexModel>(_model);
    }

    [Benchmark]
    public ComplexModel Mapperly()
    {
        return ComplexModelMapper.MapToComplexModel(_model);
    }

    [Benchmark]
    public ComplexModel MemoryPack()
    {
        var bytes = MemoryPackSerializer.Serialize(_model);
        return MemoryPackSerializer.Deserialize<ComplexModel>(bytes)!;
    }

    [Benchmark]
    public ComplexModel MessagePack()
    {
        var bytes = MessagePackSerializer.Serialize(_model);
        return MessagePackSerializer.Deserialize<ComplexModel>(bytes);
    }

    [Benchmark]
    public ComplexModel SystemTextJson_Reflection()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_model, _systemTextJsonOptions);
        return System.Text.Json.JsonSerializer.Deserialize<ComplexModel>(json, _systemTextJsonOptions)!;
    }

    [Benchmark]
    public ComplexModel SystemTextJson_SourceGen()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_model, BenchmarkJsonContext.Default.ComplexModel);
        return System.Text.Json.JsonSerializer.Deserialize(json, BenchmarkJsonContext.Default.ComplexModel)!;
    }

    [Benchmark]
    public ComplexModel NewtonsoftJson()
    {
        var json = JsonConvert.SerializeObject(_model);
        return JsonConvert.DeserializeObject<ComplexModel>(json)!;
    }
}

/// <summary>
/// Mapperly mapper for ComplexModel (compile-time code generation, no reflection).
/// </summary>
[Mapper]
public static partial class ComplexModelMapper
{
    public static partial ComplexModel MapToComplexModel(ComplexModel source);
}

/// <summary>
/// System.Text.Json source generation context for ComplexModel.
/// This provides compile-time JSON serialization without reflection.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false)]
[JsonSerializable(typeof(ComplexModel))]
[JsonSerializable(typeof(UserInfo))]
[JsonSerializable(typeof(ContactInfo))]
[JsonSerializable(typeof(DataItem))]
[JsonSerializable(typeof(SubItem))]
[JsonSerializable(typeof(Settings))]
[JsonSerializable(typeof(AdvancedSettings))]
[JsonSerializable(typeof(UserRole))]
public partial class BenchmarkJsonContext : JsonSerializerContext
{
}
