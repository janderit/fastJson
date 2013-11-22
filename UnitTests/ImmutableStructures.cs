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
        public void ValueObject()
        {
            var payload = new MyValueObject1("Hello", 42, Guid.NewGuid());
            var json = fastJSON.JSON.Instance.ToJSON(payload);
            Trace.WriteLine(json);
            var loaded = fastJSON.JSON.Instance.ToObject<MyValueObject1>(json);
            Assert.AreEqual("Hello", loaded.A);
            Assert.AreEqual(42, loaded.B);
            Assert.AreEqual(payload.C, loaded.C);
        }

        [Test]
        public void ValueObjectWithinClass()
        {
            var payload = new Container {VO=new MyValueObject1("Hello", 42, Guid.NewGuid())};
            var json = fastJSON.JSON.Instance.ToJSON(payload);
            Trace.WriteLine(json);
            var loaded = fastJSON.JSON.Instance.ToObject<Container>(json);
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