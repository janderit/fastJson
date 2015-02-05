using System;
using System.Collections.Generic;
using System.Text;

namespace fastJSON
{
    public struct SerializerData
    {
        internal SerializerData(Action payload)
        {
            Payload = payload;
        }

        internal readonly Action Payload;
    }

    public interface Serializer
    {
        void WriteValue(string value);
        void WriteField(string key, SerializerData field);
        void WriteList(IEnumerable<SerializerData> items);
        void EmptyObject();
        void Defer(SerializerData deferral);
    }

    public delegate void CustomSerialization(object t, Serializer output, Func<object, SerializerData> defer_back);
    public delegate object CustomDeserialization_value(string json, Func<Type, object, object> deserializefields);
    public delegate object CustomDeserialization_list(List<object> json, Func<Type, object, object> deserializefields);
    public delegate object CustomDeserialization_dict(Dictionary<string, object> dict, Func<Type, object, object> deserializefields);

    public delegate void CustomSerialization<in T>(T t, Serializer output, Func<object, SerializerData> defer_back);
    public delegate T CustomDeserialization_value<out T>(string json, Func<Type, object, object> deserializefields);
    public delegate T CustomDeserialization_list<out T>(List<object> json, Func<Type, object, object> deserializefields);
    public delegate T CustomDeserialization_dict<out T>(Dictionary<string, object> dict, Func<Type, object, object> deserializefields);
}