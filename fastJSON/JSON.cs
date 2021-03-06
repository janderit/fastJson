﻿using System;
using System.Collections;
using System.Collections.Generic;
#if !SILVERLIGHT
using System.Data;
#endif
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace fastJSON
{
    public delegate string Serialize(object data);
    public delegate object Deserialize(string data);

    public sealed class JSONParameters
    {
        /// <summary>
        /// Use the optimized fast Dataset Schema format (dfault = True)
        /// </summary>
        public bool UseOptimizedDatasetSchema = true;
        /// <summary>
        /// Use the fast GUID format (default = True)
        /// </summary>
        public bool UseFastGuid = true;
        /// <summary>
        /// Serialize null values to the output (default = True)
        /// </summary>
        public bool SerializeNullValues = true;
        /// <summary>
        /// Use the UTC date format (default = True)
        /// </summary>
        public bool UseUTCDateTime = true;
        /// <summary>
        /// Show the readonly properties of types in the output (default = False)
        /// </summary>
        public bool ShowReadOnlyProperties = false;
        /// <summary>
        /// Use the $types extension to optimise the output json (default = True)
        /// </summary>
        public bool UsingGlobalTypes = true;
        /// <summary>
        /// ** work in progress
        /// </summary>
        public bool IgnoreCaseOnDeserialize = false;
        /// <summary>
        /// Anonymous types have read only properties 
        /// </summary>
        public bool EnableAnonymousTypes = false;
        /// <summary>
        /// Enable fastJSON extensions $types, $type, $map (default = True)
        /// </summary>
        public bool UseExtensions = true;

        public void FixValues()
        {
            if (UseExtensions == false) // disable conflicting params
            {
                UsingGlobalTypes = false; 
            }
        }
    }

    public sealed class JSON
    {

        public static JSON CreateInstance()
        {
            return new JSON();
        }

        private readonly Reflection _reflection = new Reflection();

        private JSON()
        {
        }
        /// <summary>
        /// You can set these paramters globally for all calls
        /// </summary>
        private JSONParameters _parameters = new JSONParameters();

        private readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();

        /// <summary>
        /// You can set these paramters globally for all calls. Thread safe.
        /// </summary>
        public JSONParameters Parameters
        {
            get
            {
                if (Lock.TryEnterReadLock(100))
                {

                    try
                    {
                        return _parameters;
                    }
                    finally
                    {
                        Lock.ExitReadLock();
                    }
                }
                
                throw new Exception("Unable to obtain read lock for fastJSON parameters");
            }
            set
            {
                if (Lock.TryEnterWriteLock(250))
                {
                    try
                    {
                        _parameters = value;
                    }
                    finally
                    {
                        Lock.ExitWriteLock();
                    }
                }
                else throw new Exception("Unable to obtain write lock for fastJSON parameters");
            }
        }

        private JSONParameters _params;
        
        public string ToJSON(object obj)
        {
            _params = Parameters;
            _params.FixValues();
            _reflection.ShowReadOnlyProperties = _params.ShowReadOnlyProperties;
            return ToJSON(obj, Parameters);
        }

        public string ToJSON(object obj, JSONParameters param)
        {
            _params = param;
            _params.FixValues();
            _reflection.ShowReadOnlyProperties = _params.ShowReadOnlyProperties;
            Type t = null;

            if (obj == null)
                return "null";

            if (obj.GetType().IsGenericType)
                t = obj.GetType().GetGenericTypeDefinition();
            if (t == typeof(Dictionary<,>) || t == typeof(List<>))
                _params.UsingGlobalTypes = false;

            // FEATURE : enable extensions when you can deserialize anon types
            if (_params.EnableAnonymousTypes) { _params.UseExtensions = false; _params.UsingGlobalTypes = false; _reflection.ShowReadOnlyProperties = true; }
            return new JSONSerializer(_params, _serializers, _reflection).ConvertToJSON(obj);
        }

        public object Parse(string json)
        {
            _params = Parameters;
            _reflection.ShowReadOnlyProperties = _params.ShowReadOnlyProperties;
            return new JsonParser(json, _params.IgnoreCaseOnDeserialize).Decode();
        }

        public T ToObject<T>(string json)
        {
            return (T)ToObject(json, typeof(T));
        }

        public object ToObject(string json)
        {
            return ToObject(json, null);
        }

        public object ToObject(string json, Type type)
        {
            _params = Parameters;
            _params.FixValues();
            _reflection.ShowReadOnlyProperties = _params.ShowReadOnlyProperties;
            Type t = null;

            if (type != null && type.IsGenericType)
                t = type.GetGenericTypeDefinition();
            if (t == typeof(Dictionary<,>) || t == typeof(List<>))
                _params.UsingGlobalTypes = false;

            var usingglobals = _params.UsingGlobalTypes;

            object o = new JsonParser(json, Parameters.IgnoreCaseOnDeserialize).Decode();

            return DecodeParsed(type, null, o, usingglobals);
        }

        private object DecodeParsed(Type type, Dictionary<string, object> globaltypes, object o, bool usingglobals)
        {
            if (o is IDictionary)
            {
                if (type != null && _deserializers_dict.ContainsKey(type)) return _deserializers_dict[type](o as Dictionary<string, object>, type, (t, x) => ChangeType(DecodeParsed(t, globaltypes,x,usingglobals), t, globaltypes, usingglobals));

                if (type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Dictionary<,>))
                    // deserialize a dictionary
                    return RootDictionary(o, type, globaltypes, usingglobals);
                
                return ParseDictionary(o as Dictionary<string, object>, globaltypes, type, null, usingglobals);
            }

            if (o is List<object>)
            {
                if (type != null && _deserializers_list.ContainsKey(type)) return _deserializers_list[type](o as List<object>, type, (t, x) => ChangeType(DecodeParsed(t,globaltypes, x,usingglobals), t, globaltypes, usingglobals));

                if (type != null && type.GetGenericTypeDefinition() == typeof (Dictionary<,>)) // kv format
                    return RootDictionary(o, type, globaltypes, usingglobals);

                if (type != null && type.GetGenericTypeDefinition() == typeof (List<>)) // deserialize to generic list
                    return RootList(o, type, globaltypes);
                
                return (o as List<object>).ToArray();
            }

            if (o is string && type != null && _deserializers_value.ContainsKey(type)) return _deserializers_value[type](o, type, (t, x) => ChangeType(DecodeParsed(t,globaltypes, x,usingglobals), t, globaltypes, usingglobals));

            if (type != null && type.IsGenericType && _deserializers_value.ContainsKey(type.GetGenericTypeDefinition())) return _deserializers_value[type.GetGenericTypeDefinition()](o, type, (t, x) => ChangeType(DecodeParsed(t,globaltypes, x,usingglobals), t, globaltypes, usingglobals));

            return o;
        }

        public string Beautify(string input)
        {
            return Formatter.PrettyPrint(input);
        }

        public object FillObject(object input, string json)
        {
            _params = Parameters;
            _params.FixValues();
            _reflection.ShowReadOnlyProperties = _params.ShowReadOnlyProperties;
            var ht = new JsonParser(json, Parameters.IgnoreCaseOnDeserialize).Decode() as Dictionary<string, object>;
            if (ht == null) return null;
            return ParseDictionary(ht, null, input.GetType(), input, _parameters.UsingGlobalTypes);
        }

        public object DeepCopy(object obj)
        {
            return ToObject(ToJSON(obj));
        }

        public T DeepCopy<T>(T obj)
        {
            return ToObject<T>(ToJSON(obj));
        }

        #region [   JSON specific reflection   ]

        private struct myPropInfo
        {
            public bool filled;
            public Type pt;
            public Type bt;
            public Type changeType;
            public bool isDictionary;
            public bool isList;
            public bool isValueType;
            public bool isGenericType;
            public bool isArray;
            public bool isByteArray;
            public bool isGuid;
#if !SILVERLIGHT
            public bool isDataSet;
            public bool isDataTable;
            public bool isHashtable;
#endif
            public Reflection.GenericSetter setter;
            public bool isEnum;
            public bool isDateTime;
            public Type[] GenericTypes;
            public bool isInt;
            public bool isLong;
            public bool isString;
            public bool isBool;
            public bool isClass;
            public bool isInterface;
            public Reflection.GenericGetter getter;
            public bool isStringDictionary;
            public bool CanWrite;
        }

        readonly SafeDictionary<string, SafeDictionary<string, myPropInfo>> _propertycache = new SafeDictionary<string, SafeDictionary<string, myPropInfo>>();
        private SafeDictionary<string, myPropInfo> Getproperties(Type type, string typename)
        {
            SafeDictionary<string, myPropInfo> sd;
            if (_propertycache.TryGetValue(typename, out sd))
            {
                return sd;
            }
            
            sd = new SafeDictionary<string, myPropInfo>();
            var pr = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in pr)
            {
                myPropInfo d = CreateMyProp(p.PropertyType);
                d.CanWrite = p.CanWrite;
                d.setter = Reflection.CreateSetMethod(type, p);
                d.getter = Reflection.CreateGetMethod(type, p);
                sd.Add(p.Name, d);
            }
            var fi = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var f in fi)
            {
                myPropInfo d = CreateMyProp(f.FieldType);
                d.setter = Reflection.CreateSetField(type, f);
                d.getter = Reflection.CreateGetField(type, f);
                sd.Add(f.Name, d);
            }

            _propertycache.Add(typename, sd);
            return sd;
        }

        private myPropInfo CreateMyProp(Type t)
        {
            var d = new myPropInfo();
            d.filled = true;
            d.CanWrite = true;
            d.pt = t;
            d.isGenericType = t.IsGenericType;

            d.isDictionary = t.IsGenericType && typeof (IDictionary).IsAssignableFrom(t);
            d.isList = t.IsGenericType && typeof(IList).IsAssignableFrom(t);

            if (d.isDictionary)
                d.GenericTypes = t.GetGenericArguments();
            d.isValueType = t.IsValueType;
            d.isArray = t.IsArray;
            if (d.isArray)
                d.bt = t.GetElementType();
            if (d.isGenericType)
                d.bt = t.GetGenericArguments()[0];
            d.isByteArray = t == typeof(byte[]);
            d.isGuid = (t == typeof(Guid) || t == typeof(Guid?));
#if !SILVERLIGHT
            d.isHashtable = t == typeof(Hashtable);
            d.isDataSet = t == typeof(DataSet);
            d.isDataTable = t == typeof(DataTable);
#endif

            d.changeType = GetChangeType(t);
            d.isEnum = t.IsEnum;
            d.isDateTime = t == typeof(DateTime) || t == typeof(DateTime?);
            d.isInt = t == typeof(int) || t == typeof(int?);
            d.isLong = t == typeof(long) || t == typeof(long?);
            d.isString = t == typeof(string);
            d.isBool = t == typeof(bool) || t == typeof(bool?);
            d.isClass = t.IsClass;
            d.isInterface= t.IsInterface;

            if (d.isDictionary && d.GenericTypes.Length > 0 && d.GenericTypes[0] == typeof(string))
                d.isStringDictionary = true;

            return d;
        }

        private object ChangeType(object value, Type conversionType, Dictionary<string, object> globaltypes, bool usingglobals)
        {
            if (value.GetType() == conversionType) return value;

            if (conversionType == typeof(int))
                return (int)((long)value);

            if (conversionType == typeof(long))
                return (long)value;

            if (conversionType == typeof(string))
                return value;

            if (conversionType == typeof(Guid))
                return CreateGuid((string)value);

            if (conversionType.IsEnum)
                return CreateEnum(conversionType, (string)value);

            if (value is IList)
            {
                if (_deserializers_list.ContainsKey(conversionType)) return _deserializers_list[conversionType](Listify(value), conversionType, (t, x) => ChangeType(DecodeParsed(t,globaltypes, x,usingglobals), t, globaltypes,usingglobals));
                if (conversionType.IsGenericType && _deserializers_list.ContainsKey(conversionType.GetGenericTypeDefinition())) return _deserializers_list[conversionType.GetGenericTypeDefinition()](Listify(value), conversionType, (t, x) => ChangeType(DecodeParsed(t, globaltypes, x,usingglobals), t, globaltypes,usingglobals));
            }
            else
            {
                if (_deserializers_value.ContainsKey(conversionType)) return _deserializers_value[conversionType](value, conversionType, (t, x) => ChangeType(DecodeParsed(t,globaltypes, x,usingglobals), t, globaltypes,usingglobals));
                if (conversionType.IsGenericType && _deserializers_value.ContainsKey(conversionType.GetGenericTypeDefinition())) return _deserializers_value[conversionType.GetGenericTypeDefinition()](value, conversionType, (t, x) => ChangeType(DecodeParsed(t, globaltypes, x,usingglobals), t, globaltypes,usingglobals));
            }

            return Convert.ChangeType(value, conversionType, CultureInfo.InvariantCulture);
        }

        private List<object> Listify(object value)
        {
            var list_of_objects = value as List<object>;
            if (list_of_objects != null) return list_of_objects;
            var list = value as IList;
            return list == null ? null : list.Cast<object>().ToList();
        }

        #endregion

        #region [   p r i v a t e   m e t h o d s   ]

        private object RootList(object parse, Type type, Dictionary<string, object> globaltypes)
        {
            var gtypes = type.GetGenericArguments();
            var o = (IList)_reflection.FastCreateInstance(type);
            foreach (var k in (IList)parse)
            {
                object v;
                if (k is Dictionary<string, object>)
                    v = ParseDictionary(k as Dictionary<string, object>, globaltypes, gtypes[0], null, false);
                else
                    v = ChangeType(k, gtypes[0], globaltypes,false);

                o.Add(v);
            }
            return o;
        }

        private object RootDictionary(object parse, Type type, Dictionary<string, object> globaltypes, bool usingglobals)
        {
            var gtypes = type.GetGenericArguments();
            if (parse is Dictionary<string, object>)
            {
                var o = (IDictionary)_reflection.FastCreateInstance(type);

                foreach (var kv in (Dictionary<string, object>)parse)
                {
                    object v;
                    object k = ChangeType(kv.Key, gtypes[0], globaltypes, usingglobals);
                    if (kv.Value is Dictionary<string, object>)
                        v = ParseDictionary(kv.Value as Dictionary<string, object>, globaltypes, gtypes[1], null, usingglobals);
                    else if (kv.Value is List<object>)
                        v = CreateArray(kv.Value as List<object>, typeof(object), typeof(object), null, usingglobals);
                    else
                        v = ChangeType(kv.Value, gtypes[1], globaltypes,usingglobals);
                    o.Add(k, v);
                }

                return o;
            }
            if (parse is List<object>)
                return CreateDictionary(parse as List<object>, type, gtypes, null, usingglobals);

            return null;
        }

        private object ParseDictionary(Dictionary<string, object> d, Dictionary<string, object> globaltypes, Type type, object input, bool usingglobals)
        {
            object tn;

            if (globaltypes==null && d.TryGetValue("$types", out tn))
            {
                usingglobals = true;
                globaltypes = new Dictionary<string, object>();
                foreach (var kv in (Dictionary<string, object>)tn)
                {
                    globaltypes.Add((string)kv.Value, kv.Key);
                }
            }

            bool found = d.TryGetValue("$type", out tn);
#if !SILVERLIGHT
            if (found == false && type == typeof(System.Object))
            {
                return CreateDataset(d, globaltypes, usingglobals);
            }
#endif
            if (found)
            {
                if (usingglobals && globaltypes!=null)
                {
                    object tname = "";
                    if (globaltypes.TryGetValue((string)tn, out tname))
                        tn = tname;
                }
                type = _reflection.GetTypeFromCache((string)tn);
            }

            if (type == null)
                throw new Exception("Cannot determine type");

            string typename = type.FullName;
            object o = input;
            if (o == null)
                o = _reflection.FastCreateInstance(type);

            SafeDictionary<string, myPropInfo> props = Getproperties(type, typename);
            foreach (string n in d.Keys)
            {
                string name = n;
                if (_params.IgnoreCaseOnDeserialize) name = name.ToLower();
                if (name == "$map")
                {
                    ProcessMap(o, props, (Dictionary<string, object>)d[name]);
                    continue;
                }
                myPropInfo pi;
                if (props.TryGetValue(name, out pi) == false)
                    continue;
                if (pi.filled && pi.CanWrite)
                {
                    object v = d[name];

                    
                    if (v != null)
                    {
                        object oset = null;

                        try
                        {
                            if (pi.isInt)
                                oset = (int) ((long) v);

                            else if (pi.isLong)
                                oset = (long) v;

                            else if (pi.isString)
                                oset = (string) v;

                            else if (pi.isBool)
                                oset = (bool) v;

                            else if (pi.isGenericType && !pi.isValueType && pi.isList)
                                oset = CreateGenericList((List<object>) v, pi.pt, pi.bt, globaltypes, usingglobals);

                            else if (pi.isByteArray)
                                oset = Convert.FromBase64String((string) v);

                            else if (pi.isArray && pi.isValueType == false)
                                oset = CreateArray((List<object>) v, pi.pt, pi.bt, globaltypes, usingglobals);

                            else if (pi.isGuid)
                                oset = CreateGuid((string) v);
#if !SILVERLIGHT
                            else if (pi.isDataSet)
                                oset = CreateDataset((Dictionary<string, object>) v, globaltypes, usingglobals);

                            else if (pi.isDataTable)
                                oset = this.CreateDataTable((Dictionary<string, object>) v, globaltypes, usingglobals);
#endif

                            else if (pi.isStringDictionary)
                                oset = CreateStringKeyDictionary((Dictionary<string, object>) v, pi.pt, pi.GenericTypes,
                                    globaltypes, usingglobals
                                    );
#if !SILVERLIGHT
                            else if (pi.isDictionary || pi.isHashtable)
#else
                        else if (pi.isDictionary)
#endif
                                oset = CreateDictionary((List<object>) v, pi.pt, pi.GenericTypes, globaltypes, usingglobals);

                            else if (pi.isEnum)
                                oset = CreateEnum(pi.pt, (string) v);

                            else if (pi.isDateTime)
                                oset = CreateDateTime((string) v);

                            else if ((pi.isClass || pi.isInterface || pi.isValueType) && v is Dictionary<string, object>)
                                oset = ParseDictionary((Dictionary<string, object>) v, globaltypes, pi.pt,
                                    pi.getter(o),usingglobals);

                            else if (pi.isValueType)
                                oset = ChangeType(v, pi.changeType, globaltypes,usingglobals);

                            else if (v is List<object>)
                                oset = CreateArray((List<object>) v, pi.pt, typeof (object), globaltypes, usingglobals);

                            else
                                oset = v;

                            o = pi.setter(o, oset);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Error deserializing member "+name+"("+pi.pt.AssemblyQualifiedName+") of "+type.AssemblyQualifiedName+": "+ex.Message, ex);
                        }
                    }
                }
            }
            return o;
        }

        private void ProcessMap(object obj, SafeDictionary<string, JSON.myPropInfo> props, Dictionary<string, object> dic)
        {
            foreach (KeyValuePair<string, object> kv in dic)
            {
                myPropInfo p = props[kv.Key];
                object o = p.getter(obj);
                Type t = Type.GetType((string)kv.Value);
                if (t == typeof(Guid))
                    p.setter(obj, CreateGuid((string)o));
            }
        }

        private long CreateLong(string s)
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

        private object CreateEnum(Type pt, string v)
        {
            // TODO : optimize create enum
#if !SILVERLIGHT
            return Enum.Parse(pt, v);
#else
            return Enum.Parse(pt, v, true);
#endif
        }

        private Guid CreateGuid(string s)
        {
            if (s.Length > 30)
                return new Guid(s);
            else
                return new Guid(Convert.FromBase64String(s));
        }

        private DateTime CreateDateTime(string value)
        {
            bool utc = false;
            //                   0123456789012345678
            // datetime format = yyyy-MM-dd HH:mm:ss
            int year = (int)CreateLong(value.Substring(0, 4));
            int month = (int)CreateLong(value.Substring(5, 2));
            int day = (int)CreateLong(value.Substring(8, 2));
            int hour = (int)CreateLong(value.Substring(11, 2));
            int min = (int)CreateLong(value.Substring(14, 2));
            int sec = (int)CreateLong(value.Substring(17, 2));

            if (value.EndsWith("Z"))
                utc = true;

            if (_params.UseUTCDateTime == false && utc == false)
                return new DateTime(year, month, day, hour, min, sec);
            else
                return new DateTime(year, month, day, hour, min, sec, DateTimeKind.Utc);
        }

        private object CreateArray(List<object> data, Type pt, Type bt, Dictionary<string, object> globalTypes, bool usingglobals)
        {
            Array col = Array.CreateInstance(bt, data.Count);
            // create an array of objects
            for (int i = 0; i < data.Count; i++)// each (object ob in data)
            {
                object ob = data[i];
                if (ob is IDictionary)
                    col.SetValue(ParseDictionary((Dictionary<string, object>)ob, globalTypes, bt, null,usingglobals), i);
                else
                    col.SetValue(ChangeType(ob, bt, globalTypes,usingglobals), i);
            }

            return col;
        }


        private object CreateGenericList(List<object> data, Type pt, Type bt, Dictionary<string, object> globalTypes, bool usingglobals)
        {
            IList col = (IList)_reflection.FastCreateInstance(pt);
            // create an array of objects
            foreach (object ob in data)
            {
                if (ob is IDictionary)
                    col.Add(ParseDictionary((Dictionary<string, object>)ob, globalTypes, bt, null,usingglobals));

                else if (ob is List<object>)
                    col.Add(((List<object>)ob).ToArray());

                else
                    col.Add(ChangeType(ob, bt, globalTypes, usingglobals));
            }
            return col;
        }

        private object CreateStringKeyDictionary(Dictionary<string, object> reader, Type pt, Type[] types, Dictionary<string, object> globalTypes, bool usingglobals)
        {
            var col = (IDictionary)_reflection.FastCreateInstance(pt);
            Type t1 = null;
            Type t2 = null;
            if (types != null)
            {
                t1 = types[0];
                t2 = types[1];
            }

            foreach (KeyValuePair<string, object> values in reader)
            {
                var key = values.Key;//ChangeType(values.Key, t1);
                object val = null;
                if (values.Value is Dictionary<string, object>)
                    val = ParseDictionary((Dictionary<string, object>)values.Value, globalTypes, t2, null, usingglobals);
                else
                    val = ChangeType(values.Value, t2, globalTypes,usingglobals);
                col.Add(key, val);
            }

            return col;
        }

        private object CreateDictionary(List<object> reader, Type pt, Type[] types, Dictionary<string, object> globalTypes, bool usingglobals)
        {
            IDictionary col = (IDictionary)_reflection.FastCreateInstance(pt);
            Type t1 = null;
            Type t2 = null;
            if (types != null)
            {
                t1 = types[0];
                t2 = types[1];
            }

            foreach (Dictionary<string, object> values in reader)
            {
                object key = values["k"];
                object val = values["v"];

                if (key is Dictionary<string, object>)
                    key = ParseDictionary((Dictionary<string, object>)key, globalTypes, t1, null,usingglobals);
                else
                    key = ChangeType(key, t1, globalTypes, usingglobals);

                if (val is Dictionary<string, object>)
                    val = ParseDictionary((Dictionary<string, object>)val, globalTypes, t2, null,usingglobals);
                else
                    val = ChangeType(val, t2, globalTypes, usingglobals);

                col.Add(key, val);
            }

            return col;
        }

        private Type GetChangeType(Type conversionType)
        {
            if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                return conversionType.GetGenericArguments()[0];

            return conversionType;
        }
