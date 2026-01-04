using IDeepCloneable;

internal partial class SampleClass : IDeepCloneable<SampleClass>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

internal partial class SampleClass2 : IDeepCloneable<SampleClass2>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
