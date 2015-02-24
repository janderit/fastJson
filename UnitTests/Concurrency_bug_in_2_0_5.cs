using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using fastJSON;

namespace UnitTests
{
    [TestFixture]
    internal class Concurrency_bug_in_2_0_5
    {
        private readonly JSON _json = JSON.CreateInstance();

        private void GenerateJsonForAandB(out string jsonA, out string jsonB)
        {
            Trace.WriteLine("Begin constructing the original objects. Please ignore trace information until I'm done.");

            // set all parameters to false to produce pure JSON
            _json.Parameters = new JSONParameters {EnableAnonymousTypes = false, IgnoreCaseOnDeserialize = false, SerializeNullValues = false, ShowReadOnlyProperties = false, UseExtensions = false, UseFastGuid = false, UseOptimizedDatasetSchema = false, UseUTCDateTime = false, UsingGlobalTypes = false};

            var a = new ConcurrentClassA {PayloadA = new PayloadA()};
            var b = new ConcurrentClassB {PayloadB = new PayloadB()};

            // A is serialized with extensions and global types
            jsonA = _json.ToJSON(a, new JSONParameters { EnableAnonymousTypes = false, IgnoreCaseOnDeserialize = false, SerializeNullValues = false, ShowReadOnlyProperties = false, UseExtensions = true, UseFastGuid = false, UseOptimizedDatasetSchema = false, UseUTCDateTime = false, UsingGlobalTypes = true });
            // B is serialized using the above defaults
            jsonB = _json.ToJSON(b);

            Trace.WriteLine("Ok, I'm done constructing the objects. Below is the generated json. Trace messages that follow below are the result of deserialization and critical for understanding the timing.");
            Trace.WriteLine(jsonA);
            Trace.WriteLine(jsonB);
        }

        [Test]
        public void UsingGlobalsBug_singlethread_ok()
        {
            string jsonA;
            string jsonB;
            GenerateJsonForAandB(out jsonA, out jsonB);

            var ax = _json.ToObject(jsonA); // A has type information in JSON-extended
            var bx = _json.ToObject<ConcurrentClassB>(jsonB); // B needs external type info

            Assert.IsNotNull(ax);
            Assert.IsInstanceOf<ConcurrentClassA>(ax);
            Assert.IsNotNull(bx);
            Assert.IsInstanceOf<ConcurrentClassB>(bx);
        }

        [Test]
        public void UsingGlobalsBug_multithread_nok()
        {
            string jsonA;
            string jsonB;
            GenerateJsonForAandB(out jsonA, out jsonB);

            object ax = null;
            object bx = null;

            /* 
             * Intended timing to force CannotGetType bug in 2.0.5:
             * the outer class ConcurrentClassA is deserialized first from json with extensions+global types. It reads the global types and sets _usingglobals to true.
             * The constructor contains a sleep to force parallel deserialization of ConcurrentClassB while in A's constructor.
             * The deserialization of B sets _usingglobals back to false.
             * After B is done, A continues to deserialize its PayloadA. It finds type "2" but since _usingglobals is false now, it fails with "Cannot get type".
             */

            Exception exception = null;

            var thread = new Thread(() =>
                                        {
                                            try
                                            {
                                                Trace.WriteLine(Thread.CurrentThread.ManagedThreadId + " A begins deserialization");
                                                ax = _json.ToObject(jsonA); // A has type information in JSON-extended
                                                Trace.WriteLine(Thread.CurrentThread.ManagedThreadId + " A is done");
                                            }
                                            catch (Exception ex)
                                            {
                                                exception = ex;                                                
                                            }
                                        });

            thread.Start();

            Thread.Sleep(500); // wait to allow A to begin deserialization first

            Trace.WriteLine(Thread.CurrentThread.ManagedThreadId + " B begins deserialization");
            bx = _json.ToObject<ConcurrentClassB>(jsonB); // B needs external type info
            Trace.WriteLine(Thread.CurrentThread.ManagedThreadId + " B is done");

            Trace.WriteLine(Thread.CurrentThread.ManagedThreadId + " waiting for A to continue");
            thread.Join(); // wait for completion of A due to Sleep in A's constructor
            Trace.WriteLine(Thread.CurrentThread.ManagedThreadId + " threads joined.");

            Assert.IsNull(exception, exception==null?"":exception.Message+" "+exception.StackTrace);

            Assert.IsNotNull(ax);
            Assert.IsInstanceOf<ConcurrentClassA>(ax);
            Assert.IsNotNull(bx);
            Assert.IsInstanceOf<ConcurrentClassB>(bx);
        }
    }


    public class ConcurrentClassA
    {
        public ConcurrentClassA()
        {
            Trace.WriteLine("ctor ConcurrentClassA. I will sleep for 2 seconds.");
            Thread.Sleep(2000);
            Thread.MemoryBarrier(); // just to be sure the caches on multi-core processors do not hide the bug. For me, the bug is present without the memory barrier, too.
            Trace.WriteLine("ctor ConcurrentClassA. I am done sleeping.");
        }

        public PayloadA PayloadA { get; set; }
    }

    public class ConcurrentClassB
    {
        public ConcurrentClassB()
        {
            Trace.WriteLine("ctor ConcurrentClassB.");
        }

        public PayloadB PayloadB { get; set; }
    }

    public class PayloadA
    {
        public PayloadA()
        {
            Trace.WriteLine("ctor PayLoadA.");
        }
    }

    public class PayloadB
    {
        public PayloadB()
        {
            Trace.WriteLine("ctor PayLoadB.");
        }
    }


}
