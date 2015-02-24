using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using fastJSON;

namespace UnitTests
{
    [TestFixture]
    public class ImmutableStructures
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
        public void ValueObject()
        {
            var payload = new MyValueObject1("Hello", 42, Guid.NewGuid());
            var json = _json.ToJSON(payload);
            Trace.WriteLine(json);
            var loaded = _json.ToObject<MyValueObject1>(json);
            Assert.AreEqual("Hello", loaded.A);
            Assert.AreEqual(42, loaded.B);
            Assert.AreEqual(payload.C, loaded.C);
        }

        [Test]
        public void ValueObjectWithinClass()
        {
            var payload = new Container {VO=new MyValueObject1("Hello", 42, Guid.NewGuid())};
            var json = _json.ToJSON(payload);
            Trace.WriteLine(json);
            var loaded = _json.ToObject<Container>(json);
            Assert.AreEqual("Hello", loaded.VO.A);
            Assert.AreEqual(42, loaded.VO.B);
            Assert.AreEqual(payload.VO.C, loaded.VO.C);
        }

    }

    public class Container
    {
        public MyValueObject1 VO { get; set; }
    }

    public struct MyValueObject1
    {
        public readonly string A;
        public readonly int B;
        public readonly Guid C;

        public MyValueObject1(string a, int b, Guid c)
        {
            A = a;
            B = b;
            C = c;
        }
    }


    

}