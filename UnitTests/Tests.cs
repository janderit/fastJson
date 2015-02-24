using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Data;
using System.Collections;
using System.Threading;
using fastJSON;

namespace UnitTests
{
    public class Tests
    {
        private readonly JSON _json = JSON.CreateInstance();

        #region [  helpers  ]

        private const int count = 1000;
        private const int tcount = 5;
        static readonly DataSet ds = new DataSet();
        private const bool exotic = false;
        private const bool dsser = false;

        public enum Gender
        {
            Male,
            Female
        }

        public class colclass
        {
            public colclass()
            {
                items = new List<baseclass>();
                date = DateTime.Now;
                multilineString = @"
            AJKLjaskljLA
       ahjksjkAHJKS سلام فارسی
       AJKHSKJhaksjhAHSJKa
       AJKSHajkhsjkHKSJKash
       ASJKhasjkKASJKahsjk
            ";
                isNew = true;
                booleanValue = true;
                ordinaryDouble = 0.001;
                gender = Gender.Female;
                intarray = new int[] { 1, 2, 3, 4, 5 };
            }
            public bool booleanValue { get; set; }
            public DateTime date { get; set; }
            public string multilineString { get; set; }
            public List<baseclass> items { get; set; }
            public decimal ordinaryDecimal { get; set; }
            public double ordinaryDouble { get; set; }
            public bool isNew { get; set; }
            public string laststring { get; set; }
            public Gender gender { get; set; }

            public DataSet dataset { get; set; }
            public Dictionary<string, baseclass> stringDictionary { get; set; }
            public Dictionary<baseclass, baseclass> objectDictionary { get; set; }
            public Dictionary<int, baseclass> intDictionary { get; set; }
            public Guid? nullableGuid { get; set; }
            public decimal? nullableDecimal { get; set; }
            public double? nullableDouble { get; set; }
            public Hashtable hash { get; set; }
            public baseclass[] arrayType { get; set; }
            public byte[] bytes { get; set; }
            public int[] intarray { get; set; }

        }

        public static colclass CreateObject()
        {
            var c = new colclass {booleanValue = true, ordinaryDecimal = 3};

            c.items.Add(new class1("1", "1", Guid.NewGuid()));
            c.items.Add(new class2("2", "2", "desc1"));
            c.items.Add(new class1("3", "3", Guid.NewGuid()));
            c.items.Add(new class2("4", "4", "desc2"));

            c.laststring = "" + DateTime.Now;

            return c;
        }

        public class baseclass
        {
            public string Name { get; set; }
            public string Code { get; set; }
        }

        public class class1 : baseclass
        {
            public class1() { }
            public class1(string name, string code, Guid g)
            {
                Name = name;
                Code = code;
                guid = g;
            }
            public Guid guid { get; set; }
        }

        public class class2 : baseclass
        {
            public class2() { }
            public class2(string name, string code, string desc)
            {
                Name = name;
                Code = code;
                description = desc;
            }
            public string description { get; set; }
        }

        public class NoExt
        {
            [System.Xml.Serialization.XmlIgnore()]
            public string Name { get; set; }
            public string Address { get; set; }
            public int Age { get; set; }
            public baseclass[] objs { get; set; }
            public Dictionary<string, class1> dic { get; set; }
            public NoExt intern { get; set; }
        }

        public class Retclass
        {
            public object ReturnEntity { get; set; }
            public string Name { get; set; }
            public string Field1;
            public int Field2;
            public object obj;
            public string ppp { get { return "sdfas df "; } }
            public DateTime date { get; set; }
            public DataTable ds { get; set; }
        }

        public struct Retstruct
        {
            public object ReturnEntity { get; set; }
            public string Name { get; set; }
            public string Field1;
            public int Field2;
            public string ppp { get { return "sdfas df "; } }
            public DateTime date { get; set; }
            public DataTable ds { get; set; }
        }

        private static long CreateLong(string s)
        {
            long num = 0;
            bool neg = false;
            foreach (char cc in s)
            {
                if (cc == '-')
                    neg = true;
                else if (cc == '+')
                    neg = false;
                else
                {
                    num *= 10;
                    num += (int)(cc - '0');
                }
            }

            return neg ? -num : num;
        }

