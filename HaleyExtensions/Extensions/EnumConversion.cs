using Haley.Models;
using System;
using System.ComponentModel;
using System.Reflection;

namespace Haley.Utils
{
    public static class EnumConversion
    {
        public static string GetKey(this Enum @enum)
        {
            try
            {
                string enum_type_name = @enum.GetType().ToString();
                string enum_value_name = @enum.ToString();
                string enum_key = enum_type_name + "###" + enum_value_name; //Concatenated value for storing as key
                return enum_key;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Description Attribute handling
        public static string GetDescription(this Enum @enum) {
            FieldInfo fi = @enum.GetType().GetField(@enum.ToString());
            var attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length == 0 ? @enum.ToString() : ((DescriptionAttribute)attributes[0]).Description;
        }

        /// <summary>
        /// Get enum value from description
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="description"></param>
        /// <returns></returns>
        public static T GetValueFromDescription<T>(this string description) where T : Enum {
            foreach (var field in typeof(T).GetFields()) {
                if (Attribute.GetCustomAttribute(field,
                typeof(DescriptionAttribute)) is DescriptionAttribute attribute) {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                } else {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }

            return default(T);
        }

        #endregion

        #region DescriptionNameValue Attribute
        public static (string key, string value) GetDescriptionNameValue(this Enum @enum) {
            FieldInfo fi = @enum.GetType().GetField(@enum.ToString());
            var attributes = fi.GetCustomAttributes(typeof(DescriptionNameValueAttribute), false);
            //DescriptionNamevalue doesn't allow multiple usage, so we will have only one value.
            var attribute = ((DescriptionNameValueAttribute)attributes[0]);
            return attributes.Length == 0 ? (null, @enum.ToString()) : (attribute.Key, attribute.Value);
        }

        /// <summary>
        /// Get enum value from description
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="description"></param>
        /// <returns></returns>
        public static T GetValueFromDescriptionKey<T>(this string key) where T : Enum {
            foreach (var field in typeof(T).GetFields()) {
                if (Attribute.GetCustomAttribute(field,
                typeof(DescriptionNameValueAttribute)) is DescriptionNameValueAttribute attribute) {
                    if (attribute.Key == key)
                        return (T)field.GetValue(null);
                } else {
                    if (field.Name == key)
                        return (T)field.GetValue(null);
                }
            }

            return default(T);
        }
        #endregion
    }
}