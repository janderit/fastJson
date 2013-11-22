using System.Diagnostics;
using NUnit.Framework;

namespace UnitTests.Regressions.reftype
{
    [TestFixture]
    public class Bug002
    {

        [SetUp]
        public void SetFastJsonParameters()
        {
            fastJSON.JSON.Instance.Parameters.EnableAnonymousTypes = false;
            fastJSON.JSON.Instance.Parameters.IgnoreCaseOnDeserialize = false;
            fastJSON.JSON.Instance.Parameters.SerializeNullValues = false;
            fastJSON.JSON.Instance.Parameters.ShowReadOnlyProperties = false;
            fastJSON.JSON.Instance.Parameters.UseExtensions = true;
            fastJSON.JSON.Instance.Parameters.UseFastGuid = false;
            fastJSON.JSON.Instance.Parameters.UseOptimizedDatasetSchema = false;
            fastJSON.JSON.Instance.Parameters.UseUTCDateTime = false;
            fastJSON.JSON.Instance.Parameters.UsingGlobalTypes = false;
        }

        [Test]
        public void Test()
        {
            var payload = new OptContainer { Beginn = new Zeit(42) };
            var json = fastJSON.JSON.Instance.ToJSON(payload);
            Trace.WriteLine(json);
            var loaded = fastJSON.JSON.Instance.ToObject<OptContainer>(json);
            Assert.IsTrue(loaded.Beginn.HasValue);
            Assert.AreEqual(42, loaded.Beginn.Value.Minuten);
        }
    }

    public class OptContainer
    {
        public Optional<Zeit>  Beginn { get; set; }
    }
}