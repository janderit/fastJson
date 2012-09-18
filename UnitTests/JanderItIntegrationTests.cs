using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using fastJSON;

namespace UnitTests
{
    [TestFixture]
    class JanderItIntegrationTests
    {
        
        [Test, ExpectedException]
        public void SingleThreadRoundTripNoExtensionsFailsWithoutType()
        {
            var o = new TestClassA { Id = Guid.NewGuid() , B=new TestClassB{SomeInt = 16}};
            var json = fastJSON.JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            Assert.That(!json.Contains("$type"));
            var p = fastJSON.JSON.Instance.ToObject(json) as TestClassA;
        }

        [Test]
        public void EmptyClassSmokeTest()
        {
            var o = new EmptyClass();
            var json = fastJSON.JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            Assert.AreEqual("{}", json);
        }

        [Test]
        public void SimpleClassSmokeTest1()
        {
            var o = new SimpleClass1{Hello="World"};
            var json = fastJSON.JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            Assert.AreEqual(@"{""Hello"":""World""}", json);
        }

        [Test]
        public void SimpleClassSmokeTest2()
        {
            var o = new SimpleClass2 { Hello = "World" ,FourtyTwo= 42};
            var json = fastJSON.JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            Assert.AreEqual(@"{""Hello"":""World"",""FourtyTwo"":42}", json);
        }

        [Test]
        public void SingleThreadRoundTripNoExtensions()
        {
            var o = new TestClassA { Id = Guid.NewGuid(), B = new TestClassB { SomeInt = 16 } };
            var json = fastJSON.JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            Assert.That(!json.Contains("$type"));
            var p = fastJSON.JSON.Instance.ToObject(json, typeof(TestClassA)) as TestClassA;
            Assert.IsNotNull(p);
            Assert.AreEqual(o.Id, p.Id);
            Assert.AreEqual(o.B.SomeInt, p.B.SomeInt);
        }


        [Test]
        public void SingleThreadRoundTripExtensionsNoGlobals()
        {
            var o = new TestClassA { Id = Guid.NewGuid(), B = new TestClassB { SomeInt = 16 } };
            var json = fastJSON.JSON.Instance.ToJSON(o, new JSONParameters{EnableAnonymousTypes=false, IgnoreCaseOnDeserialize=false, SerializeNullValues=false, ShowReadOnlyProperties=false, UseExtensions=true, UseFastGuid=false, UseOptimizedDatasetSchema=false, UseUTCDateTime=false, UsingGlobalTypes = false});
            Trace.WriteLine(json);
            Assert.That(json.Contains("$type"));
            Assert.That(!json.Contains("$types"));
            var p = fastJSON.JSON.Instance.ToObject(json) as TestClassA;
            Assert.IsNotNull(p);
            Assert.AreEqual(o.Id, p.Id);
            Assert.AreEqual(o.B.SomeInt, p.B.SomeInt);
        }

        [Test]
        public void SingleThreadRoundTripExtensionsWithGlobals()
        {
            var o = new TestClassA { Id = Guid.NewGuid(), B = new TestClassB { SomeInt = 16 } };
            var json = JSON.Instance.ToJSON(o, new JSONParameters { EnableAnonymousTypes = false, IgnoreCaseOnDeserialize = false, SerializeNullValues = false, ShowReadOnlyProperties = false, UseExtensions = true, UseFastGuid = false, UseOptimizedDatasetSchema = false, UseUTCDateTime = false, UsingGlobalTypes = true });
            Trace.WriteLine(json);
            Assert.That(json.Contains("$type"));
            Assert.That(json.Contains("$types"));
            var p = fastJSON.JSON.Instance.ToObject(json) as TestClassA;
            Assert.IsNotNull(p);
            Assert.AreEqual(o.Id, p.Id);
            Assert.AreEqual(o.B.SomeInt, p.B.SomeInt);
        }

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
        public void CanDeserializePolymorphicRootObjectsOnConcreteBaseClass()
        {
            var o = new Concrete4();
            var json = JSON.Instance.ToJSON(o, new JSONParameters { EnableAnonymousTypes = false, IgnoreCaseOnDeserialize = false, SerializeNullValues = false, ShowReadOnlyProperties = false, UseExtensions = true, UseFastGuid = false, UseOptimizedDatasetSchema = false, UseUTCDateTime = false, UsingGlobalTypes = true });
            Trace.WriteLine(json);
            Assert.That(json.Contains("$type"));
            Assert.That(json.Contains("$types"));
            var p = JSON.Instance.ToObject<ConcreteClass>(json);
            Assert.IsNotNull(p);
            Assert.AreEqual("4", p.ToString());
        }

