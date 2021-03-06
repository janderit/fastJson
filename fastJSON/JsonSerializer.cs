﻿using System;
using System.Collections;
using System.Collections.Generic;
#if !SILVERLIGHT
using System.Data;
#endif
using System.Globalization;
using System.IO;
using System.Text;

namespace fastJSON
{
    internal sealed class JSONSerializer
    {
        private StringBuilder _output = new StringBuilder();
        private StringBuilder _before = new StringBuilder();
        private const int _MAX_DEPTH = 10;
        int _current_depth;
        private readonly Dictionary<string, int> _globalTypes = new Dictionary<string, int>();
        private readonly JSONParameters _params;
        private readonly Dictionary<Type, CustomSerialization> _serializers;
        private readonly Reflection _reflection;

        internal JSONSerializer(JSONParameters @params, Dictionary<Type, CustomSerialization> serializers, Reflection reflection)
        {
            _params = @params;
            _serializers = serializers;
            _reflection = reflection;
        }

        internal string ConvertToJSON(object obj)
        {
            WriteValue(obj);

            string str;
            if (_params.UsingGlobalTypes && _globalTypes != null && _globalTypes.Count > 0)
            {
                StringBuilder sb = _before;
                sb.Append("\"$types\":{");
                bool pendingSeparator = false;
                foreach (var kv in _globalTypes)
                {
                    if (pendingSeparator) sb.Append(',');
                    pendingSeparator = true;
                    sb.Append("\"");
                    sb.Append(kv.Key);
                    sb.Append("\":\"");
                    sb.Append(kv.Value);
                    sb.Append("\"");
                }
                sb.Append("},");
                sb.Append(_output);
                str = sb.ToString();
            }
            else
                str = _output.ToString();

            return str;
        }

        private void WriteValue(object obj)
        {
            if (obj == null || obj is DBNull)
                _output.Append("null");

            else if (obj is string || obj is char)
                WriteString(obj.ToString());

            else if (obj is Guid)
                WriteGuid((Guid)obj);

            else if (obj is bool)
                _output.Append(((bool)obj) ? "true" : "false"); // conform to standard

            else if (
                obj is int || obj is long || obj is double ||
                obj is decimal || obj is float ||
                obj is byte || obj is short ||
                obj is sbyte || obj is ushort ||
                obj is uint || obj is ulong
            )
                _output.Append(((IConvertible)obj).ToString(NumberFormatInfo.InvariantInfo));

            else if (obj is DateTime)
                WriteDateTime((DateTime)obj);

            else if (obj is IDictionary && obj.GetType().IsGenericType && obj.GetType().GetGenericArguments()[0] == typeof(string))
                WriteStringDictionary((IDictionary)obj);

            else if (obj is IDictionary)
                WriteDictionary((IDictionary)obj);
#if !SILVERLIGHT
            else if (obj is DataSet)
                WriteDataset((DataSet)obj);

            else if (obj is DataTable)
                this.WriteDataTable((DataTable)obj);
#endif
            else if (obj is byte[])
                WriteBytes((byte[])obj);

            else if (obj is Array || obj is IList || obj is ICollection)
                WriteArray((IEnumerable)obj);

            else if (obj is Enum)
                WriteEnum((Enum)obj);

            else
                WriteObject(obj);
        }

        private void WriteEnum(Enum e)
        {
            // TODO : optimize enum write
            WriteStringFast(e.ToString());
        }

        private void WriteGuid(Guid g)
        {
            if (_params.UseFastGuid == false)
                WriteStringFast(g.ToString());
            else
                WriteBytes(g.ToByteArray());
        }

        private void WriteBytes(byte[] bytes)
        {
#if !SILVERLIGHT
            WriteStringFast(Convert.ToBase64String(bytes, 0, bytes.Length, Base64FormattingOptions.None));
#else
            WriteStringFast(Convert.ToBase64String(bytes, 0, bytes.Length));
#endif
        }

