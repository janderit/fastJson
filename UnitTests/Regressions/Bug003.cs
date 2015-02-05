using System;
using System.Diagnostics;
using NUnit.Framework;

namespace UnitTests.Regressions.reftype
{
    [TestFixture]
    public class Bug003
    {

        [SetUp]
        public void SetFastJsonParameters()
        {
            fastJSON.JSON.Instance.ClearCustom();
            fastJSON.JSON.Instance.Parameters.EnableAnonymousTypes = false;
            fastJSON.JSON.Instance.Parameters.IgnoreCaseOnDeserialize = false;
            fastJSON.JSON.Instance.Parameters.SerializeNullValues = false;
            fastJSON.JSON.Instance.Parameters.ShowReadOnlyProperties = false;
            fastJSON.JSON.Instance.Parameters.UseExtensions = true;
            fastJSON.JSON.Instance.Parameters.UseFastGuid = true;
            fastJSON.JSON.Instance.Parameters.UseOptimizedDatasetSchema = false;
            fastJSON.JSON.Instance.Parameters.UseUTCDateTime = false;
            fastJSON.JSON.Instance.Parameters.UsingGlobalTypes = false;
        }

        [Test]
        public void Test()
        {
            var id = Guid.NewGuid();
            var payload = new OptContainer1 { Id = id };
            var json = fastJSON.JSON.Instance.ToJSON(payload);
            Trace.WriteLine(json);
            var loaded = fastJSON.JSON.Instance.ToObject<OptContainer1>(json);
            Assert.IsTrue(loaded.Id.HasValue);
            Assert.AreEqual(id, loaded.Id.Value);
        }
    }

    public class OptContainer1
    {
        public Optional<Guid>  Id { get; set; }
    }
}