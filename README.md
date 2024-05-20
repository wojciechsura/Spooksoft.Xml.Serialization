# Spooksoft.Xml.Serialization

Since there is no good solution for (de)serializing immutable models to/from XML, I wrote my own library, which performs this task.

Usage is very similar to System.Xml.Serialization - even names of the attributes are similar.

**Since 1.0.5**: `[Xml...]` attribute names were replaced with `[SpkXml...]` to avoid clash with `System.Xml.Serialization` (e.g. Visual Studio is more eager to suggest System.Xml.Serialization over other namespaces). Original attribute names are kept as aliases for now, but **will be removed** in the future releases.

```csharp
[SpkXmlRoot("MyModel")]
public class MyModel 
{
	[SpkXmlElement("MyElement")]
	public int IntProperty { get; set; }

	[SpkXmlAttribute("MyAttribute")]
	public DateTime DateTimeProperty { get; set; }
}

(...)

var serializer = new XmlSerializer();
var ms = new MemoryStream();
var model = new MyModel();

serializer.Serialize(model, ms);
```

# Immutable models

To (de)serialize immutable models, the following requirements must be met:

* Model must have a single constructor
* The constructor must have parameters, which match by type (exactly) and by name (case-insensitive) read-only properties you want to serialize

Example:

```csharp
[SpkXmlRoot("MyImmutableModel")]
public class MyImmutableModel 
{
	public MyImmutableModel(string stringProp, int intProp) 
	{
		StringProp = stringProp;
		IntProp = intProp;
	}

	public string StringProp { get; }
	public int IntProp { get; }
}
```

# Varying-type properties

If a property is of reference type and can contain an instance of derived type, my serializer will handle that, but you must explicitly specify all possible variants.

```csharp
public class MyClass
{
    [SpkXmlVariant("Base", typeof(BaseType))]
    [SpkXmlVariant("Derived1", typeof(DerivedType1))]
    [SpkXmlVariant("Derived2", typeof(DerivedType2))]
    public BaseType Prop { get; set; }
}
```

This can be also achieved differently, by using `SpkXmlIncludeDerived` attribute on base property type:

```csharp
[SpkXmlIncludeDerived("Base", typeof(BaseType))]
[SpkXmlIncludeDerived("Derived1", typeof(DerivedType1))]
[SpkXmlIncludeDerived("Derived2", typeof(DerivedType2))]
public class BaseType
{

}

public class MyClass
{
    // No attributes needed here anymore
    public BaseType Prop { get; set; }
}
```

The same will work as well for collections and maps (see below).

If you use both `SpkXmlIncludeDerived` and `SpkXmlVariant` (or `SpkXmlArrayItem` in case of collections or `SpkXmlMapKey`/`SpkXmlMapValue` in case of maps), then all mapped types will be merged. 

Note that neither **names** nor **types** in the custom mapping lists must not duplicate:

```csharp
[SpkXmlIncludeDerived("Derived1", typeof(DerivedType1))]
public class BaseType
{

}

public class MyModel
{
    // WRONG! Name Derived1 has already been used
    [SpkXmlVariant("Derived1", typeof(SomeType))]
    // WRONG! Type DerivedType1 has already been mapped
    [SpkXmlVariant("SomeName", typeof(DerivedType1))]
    public BaseType Prop { get; set; }
}
```

# Collections

Collections must be marked with `SpkXmlArray` attribute. If you want to support various types in the collection, add `SpkXmlArrayItem` attributes.

So far the following collections are supported:

* `List<T>`
* `IReadOnlyList<T>` (filled during deserialization with `List<T>` instances)
* `T[]` (for now, only single-dimensional arrays are supported)
* `ImmutableArray<T>`

```csharp
public class MyModel 
{
    [SpkXmlArray]
    [SpkXmlArrayItem("ItemType1", typeof(ItemType1))]
    [SpkXmlArrayItem("ItemType2", typeof(ItemType2))]
    public List<BaseItemType> Collection { get; set; }

    [SpkXmlArray]
    public int[] MyArray { get; set; }
}
```

# Maps

Map properties (e.g. `Dictionary<,>`) must be marked with `SpkXmlMapAttribute` attribute. If you want to support various types for either keys or values, add one or more `SpkXmlMapKeyAttribute` or `SpkXmlMapValueAttribute` attributes to the property.

So far the following maps are supported:

* `Dictionary<TKey,TValue>`

```csharp
public class MyModel
{
    [SpkXmlMap]
    [SpkXmlMapKey("Base", typeof(BaseKey))]
    [SpkXmlMapKey("Derived", typeof(DerivedKey))]
    [SpkXmlMapValue("Base", typeof(BaseValue))]
    [SpkXmlMapValue("Derived", typeof(DerivedValue))]
    public Dictionary<BaseKey, BaseValue> Dictionary { get; set; }
}
```

# Binary

To serialize binary data, use `SpkXmlBinaryAttribute` attribute. Data will be serialized in the Base64 format.

So far the following types of properties can be treated as binary:

* `byte[]`

```csharp
public class MyModel
{
    [SpkXmlBinary]
    public byte[] SomeData { get; set; }
}
```

# Custom serialization

If you want to serialize a class in custom way, implement the `IXmlSerializable` interface (one provided in the library, not the one from `System.Xml.Serialization`!)

Example:

```csharp
public class CustomSerializedModel : IXmlSerializable
{
    private int field1;
    private int field2;

    public CustomSerializedModel()
    {

    }

    public CustomSerializedModel(int field1, int field2)
    {
        this.field1 = field1;
        this.field2 = field2;
    }

    public void Read(XmlElement element)
    {
        string? values = element.GetAttribute("Values");

        if (values != null)
        {
            var splitted = values.Split('|');
            if (splitted.Length == 2)
            {
                int value = 0;
                if (int.TryParse(splitted[0], out value))
                    field1 = value;
                if (int.TryParse(splitted[1], out value))
                    field2 = value;
            }
        }
    }

    public void Write(XmlElement element)
    {
        element.SetAttribute("Values", $"{IntProperty1}|{IntProperty2}");
    }

    public int IntProperty1 => field1;
    public int IntProperty2 => field2;
}
```

# Known limitations

* `null` value in a string property serialized to an attribute will be deserialized as an empty string. If you want to keep the null value, serialize it to an element instead (`[SpkXmlElement(...)]`).
* You need to separately define `SpkXmlArray` and `SpkXmlElement` attributes (if you want to specify custom name for array element).
* You can not store collections inside an attribute. The same applies to maps and binary data.

# Development

Pull requests (e.g. bugfixes, implementation of more collection types) are mostly welcome.