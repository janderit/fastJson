using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;

namespace fastJSON
{
    internal struct Getters
    {
        public string Name;
        public Reflection.GenericGetter Getter;
        public Type propertyType;
    }

    internal sealed class Reflection
    {
        public bool ShowReadOnlyProperties = false;
        internal delegate object GenericSetter(object target, object value);
        internal delegate object GenericGetter(object obj);
        private delegate object CreateObject();
        
        private readonly SafeDictionary<Type, string> _tyname = new SafeDictionary<Type, string>();
        private readonly SafeDictionary<string, Type> _typecache = new SafeDictionary<string, Type>();
        private readonly SafeDictionary<Type, CreateObject> _constrcache = new SafeDictionary<Type, CreateObject>();
        private readonly SafeDictionary<Type, List<Getters>> _getterscache = new SafeDictionary<Type, List<Getters>>();

        #region [   PROPERTY GET SET   ]
        internal string GetTypeAssemblyName(Type t)
        {
            string val;
            if (_tyname.TryGetValue(t, out val))
                return val;
            
            var s = t.AssemblyQualifiedName;
            _tyname.Add(t, s);
            return s;
        }

        internal Type GetTypeFromCache(string typename)
        {
            Type val;
            if (_typecache.TryGetValue(typename, out val))
                return val;
            
            var t = Type.GetType(typename);
            _typecache.Add(typename, t);
            return t;
        }

        internal object FastCreateInstance(Type objtype)
        {
            try
            {
                CreateObject c;
                if (_constrcache.TryGetValue(objtype, out c))
                {
                    return c();
                }
                
                if (objtype.IsClass) 
                {
                    var dynMethod = new DynamicMethod("_", objtype, null);
                    var ilGen = dynMethod.GetILGenerator();
                    ilGen.Emit(OpCodes.Newobj, objtype.GetConstructor(Type.EmptyTypes));
                    ilGen.Emit(OpCodes.Ret);
                    c = (CreateObject)dynMethod.CreateDelegate(typeof(CreateObject));
                    _constrcache.Add(objtype, c);
                }
                else // structs
                {     
                    var dynMethod = new DynamicMethod("_",
                        MethodAttributes.Public | MethodAttributes.Static,
                        CallingConventions.Standard,
                        typeof(object),
                        null,
                        objtype, false);
                    var ilGen = dynMethod.GetILGenerator();
                    var lv = ilGen.DeclareLocal(objtype);
                    ilGen.Emit(OpCodes.Ldloca_S, lv);
                    ilGen.Emit(OpCodes.Initobj, objtype);
                    ilGen.Emit(OpCodes.Ldloc_0);
                    ilGen.Emit(OpCodes.Box, objtype);
                    ilGen.Emit(OpCodes.Ret);
                    c = (CreateObject)dynMethod.CreateDelegate(typeof(CreateObject));
                    _constrcache.Add(objtype, c);
                }
                return c();
            }
            catch (Exception exc)
            {
                throw new Exception(string.Format("Failed to fast create instance for type '{0}' from assemebly '{1}'",
                    objtype.FullName, objtype.AssemblyQualifiedName), exc);
            }
        }

