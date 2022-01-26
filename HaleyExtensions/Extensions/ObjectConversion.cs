﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.ComponentModel;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml;
using System.Diagnostics;
using System.Text;
using System.IO;

namespace Haley.Utils
{
    public static class ObjectConversion
    {
        #region Conversions
        public static string AsString(this object value)
        {
            if (value == null) return null;
            Type InputType = value.GetType();
            return (Convert.ChangeType(value, value.GetType()))?.ToString() ?? null;
        }
        public static T ChangeType<T>(this object value)
        {
            if (value == null) return default(T); //because the input value is null, we cannot change any type.

            return (T)ChangeType(value, typeof(T));
        }

        public static object ChangeType(this object value, Type targetType)
        {
            if (targetType == typeof(string)) return (string)value;
            if (targetType == typeof(int)) return int.Parse((string)value);
            if (targetType == typeof(double)) return double.Parse((string)value);
            if (targetType == typeof(bool)) return bool.Parse((string)value);
            if (targetType.BaseType == typeof(Enum)) return value;
            if (targetType.IsList() || targetType.IsArray) return _changeCollectionType(value, targetType);
            return value; 
            //return _convertReflected(value,targetType); //Will enter into circular loop as we try to call the same type again and again
        }
       
        #endregion

        #region Helpers
        private static object _changeCollectionType(object value, Type contract_collections_type)
        {
            if (!value.IsList()) return value; //It is expected that the incoming object should be of List<object>

            //Convert object to List of object
            List<object> concrete_instances = value as List<object>;
            if (concrete_instances == null) return value;

            Type contract_type = null;
            if (contract_collections_type.IsList())
            {
                contract_type = contract_collections_type.GenericTypeArguments.First();
            }
            else
            {
                contract_type = contract_collections_type.GetElementType();
            }

            if (contract_type == null) throw new ArgumentException($@"The generic arugment for the {contract_collections_type} is null");

            if (contract_collections_type.IsList())
            {
                return _convertList(concrete_instances, contract_type);
            }
            else //Then it should be an array
            {
                return _convertList(concrete_instances, contract_type, true);
            }
        }
        private static object _convertList(List<object> instances, Type contract_type, bool is_array = false)
        {
            if (contract_type == null) throw new ArgumentException("Contract type cannot be empty. Unable to convert to list.");
            var enumerable_type = typeof(Enumerable);
            var cast_method = enumerable_type.GetMethod(nameof(Enumerable.Cast)).MakeGenericMethod(contract_type);
            MethodInfo conversion_method = null;
            if (is_array)
            {
                conversion_method = enumerable_type.GetMethod(nameof(Enumerable.ToArray)).MakeGenericMethod(contract_type);
            }
            else
            {
                conversion_method = enumerable_type.GetMethod(nameof(Enumerable.ToList)).MakeGenericMethod(contract_type);
            }

            IEnumerable<object> items_to_cast;
            List<object> converted_input = new List<object>();

            foreach (var item in instances)
            {
                var _item = Convert.ChangeType(item, item.GetType()); //Concrete conversion used here.
                converted_input.Add(_item);
            }
            items_to_cast = converted_input;

            var casted_objects = cast_method.Invoke(null, new[] { items_to_cast });
            var _result = conversion_method.Invoke(null, new[] { casted_objects });
            return _result;
        }
        private static object _convertReflected(object value, Type contract_type)
        {
            try
            {
                //WE CANNOT USE BELOW METHOD BECAUSE WE HAVE MORE THAN ONE METHOD WITH NAME "CHANGE TYPE".
                //var methodInfo = typeof(ObjectConversion).GetMethod(nameof(ChangeType), BindingFlags.Static | BindingFlags.Public);

                //USE BELOW METHOD TO GET A SINGLE METHOD.
                var methodInfo = typeof(ObjectConversion).GetMethods().Single(
            p =>
                p.Name == "ChangeType" &&
                p.GetGenericArguments().Length == 1 && //Should have atleast one generic argument length
                p.GetParameters().Length == 1);  //Should have atleast one parameter
                //&& p.GetParameters()[0].ParameterType == typeof(object)); //if parameter type also needs to be filtered?

                var genericArguments = new[] { contract_type };
               
                var genericMethodInfo = methodInfo?.MakeGenericMethod(genericArguments);
                return genericMethodInfo?.Invoke(null, new[] { value });
            }
            catch (Exception ex)
            {
                return value;
            }
        }
        #endregion
    }
}