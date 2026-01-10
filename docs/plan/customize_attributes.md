
Add custom attributes to control the behavior of DeepClone.

* `[CloneIgnore]`
  * Fields or properties marked with this attribute will be ignored during DeepClone operations (they will remain at their default value).
* `[ShallowClone]`
  * Fields or properties marked with this attribute will be shallow-copied during DeepClone operations.
  * That is, for reference types, no new instance will be created; the reference to the original object will be copied.
* These attribute definitions should be created under the global namespace within `IDeepCloneable`.
* The names of these attributes can be changed via options in `CloneableGeneratorCore`.