        internal static GenericSetter CreateSetField(Type type, FieldInfo fieldInfo)
        {
            var arguments = new Type[2];
            arguments[0] = arguments[1] = typeof(object);

            var dynamicSet = new DynamicMethod("_", typeof(object), arguments, type, true);
            var il = dynamicSet.GetILGenerator();

            if (!type.IsClass) // structs
            {
                var lv = il.DeclareLocal(type); 
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Unbox_Any, type);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloca_S, lv);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(fieldInfo.FieldType.IsClass ? OpCodes.Castclass : OpCodes.Unbox_Any, fieldInfo.FieldType);
                il.Emit(OpCodes.Stfld, fieldInfo);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Box, type);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                if (fieldInfo.FieldType.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
                il.Emit(OpCodes.Stfld, fieldInfo);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ret);
            }
            return (GenericSetter)dynamicSet.CreateDelegate(typeof(GenericSetter));
        }

        internal static GenericSetter CreateSetMethod(Type type, PropertyInfo propertyInfo)
        {
            var setMethod = propertyInfo.GetSetMethod();
            if (setMethod == null)
                return null;

            var arguments = new Type[2];
            arguments[0] = arguments[1] = typeof(object);

            var setter = new DynamicMethod("_", typeof(object), arguments);
            var il = setter.GetILGenerator();

            if (!type.IsClass) // structs
            {
                var lv = il.DeclareLocal(type); 
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Unbox_Any, type);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloca_S, lv);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(propertyInfo.PropertyType.IsClass ? OpCodes.Castclass : OpCodes.Unbox_Any, propertyInfo.PropertyType);
                il.EmitCall(OpCodes.Call, setMethod, null);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Box, type);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(propertyInfo.PropertyType.IsClass ? OpCodes.Castclass : OpCodes.Unbox_Any, propertyInfo.PropertyType);
                il.EmitCall(OpCodes.Callvirt, setMethod, null);
                il.Emit(OpCodes.Ldarg_0);
            }

            il.Emit(OpCodes.Ret);

            return (GenericSetter)setter.CreateDelegate(typeof(GenericSetter));
        }

        internal static GenericGetter CreateGetField(Type type, FieldInfo fieldInfo)
        {
            var dynamicGet = new DynamicMethod("_", typeof(object), new[] { typeof(object) }, type, true);
            var il = dynamicGet.GetILGenerator();

            if (!type.IsClass) // structs
            {
                var lv = il.DeclareLocal(type);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Unbox_Any, type);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloca_S, lv);
                il.Emit(OpCodes.Ldfld, fieldInfo);
                if (fieldInfo.FieldType.IsValueType)
                    il.Emit(OpCodes.Box, fieldInfo.FieldType);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fieldInfo);
                if (fieldInfo.FieldType.IsValueType)
                    il.Emit(OpCodes.Box, fieldInfo.FieldType);
            }

            il.Emit(OpCodes.Ret);

            return (GenericGetter)dynamicGet.CreateDelegate(typeof(GenericGetter));
        }

        internal static GenericGetter CreateGetMethod(Type type, PropertyInfo propertyInfo)
        {
            var getMethod = propertyInfo.GetGetMethod();
            if (getMethod == null)
                return null;

            var arguments = new Type[1];
            arguments[0] = typeof(object);

            var getter = new DynamicMethod("_", typeof(object), arguments, type);
            var il = getter.GetILGenerator();
            
            if (!type.IsClass) // structs
            {
                var lv = il.DeclareLocal(type);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Unbox_Any, type);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloca_S, lv);
                il.EmitCall(OpCodes.Call, getMethod, null);
                if (propertyInfo.PropertyType.IsValueType)
                    il.Emit(OpCodes.Box, propertyInfo.PropertyType);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
                il.EmitCall(OpCodes.Callvirt, getMethod, null);
                if (propertyInfo.PropertyType.IsValueType)
                    il.Emit(OpCodes.Box, propertyInfo.PropertyType);
            }

            il.Emit(OpCodes.Ret);

            return (GenericGetter)getter.CreateDelegate(typeof(GenericGetter));
        }

        internal List<Getters> GetGetters(Type type)
        {
            List<Getters> val;
            if (_getterscache.TryGetValue(type, out val))
                return val;

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var getters = new List<Getters>();
            foreach (var p in props)
            {
                if (!p.CanWrite && ShowReadOnlyProperties == false) continue;
                
                var att = p.GetCustomAttributes(typeof(System.Xml.Serialization.XmlIgnoreAttribute), false);
                if (att.Length > 0)
                    continue;

                var g = CreateGetMethod(type, p);
                if (g == null) continue;
                var gg = new Getters {Name = p.Name, Getter = g, propertyType = p.PropertyType};
                getters.Add(gg);
            }

            var fi = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var f in fi)
            {
                var att = f.GetCustomAttributes(typeof(System.Xml.Serialization.XmlIgnoreAttribute), false);
                if (att.Length > 0)
                    continue;

                var g = CreateGetField(type, f);
                if (g == null) continue;
                var gg = new Getters {Name = f.Name, Getter = g, propertyType = f.FieldType};
                getters.Add(gg);
            }

            _getterscache.Add(type, getters);
            return getters;
        }

        #endregion
    }
}