        private void WriteDateTime(DateTime dateTime)
        {
            // datetime format standard : yyyy-MM-dd HH:mm:ss
            DateTime dt = dateTime;
            if (_params.UseUTCDateTime)
                dt = dateTime.ToUniversalTime();

            _output.Append("\"");
            _output.Append(dt.Year.ToString("0000", NumberFormatInfo.InvariantInfo));
            _output.Append("-");
            _output.Append(dt.Month.ToString("00", NumberFormatInfo.InvariantInfo));
            _output.Append("-");
            _output.Append(dt.Day.ToString("00", NumberFormatInfo.InvariantInfo));
            _output.Append(" ");
            _output.Append(dt.Hour.ToString("00", NumberFormatInfo.InvariantInfo));
            _output.Append(":");
            _output.Append(dt.Minute.ToString("00", NumberFormatInfo.InvariantInfo));
            _output.Append(":");
            _output.Append(dt.Second.ToString("00", NumberFormatInfo.InvariantInfo));

            if (_params.UseUTCDateTime || dt.Kind==DateTimeKind.Utc)
                _output.Append("Z");

            _output.Append("\"");
        }

#if !SILVERLIGHT
        private DatasetSchema GetSchema(DataTable ds)
        {
            if (ds == null) return null;

            var m = new DatasetSchema {Info = new List<string>(), Name = ds.TableName};

            foreach (DataColumn c in ds.Columns)
            {
                m.Info.Add(ds.TableName);
                m.Info.Add(c.ColumnName);
                m.Info.Add(c.DataType.ToString());
            }
            // FEATURE : serialize relations and constraints here

            return m;
        }

        private DatasetSchema GetSchema(DataSet ds)
        {
            if (ds == null) return null;

            var m = new DatasetSchema {Info = new List<string>(), Name = ds.DataSetName};

            foreach (DataTable t in ds.Tables)
            {
                foreach (DataColumn c in t.Columns)
                {
                    m.Info.Add(t.TableName);
                    m.Info.Add(c.ColumnName);
                    m.Info.Add(c.DataType.ToString());
                }
            }
            // FEATURE : serialize relations and constraints here

            return m;
        }

        private string GetXmlSchema(DataTable dt)
        {
            using (var writer = new StringWriter())
            {
                dt.WriteXmlSchema(writer);
                return dt.ToString();
            }
        }

        private void WriteDataset(DataSet ds)
        {
            _output.Append('{');
            if ( _params.UseExtensions)
            {
                WritePair("$schema", _params.UseOptimizedDatasetSchema ? (object)GetSchema(ds) : ds.GetXmlSchema());
                _output.Append(',');
            }
            bool tablesep = false;
            foreach (DataTable table in ds.Tables)
            {
                if (tablesep) _output.Append(",");
                tablesep = true;
                WriteDataTableData(table);
            }
            // end dataset
            _output.Append('}');
        }

        private void WriteDataTableData(DataTable table)
        {
            _output.Append('\"');
            _output.Append(table.TableName);
            _output.Append("\":[");
            DataColumnCollection cols = table.Columns;
            bool rowseparator = false;
            foreach (DataRow row in table.Rows)
            {
                if (rowseparator) _output.Append(",");
                rowseparator = true;
                _output.Append('[');

                bool pendingSeperator = false;
                foreach (DataColumn column in cols)
                {
                    if (pendingSeperator) _output.Append(',');
                    WriteValue(row[column]);
                    pendingSeperator = true;
                }
                _output.Append(']');
            }

            _output.Append(']');
        }

        void WriteDataTable(DataTable dt)
        {
            this._output.Append('{');
            if (_params.UseExtensions)
            {
                this.WritePair("$schema", _params.UseOptimizedDatasetSchema ? (object)this.GetSchema(dt) : this.GetXmlSchema(dt));
                this._output.Append(',');
            }

            WriteDataTableData(dt);

            // end datatable
            this._output.Append('}');
        }
#endif

        bool _TypesWritten;