#if !SILVERLIGHT
        private DataSet CreateDataset(Dictionary<string, object> reader, Dictionary<string, object> globalTypes, bool usingglobals)
        {
            DataSet ds = new DataSet();
            ds.EnforceConstraints = false;
            ds.BeginInit();

            // read dataset schema here
            ReadSchema(reader, ds, globalTypes, usingglobals);

            foreach (KeyValuePair<string, object> pair in reader)
            {
                if (pair.Key == "$type" || pair.Key == "$schema") continue;

                List<object> rows = (List<object>)pair.Value;
                if (rows == null) continue;

                DataTable dt = ds.Tables[pair.Key];
                ReadDataTable(rows, dt);
            }

            ds.EndInit();

            return ds;
        }

        private void ReadSchema(Dictionary<string, object> reader, DataSet ds, Dictionary<string, object> globalTypes, bool usingglobals)
        {
            var schema = reader["$schema"];

            if (schema is string)
            {
                TextReader tr = new StringReader((string)schema);
                ds.ReadXmlSchema(tr);
            }
            else
            {
                var ms = (DatasetSchema)ParseDictionary((Dictionary<string, object>)schema, globalTypes, typeof(DatasetSchema), null, usingglobals);
                ds.DataSetName = ms.Name;
                for (int i = 0; i < ms.Info.Count; i += 3)
                {
                    if (ds.Tables.Contains(ms.Info[i]) == false)
                        ds.Tables.Add(ms.Info[i]);
                    ds.Tables[ms.Info[i]].Columns.Add(ms.Info[i + 1], Type.GetType(ms.Info[i + 2]));
                }
            }
        }

        private void ReadDataTable(List<object> rows, DataTable dt)
        {
            dt.BeginInit();
            dt.BeginLoadData();
            List<int> guidcols = new List<int>();
            List<int> datecol = new List<int>();

            foreach (DataColumn c in dt.Columns)
            {
                if (c.DataType == typeof(Guid) || c.DataType == typeof(Guid?))
                    guidcols.Add(c.Ordinal);
                if (_params.UseUTCDateTime && (c.DataType == typeof(DateTime) || c.DataType == typeof(DateTime?)))
                    datecol.Add(c.Ordinal);
            }

            foreach (List<object> row in rows)
            {
                object[] v = new object[row.Count];
                row.CopyTo(v, 0);
                foreach (int i in guidcols)
                {
                    string s = (string)v[i];
                    if (s != null && s.Length < 36)
                        v[i] = new Guid(Convert.FromBase64String(s));
                }
                if (_params.UseUTCDateTime)
                {
                    foreach (int i in datecol)
                    {
                        string s = (string)v[i];
                        if (s != null)
                            v[i] = CreateDateTime(s);
                    }
                }
                dt.Rows.Add(v);
            }

            dt.EndLoadData();
            dt.EndInit();
        }

        DataTable CreateDataTable(Dictionary<string, object> reader, Dictionary<string, object> globalTypes, bool usingglobals)
        {
            var dt = new DataTable();

            // read dataset schema here
            var schema = reader["$schema"];

            if (schema is string)
            {
                TextReader tr = new StringReader((string)schema);
                dt.ReadXmlSchema(tr);
            }
            else
            {
                var ms = (DatasetSchema)this.ParseDictionary((Dictionary<string, object>)schema, globalTypes, typeof(DatasetSchema), null, usingglobals);
                dt.TableName = ms.Info[0];
                for (int i = 0; i < ms.Info.Count; i += 3)
                {
                    dt.Columns.Add(ms.Info[i + 1], Type.GetType(ms.Info[i + 2]));
                }
            }

            foreach (var pair in reader)
            {
                if (pair.Key == "$type" || pair.Key == "$schema")
                    continue;

                var rows = (List<object>)pair.Value;
                if (rows == null)
                    continue;

                if (!dt.TableName.Equals(pair.Key, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                ReadDataTable(rows, dt);
            }

            return dt;
        }
#endif
        #endregion


        private readonly Dictionary<Type, CustomSerialization> _serializers = new Dictionary<Type, CustomSerialization>();
        private readonly Dictionary<Type, CustomDeserialization_dict> _deserializers_dict = new Dictionary<Type, CustomDeserialization_dict>();
        private readonly Dictionary<Type, CustomDeserialization_list> _deserializers_list = new Dictionary<Type, CustomDeserialization_list>();
        private readonly Dictionary<Type, CustomDeserialization_value> _deserializers_value = new Dictionary<Type, CustomDeserialization_value>();

        public void RegisterCustomSerializer(Type type, CustomSerialization serialize)
        {
            if (_serializers.ContainsKey(type)) _serializers.Remove(type);
            _serializers.Add(type, serialize);
        }

        public void RegisterCustomDeserializer_d(Type type, CustomDeserialization_dict deserialize)
        {
            if (_deserializers_dict.ContainsKey(type)) _deserializers_dict.Remove(type);
            _deserializers_dict.Add(type, deserialize);
        }

        public void RegisterCustomDeserializer_l(Type type, CustomDeserialization_list deserialize)
        {
            if (_deserializers_list.ContainsKey(type)) _deserializers_list.Remove(type);
            _deserializers_list.Add(type, deserialize);
        }

        public void RegisterCustomDeserializer_v(Type type, CustomDeserialization_value deserialize)
        {
            if (_deserializers_value.ContainsKey(type)) _deserializers_value.Remove(type);
            _deserializers_value.Add(type, deserialize);
        }

        public void RegisterCustomSerializer<T>(CustomSerialization<T> serialize)
        {
            RegisterCustomSerializer(typeof (T), (t, output, defer) => serialize((T) t, output, defer));
        }

        public void RegisterCustomDeserializer_d<T>(CustomDeserialization_dict<T> deserialize)
        {
            RegisterCustomDeserializer_d(typeof(T), (input, type, defer) => deserialize(input, type, defer));
        }

        public void RegisterCustomDeserializer_l<T>(CustomDeserialization_list<T> deserialize)
        {
            RegisterCustomDeserializer_l(typeof(T), (input, type, defer) => deserialize(input, type, defer));
        }

        public void RegisterCustomDeserializer_v<T>(CustomDeserialization_value<T> deserialize)
        {
            RegisterCustomDeserializer_v(typeof(T), (input, type, defer) => deserialize(input, type, defer));
        }

        public void ClearCustom()
        {
            _serializers.Clear();
            _deserializers_dict.Clear();
            _deserializers_list.Clear();
            _deserializers_value.Clear();
        }
    }

    

}