        [Test]
        public void CanDeserializePolymorphicRootObjectsOnAbstractBaseClass()
        {
            SetFastJsonParameters();

            var o = new Concrete2();
            var json = JSON.Instance.ToJSON(o, new JSONParameters { EnableAnonymousTypes = false, IgnoreCaseOnDeserialize = false, SerializeNullValues = false, ShowReadOnlyProperties = false, UseExtensions = true, UseFastGuid = false, UseOptimizedDatasetSchema = false, UseUTCDateTime = false, UsingGlobalTypes = true });
            Trace.WriteLine(json);
            Assert.That(json.Contains("$type"));
            Assert.That(json.Contains("$types"));
            var p = JSON.Instance.ToObject<AbstractClass>(json);
            Assert.IsNotNull(p);
            Assert.AreEqual("2", p.ToString());
        }

        [Test]
        public void CanDeserializePolymorphicRootObjectsOnInterface()
        {
            var o = new Concrete1();
            var json = JSON.Instance.ToJSON(o, new JSONParameters { EnableAnonymousTypes = false, IgnoreCaseOnDeserialize = false, SerializeNullValues = false, ShowReadOnlyProperties = false, UseExtensions = true, UseFastGuid = false, UseOptimizedDatasetSchema = false, UseUTCDateTime = false, UsingGlobalTypes = true });
            Trace.WriteLine(json);
            Assert.That(json.Contains("$type"));
            Assert.That(json.Contains("$types"));
            var p = JSON.Instance.ToObject<MyInterface>(json);
            Assert.IsNotNull(p);
            Assert.AreEqual("1", p.ToString());
        }

        [Test]
        public void CanDeserializePolymorphicConstituentObjectsOnConcreteBaseClass()
        {
            var o = new ContainerConcrete {Payload = new Concrete3()};
            var json = JSON.Instance.ToJSON(o, new JSONParameters { EnableAnonymousTypes = false, IgnoreCaseOnDeserialize = false, SerializeNullValues = false, ShowReadOnlyProperties = false, UseExtensions = true, UseFastGuid = false, UseOptimizedDatasetSchema = false, UseUTCDateTime = false, UsingGlobalTypes = true });
            Trace.WriteLine(json);
            Assert.That(json.Contains("$type"));
            Assert.That(json.Contains("$types"));
            var p = JSON.Instance.ToObject<ContainerConcrete>(json);
            Assert.IsNotNull(p);
            Assert.IsNotNull(p.Payload);
            Assert.AreEqual("3", p.Payload.ToString());
        }

        [Test]
        public void CanDeserializePolymorphicConstituentObjectsOnAbstractBaseClass()
        {
            var o = new ContainerAbstract { Payload = new Concrete2() };
            var json = JSON.Instance.ToJSON(o, new JSONParameters { EnableAnonymousTypes = false, IgnoreCaseOnDeserialize = false, SerializeNullValues = false, ShowReadOnlyProperties = false, UseExtensions = true, UseFastGuid = false, UseOptimizedDatasetSchema = false, UseUTCDateTime = false, UsingGlobalTypes = true });
            Trace.WriteLine(json);
            Assert.That(json.Contains("$type"));
            Assert.That(json.Contains("$types"));
            var p = JSON.Instance.ToObject<ContainerAbstract>(json);
            Assert.IsNotNull(p);
            Assert.IsNotNull(p.Payload);
            Assert.AreEqual("2", p.Payload.ToString());
        }

        [Test]
        public void CanDeserializePolymorphicConstituentObjectsOnInterface()
        {
            var o = new ContainerInterface { Payload = new Concrete1() };
            var json = JSON.Instance.ToJSON(o, new JSONParameters { EnableAnonymousTypes = false, IgnoreCaseOnDeserialize = false, SerializeNullValues = false, ShowReadOnlyProperties = false, UseExtensions = true, UseFastGuid = false, UseOptimizedDatasetSchema = false, UseUTCDateTime = false, UsingGlobalTypes = true });
            Trace.WriteLine(json);
            Assert.That(json.Contains("$type"));
            Assert.That(json.Contains("$types"));
            var p = JSON.Instance.ToObject<ContainerInterface>(json);
            Assert.IsNotNull(p);
            Assert.IsNotNull(p.Payload);
            Assert.AreEqual("1", p.Payload.ToString());
        }

        [Test]
        public void CorrectlyDeserializesDecimalValues()
        {
            var o = new DecimalContainer { Value = 16.06M };
            var json = JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            var p = JSON.Instance.ToObject<DecimalContainer>(json);
            Assert.IsNotNull(p);
            Assert.AreEqual(16.06M, p.Value);
        }

