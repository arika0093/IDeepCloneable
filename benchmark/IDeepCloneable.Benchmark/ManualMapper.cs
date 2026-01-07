using System.Collections.Generic;

namespace IDeepCloneable.Benchmark;

/// <summary>
/// Manual deep copy implementation for comparison.
/// This represents a handwritten deep clone method without any library support.
/// </summary>
public static class ManualMapper
{
    public static ComplexModel DeepCopy(ComplexModel source)
    {
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
