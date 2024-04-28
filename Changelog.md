# 1.0.0 

* [new] Initial release

# 1.0.1

* [new] Added support for single-dimensional arrays
* [new] Added support for maps (e.g. Dictionary<,>)
* [new] Added support for binary serialization (only for byte[] properties)

# 1.0.2

* [new] Added support for varying-type properties
* Refactored implementation of varying-type item serialization (XmlArrayItem, XmlMapKeyItem, XmlMapValueItem, XmlVariant) to reduce duplicated code and simplify implementation

# 1.0.3

* [new] Added attribute `XmlIncludeDerived` to simplify working with class hierarchies