        private static DataSet CreateDataset()
        {
            DataSet ds = new DataSet();
            for (int j = 1; j < 3; j++)
            {
                DataTable dt = new DataTable();
                dt.TableName = "Table" + j;
                dt.Columns.Add("col1", typeof(int));
                dt.Columns.Add("col2", typeof(string));
                dt.Columns.Add("col3", typeof(Guid));
                dt.Columns.Add("col4", typeof(string));
                dt.Columns.Add("col5", typeof(bool));
                dt.Columns.Add("col6", typeof(string));
                dt.Columns.Add("col7", typeof(string));
                ds.Tables.Add(dt);
                Random rrr = new Random();
                for (int i = 0; i < 100; i++)
                {
                    DataRow dr = dt.NewRow();
                    dr[0] = rrr.Next(int.MaxValue);
                    dr[1] = "" + rrr.Next(int.MaxValue);
                    dr[2] = Guid.NewGuid();
                    dr[3] = "" + rrr.Next(int.MaxValue);
                    dr[4] = true;
                    dr[5] = "" + rrr.Next(int.MaxValue);
                    dr[6] = "" + rrr.Next(int.MaxValue);

                    dt.Rows.Add(dr);
                }
            }
            return ds;
        }

        public class RetNestedclass
        {
            public Retclass Nested { get; set; }
        }

        #endregion

        [SetUp]
        public void SetupParameters()
        {
            _json.Parameters = new JSONParameters();
        }

        [Test]
        public void objectarray()
        {
            var o = new object[] { 1, "sdaffs", DateTime.Now };
            var s = _json.ToJSON(o);
            var p = _json.ToObject(s);
        }

        [Test]
        public void ClassTest()
        {
            Retclass r = new Retclass();
            r.Name = "hello";
            r.Field1 = "dsasdF";
            r.Field2 = 2312;
            r.date = DateTime.Now;
            r.ds = CreateDataset().Tables[0];

            var s = _json.ToJSON(r);
            Console.WriteLine(s);
            var o = _json.ToObject(s);

            Assert.AreEqual(2312, (o as Retclass).Field2);
        }


        [Test]
        public void StructTest()
        {
            Retstruct r = new Retstruct();
            r.Name = "hello";
            r.Field1 = "dsasdF";
            r.Field2 = 2312;
            r.date = DateTime.Now;
            r.ds = CreateDataset().Tables[0];

            var s = _json.ToJSON(r);
            Console.WriteLine(s);
            var o = _json.ToObject(s);

            Assert.AreEqual(2312, ((Retstruct)o).Field2);
        }

        [Test]
        public void ParseTest()
        {
            Retclass r = new Retclass();
            r.Name = "hello";
            r.Field1 = "dsasdF";
            r.Field2 = 2312;
            r.date = DateTime.Now;
            r.ds = CreateDataset().Tables[0];

            var s = _json.ToJSON(r);
            Console.WriteLine(s);
            var o = _json.Parse(s);

            Assert.IsNotNull(o);
        }

        [Test]
        public void StringListTest()
        {
            List<string> ls = new List<string>();
            ls.AddRange(new string[] { "a", "b", "c", "d" });

            var s = _json.ToJSON(ls);
            Console.WriteLine(s);
            var o = _json.ToObject(s);

            Assert.IsNotNull(o);
        }

        [Test]
        public void IntListTest()
        {
            List<int> ls = new List<int>();
            ls.AddRange(new int[] { 1, 2, 3, 4, 5, 10 });

            var s = _json.ToJSON(ls);
            Console.WriteLine(s);
            var p = _json.Parse(s);
            var o = _json.ToObject(s); // long[] {1,2,3,4,5,10}

            Assert.IsNotNull(o);
        }

        [Test]
        public void List_int()
        {
            List<int> ls = new List<int>();
            ls.AddRange(new int[] { 1, 2, 3, 4, 5, 10 });

            var s = _json.ToJSON(ls);
            Console.WriteLine(s);
            var p = _json.Parse(s);
            var o = _json.ToObject<List<int>>(s);

            Assert.IsNotNull(o);
        }

        [Test]
        public void Variables()
        {
            var s = _json.ToJSON(42);
            var o = _json.ToObject(s);
            Assert.AreEqual(o, 42);

            s = _json.ToJSON("hello");
            o = _json.ToObject(s);
            Assert.AreEqual(o, "hello");

            s = _json.ToJSON(42.42M);
            o = _json.ToObject(s);
            Assert.AreEqual(42.42M, o);
        }

