using System;
using System.Diagnostics;
using fastJSON;
using NUnit.Framework;

namespace UnitTests.Regressions.reftype
{
    [TestFixture]
    public class Bug003
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
            _json.Parameters.UsingGlobalTypes = false;
        }

        [Test]
        public void Test()
        {
            var id = Guid.NewGuid();
            var payload = new OptContainer1 { Id = id };
            var json = _json.ToJSON(payload);
            Trace.WriteLine(json);
            var loaded = _json.ToObject<OptContainer1>(json);
            Assert.IsTrue(loaded.Id.HasValue);
            Assert.AreEqual(id, loaded.Id.Value);
        }
    }

    public class OptContainer1
    {
        public Optional<Guid>  Id { get; set; }
    }
}