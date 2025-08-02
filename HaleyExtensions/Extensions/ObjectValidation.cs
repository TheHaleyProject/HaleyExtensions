using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Xml.Linq;
using System.Reflection;
using System.Linq;

namespace Haley.Utils
{
    public static class ObjectValidation
    {
        public static bool IsList(this object obj)
        {
            try
            {
                if (obj == null) return false;
                if (obj is Type)
                {
                    Type _type = (Type)obj;
                    if (_type.IsGenericType && _type.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>))) return true;
                }
                else
                {
                    if (obj is IList && obj.GetType().IsGenericType && obj.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>))) return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool IsDictionary(this object obj)
        {
            try
            {
                if (obj == null) return false;
                if (obj is Type)
                {
                    Type _type = (Type)obj;
                    if (_type.IsGenericType && _type.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>))) return true;
                }
                else
                {
                    if (obj is IDictionary && obj.GetType().IsGenericType && obj.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>))) return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool IsEnumerable(this object obj)
        {
            try
            {
                if (obj == null) return false;
                if (obj is Type)
                {
                    Type _type = (Type)obj;
                    if(_type.IsGenericType && _type.GetGenericTypeDefinition().IsAssignableFrom(typeof(IEnumerable<>))) return true;
                }
                else
                {
                    if (obj is IEnumerable && obj.GetType().IsGenericType && obj.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(IEnumerable<>))) return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool IsCollection(this object obj)
        {
            try
            {
                if (obj == null) return false;
                if (obj is Type)
                {
                    Type _type = (Type)obj;
                    if (_type.IsGenericType && _type.GetGenericTypeDefinition().IsAssignableFrom(typeof(ICollection<>))) return true;
                }
                else
                {
                    if (obj is ICollection && obj.GetType().IsGenericType && obj.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(ICollection<>))) return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool IsNumericType(this object obj) {
            if (obj is int || obj is double || obj is float || obj is decimal || obj is long || obj is short || obj is byte || obj is sbyte ||obj is ulong || obj is ushort) {
                return true;
            }
            return false;
        }

        public static bool AssertValue(this object obj, bool throwException = false,string propname = null) {
            var objType = obj?.GetType();
            if (objType != null &&  objType.FullName.StartsWith("System.ValueTuple")) {
                //Let us support only value tuple.
                var fields = objType.GetFields(BindingFlags.Instance | BindingFlags.Public).Where(p => p.Name.StartsWith("Item")).ToArray();
                return fields[0].GetValue(obj).AssertValue(throwException, fields[1].GetValue(obj).ToString());
            }

            if (obj == null || (obj is string obstr && string.IsNullOrWhiteSpace(obstr))) {
                var property = propname ?? "";
                if (throwException) throw new ArgumentNullException($@"Provided input is empty or null. Please check {property}");
                return false;
            }
            return true;
        }

        public static bool AssertValues(bool throwException, params object[] values) {
            foreach (var item in values) {
                if (!item.AssertValue(throwException)) return false;
            }
            return true;
        }
    }
}