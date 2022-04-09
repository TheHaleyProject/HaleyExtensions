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

namespace Haley.Utils
{
    public static class ObjectSerialization
    {
        private static JsonSerializerOptions commonOptions = new JsonSerializerOptions() 
        { 
            WriteIndented = true ,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };

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
            XmlSerializer serializer = new XmlSerializer(_type); //New serializer for the given type.
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            StringReader rdr = new StringReader(input);
            return (T)serializer.Deserialize(rdr);
        }

        public static T JsonDeserialize<T>(this string input)
        {
            return JsonDeserializeInternal<T>(input, commonOptions);
        }
        public static T JsonDeserialize<T>(this string input, JsonSerializerOptions options)
        {
            return JsonDeserializeInternal<T>(input, options);
        }

        private static T JsonDeserializeInternal<T>(string input,JsonSerializerOptions option)
        {
            return (T)JsonSerializer.Deserialize(input, typeof(T), options: option);
        }

        public static string ToJson(this object source,JsonSerializerOptions options)
        {
            return ToJsonInternal(source, ref options, null);
        }
        private static string ToJsonInternal(object source, ref JsonSerializerOptions options, List<JsonConverter> converters = null)
        {
            if (!options.Converters.Contains(new JsonStringEnumConverter()))
            {
                try
                {
                    options.Converters.Add(new JsonStringEnumConverter());
                }
                catch (Exception)
                {
                }
            }

            if (converters != null && converters?.Count > 0)
            {
                foreach (var item in converters)
                {
                    try
                    {
                        options.Converters.Add(item);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
            return JsonSerializer.Serialize(source, source.GetType(), options);
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
            try
            {
                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(input)))
                {
                   return Serializer.Deserialize<T>(ms);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
 }