        [Test]
        public void Dictionary_String_RetClass()
        {
            Dictionary<string, Retclass> r = new Dictionary<string, Retclass>();
            r.Add("11", new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
            r.Add("12", new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
            var s = _json.ToJSON(r);
            Console.WriteLine(_json.Beautify(s));
            var o = _json.ToObject<Dictionary<string, Retclass>>(s);
            Assert.AreEqual(2, o.Count);
        }

        [Test]
        public void Dictionary_String_RetClass_noextensions()
        {
            Dictionary<string, Retclass> r = new Dictionary<string, Retclass>();
            r.Add("11", new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
            r.Add("12", new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
            var s = _json.ToJSON(r, new fastJSON.JSONParameters { UseExtensions = false });
            Console.WriteLine(_json.Beautify(s));
            var o = _json.ToObject<Dictionary<string, Retclass>>(s);
            Assert.AreEqual(2, o.Count);
        }

        [Test]
        public void Dictionary_int_RetClass()
        {
            Dictionary<int, Retclass> r = new Dictionary<int, Retclass>();
            r.Add(11, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
            r.Add(12, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
            var s = _json.ToJSON(r);
            Console.WriteLine(_json.Beautify(s));
            var o = _json.ToObject<Dictionary<int, Retclass>>(s);
            Assert.AreEqual(2, o.Count);
        }

        [Test]
        public void Dictionary_int_RetClass_noextensions()
        {
            Dictionary<int, Retclass> r = new Dictionary<int, Retclass>();
            r.Add(11, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
            r.Add(12, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
            var s = _json.ToJSON(r, new fastJSON.JSONParameters { UseExtensions = false });
            Console.WriteLine(_json.Beautify(s));
            var o = _json.ToObject<Dictionary<int, Retclass>>(s);
            Assert.AreEqual(2, o.Count);
        }

        [Test]
        public void Dictionary_Retstruct_RetClass()
        {
            Dictionary<Retstruct, Retclass> r = new Dictionary<Retstruct, Retclass>();
            r.Add(new Retstruct { Field1 = "111", Field2 = 1, date = DateTime.Now }, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
            r.Add(new Retstruct { Field1 = "222", Field2 = 2, date = DateTime.Now }, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
            var s = _json.ToJSON(r);
            Console.WriteLine(_json.Beautify(s));
            var o = _json.ToObject<Dictionary<Retstruct, Retclass>>(s);
            Assert.AreEqual(2, o.Count);
        }

        [Test]
        public void Dictionary_Retstruct_RetClass_noextentions()
        {
            Dictionary<Retstruct, Retclass> r = new Dictionary<Retstruct, Retclass>();
            r.Add(new Retstruct { Field1 = "111", Field2 = 1, date = DateTime.Now }, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
            r.Add(new Retstruct { Field1 = "222", Field2 = 2, date = DateTime.Now }, new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
            var s = _json.ToJSON(r, new fastJSON.JSONParameters { UseExtensions = false });
            Console.WriteLine(_json.Beautify(s));
            var o = _json.ToObject<Dictionary<Retstruct, Retclass>>(s);
            Assert.AreEqual(2, o.Count);
        }

        [Test]
        public void List_RetClass()
        {
            List<Retclass> r = new List<Retclass>();
            r.Add(new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
            r.Add(new Retclass { Field1 = "222", Field2 = 3, date = DateTime.Now });
            var s = _json.ToJSON(r);
            Console.WriteLine(_json.Beautify(s));
            var o = _json.ToObject<List<Retclass>>(s);
            Assert.AreEqual(2, o.Count);
        }

        [Test]
        public void List_RetClass_noextensions()
        {
            List<Retclass> r = new List<Retclass>();
            r.Add(new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now });
            r.Add(new Retclass { Field1 = "222", Field2 = 3, date = DateTime.Now });
            var s = _json.ToJSON(r, new fastJSON.JSONParameters { UseExtensions = false });
            Console.WriteLine(_json.Beautify(s));
            var o = _json.ToObject<List<Retclass>>(s);
            Assert.AreEqual(2, o.Count);
        }

        [Test]
        public void Perftest()
        {
            string s = "123456";

            DateTime dt = DateTime.Now;
            int c = 1000000;

            for (int i = 0; i < c; i++)
            {
                var o = CreateLong(s);
            }

            Console.WriteLine("convertlong (ms): " + DateTime.Now.Subtract(dt).TotalMilliseconds);

            dt = DateTime.Now;

            for (int i = 0; i < c; i++)
            {
                var o = long.Parse(s);
            }

            Console.WriteLine("long.parse (ms): " + DateTime.Now.Subtract(dt).TotalMilliseconds);

            dt = DateTime.Now;

            for (int i = 0; i < c; i++)
            {
                var o = Convert.ToInt64(s);
            }

            Console.WriteLine("convert.toint64 (ms): " + DateTime.Now.Subtract(dt).TotalMilliseconds);
        }

        [Test]
        public void FillObject()
        {
            NoExt ne = new NoExt();
            ne.Name = "hello";
            ne.Address = "here";
            ne.Age = 10;
            ne.dic = new Dictionary<string, class1>();
            ne.dic.Add("hello", new class1("asda", "asdas", Guid.NewGuid()));
            ne.objs = new baseclass[] { new class1("a", "1", Guid.NewGuid()), new class2("b", "2", "desc") };

            string str = _json.ToJSON(ne, new fastJSON.JSONParameters { UseExtensions = false, UsingGlobalTypes = false });
            string strr = _json.Beautify(str);
            Console.WriteLine(strr);
            object dic = _json.Parse(str);
            object oo = _json.ToObject<NoExt>(str);

            NoExt nee = new NoExt();
            nee.intern = new NoExt { Name = "aaa" };
            _json.FillObject(nee, strr);
        }

        [Test]
        public void AnonymousTypes()
        {
            var q = new { Name = "asassa", Address = "asadasd", Age = 12 };
            string sq = _json.ToJSON(q, new fastJSON.JSONParameters { EnableAnonymousTypes = true });
            Console.WriteLine(sq);
        }

        [Test]
        public void Speed_Test_Deserialize()
        {
            Console.Write("fastjson deserialize");
            colclass c = CreateObject();
            double t = 0;
            for (int pp = 0; pp < tcount; pp++)
            {
                DateTime st = DateTime.Now;
                colclass deserializedStore;
                string jsonText = _json.ToJSON(c);
                //Console.WriteLine(" size = " + jsonText.Length);
                for (int i = 0; i < count; i++)
                {
                    deserializedStore = (colclass)_json.ToObject(jsonText);
                }
                t += DateTime.Now.Subtract(st).TotalMilliseconds;
                Console.Write("\t" + DateTime.Now.Subtract(st).TotalMilliseconds);
            }
            Console.WriteLine("\tAVG = " + t / tcount);
        }

        [Test]
        public void Speed_Test_Serialize()
        {
            Console.Write("fastjson serialize");
            //_json.Parameters.UsingGlobalTypes = false;
            colclass c = CreateObject();
            double t = 0;
            for (int pp = 0; pp < tcount; pp++)
            {
                DateTime st = DateTime.Now;
                string jsonText = null;
                for (int i = 0; i < count; i++)
                {
                    jsonText = _json.ToJSON(c);
                }
                t += DateTime.Now.Subtract(st).TotalMilliseconds;
                Console.Write("\t" + DateTime.Now.Subtract(st).TotalMilliseconds);
            }
            Console.WriteLine("\tAVG = " + t / tcount);
        }

        [Test]
        public void List_NestedRetClass()
        {
            List<RetNestedclass> r = new List<RetNestedclass>();
            r.Add(new RetNestedclass { Nested = new Retclass { Field1 = "111", Field2 = 2, date = DateTime.Now } });
            r.Add(new RetNestedclass { Nested = new Retclass { Field1 = "222", Field2 = 3, date = DateTime.Now } });
            var s = _json.ToJSON(r);
            Console.WriteLine(_json.Beautify(s));
            var o = _json.ToObject<List<RetNestedclass>>(s);
            Assert.AreEqual(2, o.Count);
        }

        [Test]
        public void NullTest()
        {
            var s = _json.ToJSON(null);
            Assert.AreEqual("null", s);
            var o = _json.ToObject(s);
            Assert.AreEqual(null, o);
        }

        [Test]
        public void DisableExtensions()
        {
            var p = new fastJSON.JSONParameters { UseExtensions = false, SerializeNullValues = false };
            var s = _json.ToJSON(new Retclass { date = DateTime.Now, Name = "aaaaaaa" }, p);
            Console.WriteLine(_json.Beautify(s));
            var o = _json.ToObject<Retclass>(s);
            Assert.AreEqual("aaaaaaa", o.Name);
        }

        [Test]
        public void ZeroArray()
        {
            var s = _json.ToJSON(new object[] { });
            var o = _json.ToObject(s);
            var a = o as object[];
            Assert.AreEqual(0, a.Length);
        }


        [Test]
        public void GermanNumbers()
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de");
            decimal d = 3.141592654M;
            var s = _json.ToJSON(d);
            var o = _json.ToObject(s);
            Assert.AreEqual(d, (decimal)o);

            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en");
        }
       
    }
}
