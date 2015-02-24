using System;
using System.Diagnostics;
using System.Linq;
using fastJSON;
using NUnit.Framework;

namespace UnitTests.Regressions.reftype
{
    [TestFixture]
    public class Bug004
    {

        private readonly JSON _json = JSON.CreateInstance();

        [SetUp]
        public void SetFastJsonParameters()
        {
            _json.ClearCustom();
            _json.Parameters.EnableAnonymousTypes = false;
            _json.Parameters.IgnoreCaseOnDeserialize = false;
            _json.Parameters.SerializeNullValues = false;
            _json.Parameters.ShowReadOnlyProperties = false;
            _json.Parameters.UseExtensions = true;
            _json.Parameters.UseFastGuid = true;
            _json.Parameters.UseOptimizedDatasetSchema = false;
            _json.Parameters.UseUTCDateTime = false;
            _json.Parameters.UsingGlobalTypes = true;
        }

        [Test]
        public void Test()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();
            var id4 = Guid.NewGuid();
            var id5 = Guid.NewGuid();
            var payload = new TestObjekt4_0(id1, new TestObjekt4_1(id2), new[] {new TestObjekt4_1(id3), new TestObjekt4_1(id4), new TestObjekt4_1(id5)}.ToReadonlyList());
            var json = _json.ToJSON(payload);
            Trace.WriteLine(json);
            var loaded = _json.ToObject<TestObjekt4_0>(json);
            Assert.AreEqual(id1, loaded.Id);
            Assert.AreEqual(id2, loaded.Einzelobjekt.Id2);
            Assert.AreEqual(id3, loaded.Listenobjekt.First().Id2);
            Assert.AreEqual(id4, loaded.Listenobjekt.Skip(1).First().Id2);
            Assert.AreEqual(id5, loaded.Listenobjekt.Skip(2).First().Id2);
        }
    }

    public struct TestObjekt4_0
    {
        public TestObjekt4_0(Guid id, TestObjekt4_1 einzelobjekt, ReadonlyList<TestObjekt4_1> listenobjekt)
        {
            Id = id;
            Listenobjekt = listenobjekt;
            Einzelobjekt = einzelobjekt;
        }

        public readonly Guid Id;
        public readonly TestObjekt4_1 Einzelobjekt;
        public readonly ReadonlyList<TestObjekt4_1> Listenobjekt;
    }

    public struct TestObjekt4_1
    {
        public TestObjekt4_1(Guid id2)
        {
            Id2 = id2;
        }

        public readonly Guid Id2;
    }

}