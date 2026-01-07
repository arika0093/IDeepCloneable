using System;
using System.Collections.Generic;

namespace IDeepCloneable.Benchmark;

/// <summary>
/// Helper class to create sample complex models for benchmarking.
/// </summary>
public static class TestDataGenerator
{
    public static ComplexModel CreateSampleModel()
    {
        return new ComplexModel
        {
            Id = "model-12345",
            Name = "Sample Complex Model",
            Version = 1,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            Owner = new UserInfo
            {
                UserId = "user-001",
                UserName = "john.doe",
                Email = "john.doe@example.com",
                Role = UserRole.Owner,
                Contact = new ContactInfo
                {
                    Phone = "+1-555-1234",
                    Address = "123 Main St",
                    City = "San Francisco",
                    Country = "USA",
                },
            },
            Contributors = new List<UserInfo>
            {
                new UserInfo
                {
                    UserId = "user-002",
                    UserName = "jane.smith",
                    Email = "jane.smith@example.com",
                    Role = UserRole.Admin,
                    Contact = new ContactInfo
                    {
                        Phone = "+1-555-5678",
                        Address = "456 Oak Ave",
                        City = "New York",
                        Country = "USA",
                    },
                },
                new UserInfo
                {
                    UserId = "user-003",
                    UserName = "bob.wilson",
                    Email = "bob.wilson@example.com",
                    Role = UserRole.User,
                    Contact = new ContactInfo
                    {
                        Phone = "+1-555-9012",
                        Address = "789 Pine Rd",
                        City = "Seattle",
                        Country = "USA",
                    },
                },
            },
            Metadata = new Dictionary<string, string>
            {
                { "category", "test" },
                { "priority", "high" },
                { "status", "active" },
            },
            Items = new List<DataItem>
            {
                new DataItem
                {
                    ItemId = "item-001",
                    Title = "First Item",
                    Description = "This is the first item with a detailed description",
                    Value = 99.99,
                    Tags = new List<string> { "tag1", "tag2", "tag3" },
                    SubItems = new List<SubItem>
                    {
                        new SubItem
                        {
                            SubId = "sub-001",
                            Label = "Sub Item 1",
                            Quantity = 10,
                            Price = 19.99m,
                        },
                        new SubItem
                        {
                            SubId = "sub-002",
                            Label = "Sub Item 2",
                            Quantity = 5,
                            Price = 29.99m,
                        },
                    },
                    Properties = new Dictionary<string, string>
                    {
                        { "color", "blue" },
                        { "size", "large" },
                        { "weight", "1.5" },
                    },
                },
                new DataItem
                {
                    ItemId = "item-002",
                    Title = "Second Item",
                    Description = "This is the second item with another detailed description",
                    Value = 149.99,
                    Tags = new List<string> { "tag4", "tag5" },
                    SubItems = new List<SubItem>
                    {
                        new SubItem
                        {
                            SubId = "sub-003",
                            Label = "Sub Item 3",
                            Quantity = 15,
                            Price = 39.99m,
                        },
                    },
                    Properties = new Dictionary<string, string>
                    {
                        { "color", "red" },
                        { "size", "medium" },
                    },
                },
            },
            Settings = new Settings
            {
                IsEnabled = true,
                MaxItems = 100,
                Timeout = TimeSpan.FromSeconds(30),
                AllowedDomains = new List<string>
                {
                    "example.com",
                    "test.com",
                    "sample.org",
                },
                Limits = new Dictionary<string, int>
                {
                    { "maxUsers", 1000 },
                    { "maxStorage", 10240 },
                },
                Advanced = new AdvancedSettings
                {
                    CacheSize = 256,
                    UseCompression = true,
                    CompressionLevel = "high",
                    Features = new List<string>
                    {
                        "feature1",
                        "feature2",
                        "feature3",
                    },
                },
            },
        };
    }
}
