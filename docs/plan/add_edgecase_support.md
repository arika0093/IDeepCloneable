Support for numerous edge cases.

* Circular References
  * Ensure DeepClone works correctly even when circular references exist in the object graph.
  * The following approach is likely required:
    * During type analysis, identify locations where circular references may occur.
      * For example, references like T1->T2->T3->T1.
    * If a circular reference is possible, generate dedicated clone logic for it.
      * Prepare a static cache such as Dictionary<ObjectHashCode, TargetType>.
      * Before creating a clone, obtain the hash code with original.GetHashCode() and check if it exists in the cache.
      * If present, return the cached instance.
      * If not, create a new instance, register it in the cache, and then clone its fields.
* Array Optimization
  * If the element type is primitive or immutable, use original.AsSpan().ToArray() for fast cloning.
* Addition of IEnumerableTypeInfo
  * Add a standard array pattern (IEnumerable) to TypeInfo.
  * Acts as a fallback when existing TypeInfo does not match.
* Required Properties
  * Collect IsRequired information when gathering property info.
  * When generating clones, if IsRequired, initialize using an object initializer: new TargetType { Prop = value, ... }.
  * If not required, set properties after instance creation as usual.
* Constructor
  * Prefer creation without a constructor if possible (current behavior).
  * If a constructor exists that takes the same type as an argument, such as TargetType(TargetType), use it.
  * Otherwise, creation is uncertain.
    * For now, generate code that throws an exception.
    * In the future, this will be an analyzer error.
* 