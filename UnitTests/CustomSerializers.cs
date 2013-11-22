using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using fastJSON;

namespace UnitTests
{
    [TestFixture]
    public class CustomSerializers
    {

        [SetUp]
        public void SetFastJsonParameters()
        {
            fastJSON.JSON.Instance.Parameters.EnableAnonymousTypes = false;
            fastJSON.JSON.Instance.Parameters.IgnoreCaseOnDeserialize = false;
            fastJSON.JSON.Instance.Parameters.SerializeNullValues = false;
            fastJSON.JSON.Instance.Parameters.ShowReadOnlyProperties = false;
            fastJSON.JSON.Instance.Parameters.UseExtensions = false;
            fastJSON.JSON.Instance.Parameters.UseFastGuid = false;
            fastJSON.JSON.Instance.Parameters.UseOptimizedDatasetSchema = false;
            fastJSON.JSON.Instance.Parameters.UseUTCDateTime = false;
            fastJSON.JSON.Instance.Parameters.UsingGlobalTypes = false;
        }

        [Test]
        public void CustomSerializerSmokeTest1()
        {
            var o = new SimpleClass3 { Hello = "World" };
            fastJSON.JSON.Instance.RegisterCustomSerializer<SimpleClass3>(new SimpleClass3Serializer());
            var json = fastJSON.JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            Assert.AreEqual(@"{""Hello"":""Earth""}", json);
        }

        [Test]
        public void CustomDeserializerSmokeTest1()
        {
            fastJSON.JSON.Instance.RegisterCustomSerializer<SimpleClass3>(new SimpleClass3Serializer());
            var o = fastJSON.JSON.Instance.ToObject<SimpleClass3>("{\"Hello\":{\"Id\":\"Phobos\"}}");
            //var o = fastJSON.JSON.Instance.ToObject<SimpleClass3>("{\"Hello\":\"Phobos\"}");
            Assert.AreEqual("Moon", o.Hello);
        }

        [Test]
        public void CustomDeserializerWithTypeInfo()
        {
            fastJSON.JSON.Instance.Parameters.UseExtensions = true;
            var o = new SimpleClass3 { Hello = "World" };
            fastJSON.JSON.Instance.RegisterCustomSerializer<SimpleClass3>(new SimpleClass3Serializer());
            var json = fastJSON.JSON.Instance.ToJSON(o);
            var o2 = fastJSON.JSON.Instance.ToObject(json) as SimpleClass3;
            Assert.IsNotNull(o2);
            Assert.AreEqual("Moon", o2.Hello);
        }

        [Test]
        public void CustomDeserializerWithTypeInfoAndGlobalTypes()
        {
            fastJSON.JSON.Instance.Parameters.UseExtensions = true;
            fastJSON.JSON.Instance.Parameters.UsingGlobalTypes = true;
            var o = new SimpleClass3 { Hello = "World" };
            fastJSON.JSON.Instance.RegisterCustomSerializer<SimpleClass3>(new SimpleClass3Serializer());
            var json = fastJSON.JSON.Instance.ToJSON(o);
            var o2 = fastJSON.JSON.Instance.ToObject(json) as SimpleClass3;
            Assert.IsNotNull(o2);
            Assert.AreEqual("Moon", o2.Hello);
        }

        [Test]
        public void CustomSerializerWithInner()
        {
            var o = new SimpleClass4 { Hello = "World", Inner = new InnerClass{Datum="Heute", Id=Guid.NewGuid()}};
            fastJSON.JSON.Instance.RegisterCustomSerializer<SimpleClass4>(new SimpleClass4Serializer());
            var json = fastJSON.JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            var o2 = JSON.Instance.ToObject<SimpleClass4>(json);
            Assert.IsNotNull(o2);
            Assert.AreEqual("World!!!", o2.Hello);
            Assert.IsNotNull(o2.Inner);
            Assert.AreEqual("Heute", o2.Inner.Datum);
            Assert.AreEqual(o.Inner.Id, o2.Inner.Id);
        }

        [Test]
        public void CustomSerializerWithinOuter()
        {
            var o = new SimpleClass5 {Inner = new SimpleClass3() {Hello = "Test"}};
            fastJSON.JSON.Instance.RegisterCustomSerializer<SimpleClass3>(new SimpleClass3Serializer());
            var json = fastJSON.JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            var o2 = JSON.Instance.ToObject<SimpleClass5>(json);
            Assert.IsNotNull(o2);
            Assert.IsNotNull(o2.Inner);
            Assert.AreEqual("Moon", o2.Inner.Hello);
        }

    }

    public class SimpleClass5
    {
        public SimpleClass3 Inner { get; set; }
    }

    public class SimpleClass4Serializer : CustomSerializer<SimpleClass4>
    {
        public IEnumerable<SerializedField> ToJson(SimpleClass4 t, Func<object, string> serializefields)
        {
            yield return new SerializedField("Hello", serializefields(t.Hello+"!!"));
            yield return new SerializedField("Inner", serializefields(t.Inner));
        }

        public SimpleClass4 ToObject(IEnumerable<DeserializedField> json, Func<Type, object, object> deserializefields)
        {
            var flds = json.ToDictionary(_=>_.Name, _=>_.Parsed);

            var result = new SimpleClass4();

            result.Hello = (string)flds["Hello"]+"!";
            result.Inner = (InnerClass)deserializefields(typeof(InnerClass), flds["Inner"]);
            
            return result;
        }
    }


    public class SimpleClass3Serializer : CustomSerializer<SimpleClass3>
    {
        public IEnumerable<SerializedField> ToJson(SimpleClass3 input, Func<object, string> serializefields)
        {
            yield return new SerializedField("Hello","\"Earth\"");
        }

        public SimpleClass3 ToObject(IEnumerable<DeserializedField> json, Func<Type, object, object> deserializefields)
        {
            return new SimpleClass3 {Hello = "Moon"};
        }
    }

    public class SimpleClass3
    {
        public string Hello { get; set; }
    }


    public class SimpleClass4
    {
        public string Hello { get; set; }
        public InnerClass Inner { get; set; }
    }

    public class InnerClass
    {
        public string Datum { get; set; }
        public Guid Id { get; set; }
    }
}