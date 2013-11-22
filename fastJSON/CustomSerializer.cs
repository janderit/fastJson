using System;
using System.Collections.Generic;

namespace fastJSON
{
    public interface CustomSerializer<T>
    {
        IEnumerable<SerializedField> ToJson(T t, Func<object, string> serializefields);
        T ToObject(IEnumerable<DeserializedField> json, Func<Type, object, object> deserializefields);
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