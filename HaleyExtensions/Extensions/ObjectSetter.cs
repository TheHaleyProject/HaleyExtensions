using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Data.SqlTypes;

namespace Haley.Utils
{
    public static class ObjectSetter {

        public static void SetProperty(this object input, string name,object value, StringComparison comparison = StringComparison.OrdinalIgnoreCase) {
            input?.SetProperty(name,value,null,comparison);
        }
        public static void SetProperty(this object input, string name, object value, Func<object,bool> preValidator, StringComparison comparison = StringComparison.OrdinalIgnoreCase) {
            try {
                var inputType = input.GetType();
                var propertyInfo = inputType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Single(p => p.Name.Equals(name,comparison));
                propertyInfo?.SetProperty(input,value);

                //Below method will directly invoke the member (if available) and set it.
              //  inputType.InvokeMember("UniqueId",
              //BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
              //Type.DefaultBinder, input, new object[] { Guid.NewGuid() });

            } catch (Exception) {
                throw;
            }
        }

      
        public static void SetProperty(this PropertyInfo propInfo, object input, object value) {
            propInfo?.SetProperty(input,value, null);
        }
        public static void SetProperty(this PropertyInfo propInfo, object input, object value, Func<object, bool> preValidator) {
            if (propInfo.CanWrite == false) throw new ArgumentException($@"Cannot write the property {propInfo.Name} in {input.GetType().FullName}");
            if (preValidator != null) {
                var currentvalue = propInfo.GetValue(input);
                if (!preValidator.Invoke(currentvalue)) return; // user valiated this not to be set.
            }
            propInfo.SetValue(input, value, null); //Directly set the value.
        }
    }
 }
