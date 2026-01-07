using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MemoryPack;
using MessagePack;
using Newtonsoft.Json;

namespace IDeepCloneable.Benchmark;

/// <summary>
/// Benchmarks comparing different deep cloning approaches for complex object models.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
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
    public ComplexModel IDeepCloneable_DeepClone()
    {
        return _model.DeepClone();
    }

    [Benchmark]
    public ComplexModel FastCloner_DeepClone()
    {
        return FastCloner.FastCloner.DeepClone(_model)!;
    }

    [Benchmark]
    public ComplexModel AutoMapper_Map()
    {
        return _autoMapper.Map<ComplexModel>(_model);
    }

    [Benchmark]
    public ComplexModel ManualDeepCopy()
    {
        return ManualMapper.DeepCopy(_model);
    }

    [Benchmark]
    public ComplexModel MemoryPack_SerializeDeserialize()
    {
        var bytes = MemoryPackSerializer.Serialize(_model);
        return MemoryPackSerializer.Deserialize<ComplexModel>(bytes)!;
    }

    [Benchmark]
    public ComplexModel MessagePack_SerializeDeserialize()
    {
        var bytes = MessagePackSerializer.Serialize(_model);
        return MessagePackSerializer.Deserialize<ComplexModel>(bytes);
    }

    [Benchmark]
    public ComplexModel SystemTextJson_SerializeDeserialize()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_model, _systemTextJsonOptions);
        return System.Text.Json.JsonSerializer.Deserialize<ComplexModel>(json, _systemTextJsonOptions)!;
    }

    [Benchmark]
    public ComplexModel SystemTextJson_SourceGen_SerializeDeserialize()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_model, BenchmarkJsonContext.Default.ComplexModel);
        return System.Text.Json.JsonSerializer.Deserialize(json, BenchmarkJsonContext.Default.ComplexModel)!;
    }

    [Benchmark]
    public ComplexModel NewtonsoftJson_SerializeDeserialize()
    {
        var json = JsonConvert.SerializeObject(_model);
        return JsonConvert.DeserializeObject<ComplexModel>(json)!;
    }
}

/// <summary>
/// Manual deep copy implementation for comparison.
/// This represents a handwritten deep clone method without any library support.
/// </summary>
public static class ManualMapper
{
    public static ComplexModel DeepCopy(ComplexModel source)
    {
        if (source == null) return null!;

        return new ComplexModel
        {
            Id = source.Id,
            Name = source.Name,
            Version = source.Version,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            Owner = source.Owner != null ? DeepCopy(source.Owner) : null,
            Contributors = source.Contributors != null
                ? source.Contributors.ConvertAll(DeepCopy)
                : null,
            Metadata = source.Metadata != null
                ? new Dictionary<string, string>(source.Metadata)
                : null,
            Items = source.Items != null
                ? source.Items.ConvertAll(DeepCopy)
                : null,
            Settings = source.Settings != null ? DeepCopy(source.Settings) : null,
        };
    }

    private static UserInfo DeepCopy(UserInfo source)
    {
        return new UserInfo
        {
            UserId = source.UserId,
            UserName = source.UserName,
            Email = source.Email,
            Role = source.Role,
            Contact = source.Contact != null ? DeepCopy(source.Contact) : null,
        };
    }

    private static ContactInfo DeepCopy(ContactInfo source)
    {
        return new ContactInfo
        {
            Phone = source.Phone,
            Address = source.Address,
            City = source.City,
            Country = source.Country,
        };
    }

    private static DataItem DeepCopy(DataItem source)
    {
        return new DataItem
        {
            ItemId = source.ItemId,
            Title = source.Title,
            Description = source.Description,
            Value = source.Value,
            Tags = source.Tags != null ? new List<string>(source.Tags) : null,
            SubItems = source.SubItems != null
                ? source.SubItems.ConvertAll(DeepCopy)
                : null,
            Properties = source.Properties != null
                ? new Dictionary<string, string>(source.Properties)
                : null,
        };
    }

    private static SubItem DeepCopy(SubItem source)
    {
        return new SubItem
        {
            SubId = source.SubId,
            Label = source.Label,
            Quantity = source.Quantity,
            Price = source.Price,
        };
    }

    private static Settings DeepCopy(Settings source)
    {
        return new Settings
        {
            IsEnabled = source.IsEnabled,
            MaxItems = source.MaxItems,
            Timeout = source.Timeout,
            AllowedDomains = source.AllowedDomains != null
                ? new List<string>(source.AllowedDomains)
                : null,
            Limits = source.Limits != null
                ? new Dictionary<string, int>(source.Limits)
                : null,
            Advanced = source.Advanced != null ? DeepCopy(source.Advanced) : null,
        };
    }

    private static AdvancedSettings DeepCopy(AdvancedSettings source)
    {
        return new AdvancedSettings
        {
            CacheSize = source.CacheSize,
            UseCompression = source.UseCompression,
            CompressionLevel = source.CompressionLevel,
            Features = source.Features != null
                ? new List<string>(source.Features)
                : null,
        };
    }
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
