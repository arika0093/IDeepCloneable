namespace IDeepCloneable.Tests;

/// <summary>
/// Tests for record types with DeepClone functionality.
/// </summary>
public class RecordCloneTests
{
    [Fact]
    public void DeepClone_SimpleRecord_ClonesCorrectly()
    {
        var original = new SimpleRecord("Alice", 30);

        var clone = original.DeepClone();

        clone.ShouldNotBeNull();
        clone.ShouldNotBeSameAs(original);
        clone.Name.ShouldBe("Alice");
        clone.Age.ShouldBe(30);
    }

    [Fact]
    public void DeepClone_RecordWithProperties_ClonesCorrectly()
    {
        var original = new PersonRecord
        {
            FirstName = "John",
            LastName = "Doe",
            Age = 25,
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeNull();
        clone.ShouldNotBeSameAs(original);
        clone.FirstName.ShouldBe("John");
        clone.LastName.ShouldBe("Doe");
        clone.Age.ShouldBe(25);
    }

    [Fact]
    public void DeepClone_RecordWithNestedRecord_CreatesDeepCopy()
    {
        var original = new PersonWithAddressRecord
        {
            Name = "Jane",
            Address = new AddressRecord
            {
                Street = "Main St",
                City = "NYC",
                ZipCode = "10001",
            },
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeNull();
        clone.ShouldNotBeSameAs(original);
        clone.Address.ShouldNotBeNull();
        clone.Address.ShouldNotBeSameAs(original.Address);
        clone.Address.Street.ShouldBe("Main St");
        clone.Address.City.ShouldBe("NYC");
    }

    [Fact]
    public void DeepClone_RecordWithInitOnlyProperties_ClonesCorrectly()
    {
        var original = new RecordWithInitProps { Name = "Test", Value = 42 };

        var clone = original.DeepClone();

        clone.ShouldNotBeNull();
        clone.ShouldNotBeSameAs(original);
        clone.Name.ShouldBe("Test");
        clone.Value.ShouldBe(42);
    }

    [Fact]
    public void DeepClone_RecordStruct_ClonesCorrectly()
    {
        var original = new PointRecordStruct(10.5, 20.5);

        var clone = original.DeepClone();

        clone.X.ShouldBe(10.5);
        clone.Y.ShouldBe(20.5);
    }

    [Fact]
    public void DeepClone_RecordWithCollection_ClonesCorrectly()
    {
        var original = new RecordWithCollection
        {
            Name = "Test",
            Tags = new System.Collections.Generic.List<string> { "tag1", "tag2", "tag3" },
        };

        var clone = original.DeepClone();

        clone.ShouldNotBeNull();
        clone.ShouldNotBeSameAs(original);
        clone.Tags.ShouldNotBeNull();
        clone.Tags.ShouldNotBeSameAs(original.Tags);
        clone.Tags.ShouldBe(new[] { "tag1", "tag2", "tag3" });
    }
}

[DeepCloneable]
public partial record SimpleRecord(string Name, int Age);

[DeepCloneable]
public partial record PersonRecord
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
}

[DeepCloneable]
public partial record AddressRecord
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

[DeepCloneable]
public partial record PersonWithAddressRecord
{
    public string Name { get; set; } = string.Empty;
    public AddressRecord? Address { get; set; }
}

[DeepCloneable]
public partial record RecordWithInitProps
{
    public string Name { get; init; } = string.Empty;
    public int Value { get; init; }
}

[DeepCloneable]
public partial record struct PointRecordStruct(double X, double Y);

[DeepCloneable]
public partial record RecordWithCollection
{
    public string Name { get; init; } = string.Empty;
    public System.Collections.Generic.List<string>? Tags { get; init; }
}
