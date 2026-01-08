```csharp
private static List<Dictionary<string, SampleClass>> List_Dictionary_String_SampleClass__CloneInternal(this List<Dictionary<string, SampleClass>> datas)
{
    var temp = new List<Dictionary<string, SampleClass>();
#if NET8_0_OR_GREATER
    CollectionsMarshal.SetCount(temp, datas.Length);
#endif
    foreach(var data in datas){
        temp.Add(Dictionary_String_SampleClass_CloneInternal(data));
    }
}

private static Dictionary<string, SampleClass> Dictionary_String_SampleClass_CloneInternal(...)
{
    var temp = new Dictionary<string, SampleClass>();
    foreach(var data in datas){
        temp[data.Key] = SampleClass_CloneInternal(data.Value);
    }
}

private static SampleClass SampleClass_CloneInternal(...){ ... }
```