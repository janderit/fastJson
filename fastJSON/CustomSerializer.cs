using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace fastJSON
{
    public interface CustomSerializer<T>
    {
        IEnumerable<SerializedField> ToJson(T t, Func<object, string> serializefields);
        T ToObject(IEnumerable<DeserializedField> json, Func<Type, object, object> deserializefields);
    }

    public interface CustomSerialization
    {
    }

    public interface CustomDeserialization
    {
        
    }

    public struct FieldSetDeserializer : CustomDeserialization
    {
        public readonly Func<IEnumerable<DeserializedField>, Func<Type, object, object>, object> Deserializer;

        public FieldSetDeserializer(Func<IEnumerable<DeserializedField>, Func<Type, object, object>, object> deserializer) : this()
        {
            Deserializer = deserializer;
        }
    }

    public struct TextualDeserializer : CustomDeserialization
    {
        public readonly Func<string, Func<Type, object, object>, object> Deserializer;

        public TextualDeserializer(Func<string, Func<Type, object, object>, object> deserializer)
            : this()
        {
            Deserializer = deserializer;
        }
    }

    public sealed class Textual : CustomSerialization
    {
        public readonly String Value;

        public Textual(string value)
        {
            Value = value;
        }
    }

    public sealed class FieldSet : CustomSerialization
    {
        public readonly List<SerializedField> Fields;

        public FieldSet(List<SerializedField> fields)
        {
            Fields = fields;
        }
    }

    public struct SerializedField
    {
        public string Name;
        public string Json;

        public SerializedField(string name, string json)
        {
            Name = name;
            Json = json;
        }
    }

    public struct DeserializedField
    {
        public string Name;
        public object Parsed;

        public DeserializedField(string name, object parsed)
        {
            Name = name;
            Parsed = parsed;
        }
    }

}