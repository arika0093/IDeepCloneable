using System;
using System.Collections.Generic;
using IDeepCloneable;
using MemoryPack;
using MessagePack;

namespace IDeepCloneable.Benchmark;

/// <summary>
/// Complex JSON-like object model for benchmarking deep cloning operations.
/// This model represents a realistic scenario with nested objects, collections, and various data types.
/// </summary>
[DeepCloneable]
[MemoryPackable]
[MessagePackObject]
public partial class ComplexModel
{
    [Key(0)]
    public string Id { get; set; } = string.Empty;
    
    [Key(1)]
    public string Name { get; set; } = string.Empty;
    
    [Key(2)]
    public int Version { get; set; }
    
    [Key(3)]
    public DateTime CreatedAt { get; set; }
    
    [Key(4)]
    public DateTime? UpdatedAt { get; set; }
    
    [Key(5)]
    public UserInfo? Owner { get; set; }
    
    [Key(6)]
    public List<UserInfo>? Contributors { get; set; }
    
    [Key(7)]
    public Dictionary<string, string>? Metadata { get; set; }
    
    [Key(8)]
    public List<DataItem>? Items { get; set; }
    
    [Key(9)]
    public Settings? Settings { get; set; }
}

[MemoryPackable]
[MessagePackObject]
public partial class UserInfo
{
    [Key(0)]
    public string UserId { get; set; } = string.Empty;
    
    [Key(1)]
    public string UserName { get; set; } = string.Empty;
    
    [Key(2)]
    public string Email { get; set; } = string.Empty;
    
    [Key(3)]
    public UserRole Role { get; set; }
    
    [Key(4)]
    public ContactInfo? Contact { get; set; }
}

[MemoryPackable]
[MessagePackObject]
public partial class ContactInfo
{
    [Key(0)]
    public string Phone { get; set; } = string.Empty;
    
    [Key(1)]
    public string Address { get; set; } = string.Empty;
    
    [Key(2)]
    public string City { get; set; } = string.Empty;
    
    [Key(3)]
    public string Country { get; set; } = string.Empty;
}

[MemoryPackable]
[MessagePackObject]
public partial class DataItem
{
    [Key(0)]
    public string ItemId { get; set; } = string.Empty;
    
    [Key(1)]
    public string Title { get; set; } = string.Empty;
    
    [Key(2)]
    public string Description { get; set; } = string.Empty;
    
    [Key(3)]
    public double Value { get; set; }
    
    [Key(4)]
    public List<string>? Tags { get; set; }
    
    [Key(5)]
    public List<SubItem>? SubItems { get; set; }
    
    [Key(6)]
    public Dictionary<string, string>? Properties { get; set; }
}

[MemoryPackable]
[MessagePackObject]
public partial class SubItem
{
    [Key(0)]
    public string SubId { get; set; } = string.Empty;
    
    [Key(1)]
    public string Label { get; set; } = string.Empty;
    
    [Key(2)]
    public int Quantity { get; set; }
    
    [Key(3)]
    public decimal Price { get; set; }
}

[MemoryPackable]
[MessagePackObject]
public partial class Settings
{
    [Key(0)]
    public bool IsEnabled { get; set; }
    
    [Key(1)]
    public int MaxItems { get; set; }
    
    [Key(2)]
    public TimeSpan Timeout { get; set; }
    
    [Key(3)]
    public List<string>? AllowedDomains { get; set; }
    
    [Key(4)]
    public Dictionary<string, int>? Limits { get; set; }
    
    [Key(5)]
    public AdvancedSettings? Advanced { get; set; }
}

[MemoryPackable]
[MessagePackObject]
public partial class AdvancedSettings
{
    [Key(0)]
    public int CacheSize { get; set; }
    
    [Key(1)]
    public bool UseCompression { get; set; }
    
    [Key(2)]
    public string CompressionLevel { get; set; } = string.Empty;
    
    [Key(3)]
    public List<string>? Features { get; set; }
}

public enum UserRole
{
    Guest = 0,
    User = 1,
    Admin = 2,
    Owner = 3,
}
