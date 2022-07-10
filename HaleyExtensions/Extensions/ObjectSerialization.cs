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

namespace Haley.Utils
{
    public static class ObjectSerialization
    {
        private static JsonSerializerOptions commonOptions = new JsonSerializerOptions() 
        { 
            WriteIndented = true ,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };

        private static void EnsureDefaultJsonConverters(ref JsonSerializerOptions options)
        {
            var hasEnumConverter = options.Converters.Any(p => p is JsonStringEnumConverter);
            if (!hasEnumConverter)
            {
                try
                {
                    options.Converters.Add(new JsonStringEnumConverter());
                }
                catch (Exception)
                {
                }
            }
        }

        public static XElement ToXml(this object source)
        {
            Type _type = source.GetType();

            #region Abandoned - To Consider interfaces
            ////If the source has any Interface properties, we just get them as extratypes.
            //Type[] extraTypes = _type.GetProperties()
            //    .Where(p => p.PropertyType.IsInterface)
            //    .Select(p => p.GetValue(source, null).GetType())
            //    .ToArray();

            //DataContractSerializer serializer = new DataContractSerializer(_type, extraTypes);
            //serializer.WriteObject(xw, source);
            #endregion
            XmlSerializer serializer = new XmlSerializer(_type); //New serializer for the given type.
            //TO IGNORE UNWANTED NAMESPACES
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);
            serializer.Serialize(xw, source, ns);
            return XElement.Parse(sw.ToString());
        }

        public static T XmlDeserialize<T>(this string input)
        {
            Type _type = typeof(T);
            return (T)XmlDeserialize(input,_type);
        }

        public static object XmlDeserialize(this string input,Type targetType)
        {
            XmlSerializer serializer = new XmlSerializer(targetType); //New serializer for the given type.
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            StringReader rdr = new StringReader(input);
            return serializer.Deserialize(rdr);
        }

        public static T JsonDeserialize<T>(this string input)
        {
            return (T)JsonDeserializeInternal(input, commonOptions,typeof(T));
        }

        public static object JsonDeserialize(this string input,Type targetType)
        {
            return JsonDeserializeInternal(input, commonOptions,targetType);
        }
        public static T JsonDeserialize<T>(this string input, JsonSerializerOptions options)
        {
            return (T)JsonDeserializeInternal(input, options,typeof(T));
        }

        public static object JsonDeserialize(this string input, JsonSerializerOptions options,Type targetType)
        {
            return JsonDeserializeInternal(input, options,targetType);
        }

        private static object JsonDeserializeInternal(string input,JsonSerializerOptions option,Type targetType)
        {
            EnsureDefaultJsonConverters(ref option);
            if (targetType == null) throw new ArgumentException("Targettype cannot be null");
            return JsonSerializer.Deserialize(input, targetType, options: option);
        }

        public static string ToJson(this object source,JsonSerializerOptions options)
        {
            return ToJsonInternal(source, ref options, null);
        }
        private static string ToJsonInternal(object source, ref JsonSerializerOptions options, List<JsonConverter> converters = null)
        {
            try {
                EnsureDefaultJsonConverters(ref options);

                do {
                    if (converters == null || converters?.Count == 0) break;

                    foreach (var item in converters) {
                        try {
                            options.Converters.Add(item);
                        }
                        catch (Exception) {
                            continue;
                        }
                    }
                } while (false);
                
                return JsonSerializer.Serialize(source, source.GetType(), options);
            }
            catch (Exception ex) {
                return ex.ToString();
            }
        }
        public static string ToJson(this object source,List<JsonConverter> converters = null)
        {
            return ToJsonInternal(source,ref commonOptions, converters);
        }
        public static string BinarySerialize(this object input)
        {
            string result = null;
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, input);
                    result = Convert.ToBase64String(ms.ToArray());
                }
                return result;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static object BinaryDeserialize(this string input)
        {
            object result = null;
            try
            {
                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(input)))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    result = bf.Deserialize(ms);
                }
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static T BinaryDeserialize<T>(this string input)
        {
            return  (T)input.BinaryDeserialize();
        }
        
        public static string ProtoSerialize(this object input)
        {
            string result = null;
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize(ms, input);
                    result = Convert.ToBase64String(ms.ToArray());
                }
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static T ProtoDeserialize<T>(this string input)
        {
            return (T)ProtoDeserialize(input, typeof(T));
        }

        public static object ProtoDeserialize(this string input,Type targetType)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(input)))
                {
                    return Serializer.Deserialize(targetType,ms);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
 }
