using System.Diagnostics;
using fastJSON;
using NUnit.Framework;

namespace UnitTests.Regressions.reftype
{
    [TestFixture]
    public class Bug002
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
            _json.Parameters.UseFastGuid = false;
            _json.Parameters.UseOptimizedDatasetSchema = false;
            _json.Parameters.UseUTCDateTime = false;
            _json.Parameters.UsingGlobalTypes = false;
        }

        [Test]
        public void Test()
        {
            var payload = new OptContainer { Beginn = new Zeit(42) };
            var json = _json.ToJSON(payload);
            Trace.WriteLine(json);
            var loaded = _json.ToObject<OptContainer>(json);
            Assert.IsTrue(loaded.Beginn.HasValue);
            Assert.AreEqual(42, loaded.Beginn.Value.Minuten);
        }
    }

    public class OptContainer
    {
        public Optional<Zeit>  Beginn { get; set; }
    }
}