        private void WriteObject(object obj)
        {
            var typesnonempty = false;

            if (_serializers.ContainsKey(obj.GetType()))
            {
                var custom = _serializers[obj.GetType()];
                var target = new CustomTarget(_output, WriteValue);
                custom(obj, target, target.Defer);
                target.WriteToTarget();
            }
            else if (obj.GetType().IsGenericType && _serializers.ContainsKey(obj.GetType().GetGenericTypeDefinition()))
            {
                var custom = _serializers[obj.GetType().GetGenericTypeDefinition()];
                var target = new CustomTarget(_output, WriteValue);
                custom(obj, target, target.Defer);
                target.WriteToTarget();
            }
            else {

                if (_params.UsingGlobalTypes == false)
                    _output.Append('{');
                else
                {
                    if (_TypesWritten == false)
                    {
                        _output.Append("{");
                        _before = _output;
                        _output = new StringBuilder();
                    }
                    else
                        _output.Append("{");
                }
                _TypesWritten = true;
                _current_depth++;
                if (_current_depth > _MAX_DEPTH)
                    throw new Exception("Serializer encountered maximum depth of " + _MAX_DEPTH);


                var map = new Dictionary<string, string>();
                Type t = obj.GetType();
                bool append = false;
                if (_params.UseExtensions)
                {
                    if (_params.UsingGlobalTypes == false)
                    {
                        WritePairFast("$type", _reflection.GetTypeAssemblyName(t));
                        typesnonempty = true;
                    }
                    else
                    {
                        int dt;
                        string ct = _reflection.GetTypeAssemblyName(t);
                        if (_globalTypes.TryGetValue(ct, out dt) == false)
                        {
                            dt = _globalTypes.Count + 1;
                            _globalTypes.Add(ct, dt);
                        }
                        WritePairFast("$type", dt.ToString(CultureInfo.InvariantCulture));
                        typesnonempty = true;
                    }
                    append = true;
                }

                List<Getters> g = _reflection.GetGetters(t);
                int gc = g.Count;
                int i = g.Count;
                foreach (var p in g)
                {
                    i--;
                    if (append && i > 0) _output.Append(',');
                    object o = p.Getter(obj);
                    if ((o == null || o is DBNull) && _params.SerializeNullValues == false)
                        append = false;
                    else
                    {
                        if (i == 0 && (gc > 1 || typesnonempty)) // last non null
                            _output.Append(",");
                        WritePair(p.Name, o);
                        if (o != null && _params.UseExtensions)
                        {
                            Type tt = o.GetType();
                            if (tt == typeof (Object))
                                map.Add(p.Name, tt.ToString());
                        }
                        append = true;
                    }
                }


                if (map.Count > 0 && _params.UseExtensions)
                {
                    _output.Append(",\"$map\":");
                    WriteStringDictionary(map);
                }

                _current_depth--;
                _output.Append('}');
            }

            _current_depth--;
        }

        private void WritePairFast(string name, string value)
        {
            if ((value == null) && _params.SerializeNullValues == false)
                return;
            WriteStringFast(name);

            _output.Append(':');

            WriteStringFast(value);
        }

        private void WritePair(string name, object value)
        {
            if ((value == null || value is DBNull) && _params.SerializeNullValues == false)
                return;
            WriteStringFast(name);

            _output.Append(':');

            WriteValue(value);
        }

        private void WriteArray(IEnumerable array)
        {
            _output.Append('[');

            bool pendingSeperator = false;

            foreach (object obj in array)
            {
                if (pendingSeperator) _output.Append(',');

                WriteValue(obj);

                pendingSeperator = true;
            }
            _output.Append(']');
        }

        private void WriteStringDictionary(IDictionary dic)
        {
            _output.Append('{');

            bool pendingSeparator = false;

            foreach (DictionaryEntry entry in dic)
            {
                if (pendingSeparator) _output.Append(',');

                WritePair((string)entry.Key, entry.Value);

                pendingSeparator = true;
            }
            _output.Append('}');
        }

        private void WriteDictionary(IDictionary dic)
        {
            _output.Append('[');

            bool pendingSeparator = false;

            foreach (DictionaryEntry entry in dic)
            {
                if (pendingSeparator) _output.Append(',');
                _output.Append('{');
                WritePair("k", entry.Key);
                _output.Append(",");
                WritePair("v", entry.Value);
                _output.Append('}');

                pendingSeparator = true;
            }
            _output.Append(']');
        }

