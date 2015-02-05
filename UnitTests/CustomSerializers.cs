using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NUnit.Framework;
using fastJSON;
using UnitTests.Regressions.reftype;

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
            fastJSON.JSON.Instance.RegisterCustomSerializer<SimpleClass3>((t, s, d) => s.WriteField("Hello", d("Earth")));
            var json = fastJSON.JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            Assert.AreEqual(@"{""Hello"":""Earth""}", json);
        }

        [Test]
        public void CustomDeserializerSmokeTest1()
        {
            fastJSON.JSON.Instance.RegisterCustomDeserializer_d((j, d) => new SimpleClass3 {Hello = "Moon"});
            var o = fastJSON.JSON.Instance.ToObject<SimpleClass3>("{\"Hello\":\"Phobos\"}");
            Assert.AreEqual("Moon", o.Hello);
        }

        [Test]
        public void CustomDeserializerSmokeTest2()
        {
            fastJSON.JSON.Instance.RegisterCustomDeserializer_l((j, d) => new SimpleClass3 { Hello = "Moon" });
            var o = fastJSON.JSON.Instance.ToObject<SimpleClass3>("[\"Phobos\"]");
            Assert.AreEqual("Moon", o.Hello);
        }

        [Test]
        public void CustomDeserializerSmokeTest3()
        {
            fastJSON.JSON.Instance.RegisterCustomDeserializer_v((j, d) => new SimpleClass3 { Hello = "Moon" });
            var o = fastJSON.JSON.Instance.ToObject<SimpleClass3>("\"Phobos\"");
            Assert.AreEqual("Moon", o.Hello);
        }

        [Test]
        public void CustomSerializerOption_empty()
        {
            var o = new Optional<int>();
            fastJSON.JSON.Instance.RegisterCustomSerializer(typeof(Optional<>), (v, s, d) =>
            {
                var boxed = (v as OptionalBox).BOX();
                if (boxed.HasValue) s.Defer(d(boxed.Value));
                else s.EmptyObject();
            });
            var json = fastJSON.JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void CustomSerializerOption_valuetype()
        {
            var o = new Optional<int>(42);

            fastJSON.JSON.Instance.RegisterCustomSerializer(typeof(Optional<>), (v, s, d) =>
            {
                var boxed = (v as OptionalBox).BOX();
                if (boxed.HasValue) s.Defer(d(boxed.Value));
                else s.EmptyObject();
            });

            var json = fastJSON.JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            Assert.AreEqual("42", json);
        }

        /*[Test]
        public void CustomDeserializerWithTypeInfo()
        {
            fastJSON.JSON.Instance.Parameters.UseExtensions = true;
            var o = new SimpleClass3 { Hello = "World" };
            fastJSON.JSON.Instance.RegisterCustomSerializer<SimpleClass3>(new SimpleClass3Serializer());
            var json = fastJSON.JSON.Instance.ToJSON(o);
            var o2 = fastJSON.JSON.Instance.ToObject(json) as SimpleClass3;
            Assert.IsNotNull(o2);
            Assert.AreEqual("Moon", o2.Hello);
        }*/

        /*[Test]
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
        }*/

        /*[Test]
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
        }*/
        
        /*[Test]
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
        }*/

        /*[Test]
        public void CustomSerializerWithTextRepresentation()
        {
            var o = new SimpleClass4 { Hello = "World", Inner = new InnerClass { Datum = "Heute", Id = Guid.NewGuid() } };
            fastJSON.JSON.Instance.RegisterCustomSerializer<SimpleClass4>(new SimpleClass4Serializer());
            fastJSON.JSON.Instance.RegisterCustomSerializer<InnerClass>((i,d)=>i.Datum+"|"+i.Id, (line, defer) =>
            {
                var parts = line.Split('|');
                return new InnerClass {Datum = parts[0], Id = Guid.Parse(parts[1])};
            });
            var json = fastJSON.JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            var o2 = JSON.Instance.ToObject<SimpleClass4>(json);
            Assert.IsNotNull(o2);
            Assert.AreEqual("World!!!", o2.Hello);
            Assert.IsNotNull(o2.Inner);
            Assert.AreEqual("Heute", o2.Inner.Datum);
            Assert.AreEqual(o.Inner.Id, o2.Inner.Id);
        }*/

    }

    public class SimpleClass5
    {
        public SimpleClass3 Inner { get; set; }
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