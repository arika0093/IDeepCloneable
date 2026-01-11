Ensure correct operation for the types defined in `tests/GenericsTypeWorkTests`.
Specifically, DeepClone should work properly for:

* Collection properties such as `IEnumerable<int>`, `IEnumerable<string>`, etc.
* Custom collections such as `CustomEnumerable<string>`
* Generic type properties such as `MyGenericsClass<T>`
* Composite patterns such as `IEnumerable<MyGenericsClass<T>>`

DeepClone must function correctly for all of the above cases.