        private void WriteStringFast(string s)
        {
            _output.Append('\"');
            _output.Append(s);
            _output.Append('\"');
        }

        private void WriteString(string s)
        {
            _output.Append('\"');

            int runIndex = -1;

            for (var index = 0; index < s.Length; ++index)
            {
                var c = s[index];

                if (c >= ' ' && c < 128 && c != '\"' && c != '\\')
                {
                    if (runIndex == -1)
                    {
                        runIndex = index;
                    }

                    continue;
                }

                if (runIndex != -1)
                {
                    _output.Append(s, runIndex, index - runIndex);
                    runIndex = -1;
                }

                switch (c)
                {
                    case '\t': _output.Append("\\t"); break;
                    case '\r': _output.Append("\\r"); break;
                    case '\n': _output.Append("\\n"); break;
                    case '"':
                    case '\\': _output.Append('\\'); _output.Append(c); break;
                    default:
                        _output.Append("\\u");
                        _output.Append(((int)c).ToString("X4", NumberFormatInfo.InvariantInfo));
                        break;
                }
            }

            if (runIndex != -1)
            {
                _output.Append(s, runIndex, s.Length - runIndex);
            }

            _output.Append('\"');
        }
    }

    internal sealed class CustomTarget : Serializer
    {
        private readonly StringBuilder _sb;
        private readonly Action<object> _defer;
        private readonly Queue<Action> _actions = new Queue<Action>();

        private bool? _isObject;

        public CustomTarget(StringBuilder sb, Action<object> defer)
        {
            _sb = sb;
            _defer = defer;
        }

        public void WriteValue(string value)
        {
            if (_isObject.HasValue) throw new InvalidOperationException("FASTJSON/CUSTOM ERROR 00A");
            _isObject = false;
            _actions.Enqueue(() =>
            {
                _sb.Append('"');
                _sb.Append(value);
                _sb.Append('"');
            });
        }

        public void WriteField(string key, SerializerData field)
        {
            if (_isObject.HasValue && !_isObject.Value) throw new InvalidOperationException("FASTJSON/CUSTOM ERROR 00B");
            _isObject = true;
            _actions.Enqueue(() =>
            {
                _sb.Append('"');
                _sb.Append(key);
                _sb.Append('"');
                _sb.Append(':');
            });
            _actions.Enqueue(field.Payload);
        }

        public void WriteList(IEnumerable<SerializerData> items)
        {
            if (_isObject.HasValue && _isObject.Value) throw new InvalidOperationException("FASTJSON/CUSTOM ERROR 00C");
            _isObject = false;
            var pendingSeperator = false;
            _actions.Enqueue(() => _sb.Append('['));
            foreach (var item in items) {
                if (pendingSeperator) _actions.Enqueue(() => _sb.Append(','));
                _actions.Enqueue(item.Payload);
                pendingSeperator = true;
            }
            _actions.Enqueue(() => _sb.Append(']'));
        }

        public void EmptyObject()
        {
            if (_isObject.HasValue && !_isObject.Value) throw new InvalidOperationException("FASTJSON/CUSTOM ERROR 00D");
            _isObject = true;
        }

        public void Defer(SerializerData deferral)
        {
            if (_isObject.HasValue && _isObject.Value) throw new InvalidOperationException("FASTJSON/CUSTOM ERROR 00E");
            _isObject = false;
            _actions.Enqueue(deferral.Payload);
        }

        public SerializerData String(string s)
        {
            return new SerializerData(() =>
            {
                _sb.Append('"');
                _sb.Append(s);
                _sb.Append('"');
            });
        }

        public SerializerData Defer(object o)
        {
            return new SerializerData(() => _defer(o));
        }

        public void WriteToTarget()
        {
            if (_isObject.HasValue && _isObject.Value) _sb.Append('{');
            while (_actions.Count > 0) _actions.Dequeue()();
            if (_isObject.HasValue && _isObject.Value) _sb.Append('}');
        }
    }
}