        [Test]
        public void CorrectlyDeserializesDateTimeValues()
        {
            var d = DateTime.Now;
            var o = new DateTimeContainer { Value = d };
            var json = JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            var p = JSON.Instance.ToObject<DateTimeContainer>(json);
            Assert.IsNotNull(p);
            Assert.AreEqual(DateTimeKind.Unspecified, p.Value.Kind); // expect unspecified time
            Assert.AreEqual(d.ToString("yyyy-MM-dd HH:mm:ss"), p.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        [Test]
        public void CorrectlyDeserializesDateTimeValuesWithEmptyTime()
        {
            var now = DateTime.Now;
            var d = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            var o = new DateTimeContainer { Value = d };
            var json = JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            var p = JSON.Instance.ToObject<DateTimeContainer>(json);
            Assert.IsNotNull(p);
            Assert.AreEqual(DateTimeKind.Unspecified, p.Value.Kind); // expect unspecified time
            Assert.AreEqual(d.ToString("yyyy-MM-dd HH:mm:ss"), p.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        [Test]
        public void CorrectlyDeserializesUtcDateTimeValues()
        {
            var d = DateTime.UtcNow;
            var o = new DateTimeContainer { Value = d };
            var json = JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            var p = JSON.Instance.ToObject<DateTimeContainer>(json);
            Assert.IsNotNull(p);
            Assert.AreEqual(DateTimeKind.Utc, p.Value.Kind); // expect UTC
            Assert.AreEqual(d.ToString("yyyy-MM-dd HH:mm:ss"), p.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        [Test]
        public void CorrectlyDeserializesUtcDateTimeValuesViaUtc()
        {
            fastJSON.JSON.Instance.Parameters.UseUTCDateTime = true;
            var d = DateTime.UtcNow;
            var o = new DateTimeContainer { Value = d };
            var json = JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            var p = JSON.Instance.ToObject<DateTimeContainer>(json);
            Assert.IsNotNull(p);
            Assert.AreEqual(DateTimeKind.Utc, p.Value.Kind); // Expect UTC
            Assert.AreEqual(d.ToString("yyyy-MM-dd HH:mm:ss"), p.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        [Test]
        public void CorrectlyDeserializesDateTimeValuesViaUtc()
        {
            fastJSON.JSON.Instance.Parameters.UseUTCDateTime = true;
            var d = DateTime.Now;
            var o = new DateTimeContainer { Value = d };
            var json = JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            var p = JSON.Instance.ToObject<DateTimeContainer>(json);
            Assert.IsNotNull(p);
            Assert.AreEqual(DateTimeKind.Utc, p.Value.Kind); // Expect UTC
            Assert.AreEqual(d.ToString("yyyy-MM-dd HH:mm:ss"), p.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
        }

        [Test]
        public void CorrectlyDeserializesDateTimeValuesWithEmptyTimeViaUtc()
        {
            fastJSON.JSON.Instance.Parameters.UseUTCDateTime = true;
            var now = DateTime.Now;
            var d = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            var o = new DateTimeContainer { Value = d };
            var json = JSON.Instance.ToJSON(o);
            Trace.WriteLine(json);
            var p = JSON.Instance.ToObject<DateTimeContainer>(json);
            Assert.IsNotNull(p);
            Assert.AreEqual(DateTimeKind.Utc, p.Value.Kind); // Expect UTC
            Assert.AreEqual(d.ToString("yyyy-MM-dd HH:mm:ss"), p.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
        }

        

    }

    public class DecimalContainer
    {
        public Decimal Value { get; set; }
    }

    public class DateTimeContainer
    {
        public DateTime Value { get; set; }
    }

    public class TestClassA
    {
        public Guid Id { get; set; }
        public TestClassB B { get; set; }
    }


    public class TestClassB
    {
        public Int32 SomeInt { get; set; }
    }


    public interface MyInterface{}

    public abstract class AbstractClass{}
    public class ConcreteClass{}

    public class Concrete1 : AbstractClass, MyInterface
    {
        public override string ToString()
        {
            return "1";
        }
    }

    public class Concrete2 : AbstractClass, MyInterface
    {
        public override string ToString()
        {
            return "2";
        }
    }

    public class Concrete3 : ConcreteClass
    {
        public override string ToString()
        {
            return "3";
        }
    }

    public class Concrete4 : ConcreteClass
    {
        public override string ToString()
        {
            return "4";
        }
    }

    public class ContainerInterface { public MyInterface Payload { get; set; } }
    public class ContainerConcrete { public ConcreteClass Payload { get; set; } }
    public class ContainerAbstract { public AbstractClass Payload { get; set; } }

    public class EmptyClass { }
    public class SimpleClass1 { public string Hello { get; set; } }
    public class SimpleClass2 { public string Hello { get; set; } public int FourtyTwo { get; set; }}

}
