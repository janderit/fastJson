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

        private readonly JSON _json = JSON.CreateInstance();

        [SetUp]
        public void SetFastJsonParameters()
        {
            _json.Parameters.EnableAnonymousTypes = false;
            _json.Parameters.IgnoreCaseOnDeserialize = false;
            _json.Parameters.SerializeNullValues = false;
            _json.Parameters.ShowReadOnlyProperties = false;
            _json.Parameters.UseExtensions = false;
            _json.Parameters.UseFastGuid = false;
            _json.Parameters.UseOptimizedDatasetSchema = false;
            _json.Parameters.UseUTCDateTime = false;
            _json.Parameters.UsingGlobalTypes = false;
        }

        [Test]
        public void CustomSerializerSmokeTest1()
        {
            var o = new SimpleClass3 { Hello = "World" };
            _json.RegisterCustomSerializer<SimpleClass3>((t, s, d) => s.WriteField("Hello", d("Earth")));
            var json = _json.ToJSON(o);
            Trace.WriteLine(json);
            Assert.AreEqual(@"{""Hello"":""Earth""}", json);
        }

        [Test]
        public void CustomDeserializerSmokeTest1()
        {
            _json.RegisterCustomDeserializer_d((j,t, d) => new SimpleClass3 {Hello = "Moon"});
            var o = _json.ToObject<SimpleClass3>("{\"Hello\":\"Phobos\"}");
            Assert.AreEqual("Moon", o.Hello);
        }

        [Test]
        public void CustomDeserializerSmokeTest2()
        {
            _json.RegisterCustomDeserializer_l((j, t, d) => new SimpleClass3 { Hello = "Moon" });
            var o = _json.ToObject<SimpleClass3>("[\"Phobos\"]");
            Assert.AreEqual("Moon", o.Hello);
        }

        [Test]
        public void CustomDeserializerSmokeTest3()
        {
            _json.RegisterCustomDeserializer_v((j, t, d) => new SimpleClass3 { Hello = "Moon" });
            var o = _json.ToObject<SimpleClass3>("\"Phobos\"");
            Assert.AreEqual("Moon", o.Hello);
        }

        [Test]
        public void CustomSerializerOption_empty()
        {
            var o = new Optional<int>();
            _json.RegisterCustomSerializer(typeof(Optional<>), (v, s, d) =>
            {
                var boxed = (v as OptionalBox).BOX();
                if (boxed.HasValue) s.Defer(d(boxed.Value));
                else s.EmptyObject();
            });
            var json = _json.ToJSON(o);
            Trace.WriteLine(json);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void CustomSerializerOption_valuetype()
        {
            var o = new Optional<int>(42);

            _json.RegisterCustomSerializer(typeof(Optional<>), (v, s, d) =>
            {
                var boxed = (v as OptionalBox).BOX();
                if (boxed.HasValue) s.Defer(d(boxed.Value));
                else s.EmptyObject();
            });

            var json = _json.ToJSON(o);
            Trace.WriteLine(json);
            Assert.AreEqual("42", json);
        }

        [Test]
        public void CustomSerializerOption_valuetype_deserialize()
        {
            _json.RegisterCustomDeserializer_d(typeof(Optional<>), (dico, t, defer) => Activator.CreateInstance(t));
            _json.RegisterCustomDeserializer_v(typeof(Optional<>), (v, t, defer) => Activator.CreateInstance(t, new[] { Convert.ChangeType(defer(t.GetGenericArguments().Single(), v), t.GetGenericArguments().Single()) }));

            var o = _json.ToObject<Optional<int>>("42");
            Trace.WriteLine(o);
            Assert.IsTrue(o.HasValue);
            Assert.AreEqual(42, o.Value);
        }

        [Test]
        public void CustomSerializerOption_none_deserialize()
        {
            _json.RegisterCustomDeserializer_d(typeof(Optional<>), (dico, t, defer) => Activator.CreateInstance(t));
            _json.RegisterCustomDeserializer_v(typeof(Optional<>), (v, t, defer) => Activator.CreateInstance(t, new[] { Convert.ChangeType(defer(t.GetGenericArguments().Single(), v), t.GetGenericArguments().Single()) }));

            var o = _json.ToObject<Optional<int>>("{}");
            Trace.WriteLine(o);
            Assert.IsFalse(o.HasValue);
        }

        [Test]
        public void FastGuid()
        {
            _json.Parameters.UseFastGuid = true;
            _json.RegisterCustomDeserializer_d(typeof(Optional<>), (dico, t, defer) => Activator.CreateInstance(t));
            _json.RegisterCustomDeserializer_v(typeof(Optional<>), (v, t, defer) => Activator.CreateInstance(t, new[] { Convert.ChangeType(defer(t.GetGenericArguments().Single(), v), t.GetGenericArguments().Single()) }));
            var id = Guid.NewGuid();
            var payload = new OptContainer1 { Id = id };
            var json = _json.ToJSON(payload);
            Trace.WriteLine(json);
            var loaded = _json.ToObject<OptContainer1>(json);
            Assert.IsTrue(loaded.Id.HasValue);
            Assert.AreEqual(id, loaded.Id.Value);
        }

        /*[Test]
        public void CustomDeserializerWithTypeInfo()
        {
            _json.Parameters.UseExtensions = true;
            var o = new SimpleClass3 { Hello = "World" };
            _json.RegisterCustomSerializer<SimpleClass3>(new SimpleClass3Serializer());
            var json = _json.ToJSON(o);
            var o2 = _json.ToObject(json) as SimpleClass3;
            Assert.IsNotNull(o2);
            Assert.AreEqual("Moon", o2.Hello);
        }*/

        /*[Test]
        public void CustomDeserializerWithTypeInfoAndGlobalTypes()
        {
            _json.Parameters.UseExtensions = true;
            _json.Parameters.UsingGlobalTypes = true;
            var o = new SimpleClass3 { Hello = "World" };
            _json.RegisterCustomSerializer<SimpleClass3>(new SimpleClass3Serializer());
            var json = _json.ToJSON(o);
            var o2 = _json.ToObject(json) as SimpleClass3;
            Assert.IsNotNull(o2);
            Assert.AreEqual("Moon", o2.Hello);
        }*/

        /*[Test]
        public void CustomSerializerWithInner()
        {
            var o = new SimpleClass4 { Hello = "World", Inner = new InnerClass{Datum="Heute", Id=Guid.NewGuid()}};
            _json.RegisterCustomSerializer<SimpleClass4>(new SimpleClass4Serializer());
            var json = _json.ToJSON(o);
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
            _json.RegisterCustomSerializer<SimpleClass3>(new SimpleClass3Serializer());
            var json = _json.ToJSON(o);
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
            _json.RegisterCustomSerializer<SimpleClass4>(new SimpleClass4Serializer());
            _json.RegisterCustomSerializer<InnerClass>((i,d)=>i.Datum+"|"+i.Id, (line, defer) =>
            {
                var parts = line.Split('|');
                return new InnerClass {Datum = parts[0], Id = Guid.Parse(parts[1])};
            });
            var json = _json.ToJSON(o);
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