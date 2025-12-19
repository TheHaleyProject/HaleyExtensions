using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Haley.Utils
{
    public static class ObjectSerialization {
        #region Helpers
        private static JsonSerializerOptions commonOptions = GenerateNewOptions(true);
        public static JsonSerializerOptions GenerateNewOptions(bool include_default_converters = false) {
            var result = new JsonSerializerOptions() {
                WriteIndented = true,
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                IgnoreReadOnlyFields = false,
                IgnoreReadOnlyProperties = false,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping //to allow & to be saved as & and not in unicode format.
            };
            if (include_default_converters) EnsureDefaultJsonConverters(ref result);
            return result;
        }
        internal static JsonSerializerOptions GetOptions(bool generateNew = false) {
            if (generateNew) {
                return GenerateNewOptions(true);
            }
            return commonOptions;
        }

        internal static JsonSerializerOptions GetOptionsWithOverride(bool generateNew = false, bool? caseInSensitive = null, bool? ignoreReadOnlyElements = null, bool? writeIntended = null) {

            if (caseInSensitive == null && ignoreReadOnlyElements == null && writeIntended == null && !generateNew) return commonOptions; //Return the common static options.

            var options = GetOptions(true);
            if (caseInSensitive != null) options.PropertyNameCaseInsensitive = caseInSensitive.Value;
            if (writeIntended != null) options.WriteIndented = writeIntended.Value;
            if (ignoreReadOnlyElements != null) {
                options.IgnoreReadOnlyFields = ignoreReadOnlyElements.Value;
                options.IgnoreReadOnlyProperties = ignoreReadOnlyElements.Value;
            }
            return options;
        }

        private static void EnsureDefaultJsonConverters(ref JsonSerializerOptions options) {
            var hasEnumConverter = options.Converters.Any(p => p is JsonStringEnumConverter);
            if (!hasEnumConverter) {
                try {
                    options.Converters.Add(new JsonStringEnumConverter());
                } catch (Exception) {
                }
            }
        }
        #endregion

        #region XML / JSON Serialization
        public static XElement ToXml(this object source) {
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

        public static T FromXml<T>(this string input) {
            Type _type = typeof(T);
            return (T)FromXml(input, _type);
        }

        public static object FromXml(this string input, Type targetType) {
            XmlSerializer serializer = new XmlSerializer(targetType); //New serializer for the given type.
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            StringReader rdr = new StringReader(input);
            return serializer.Deserialize(rdr);
        }

        public static T FromJson<T>(this string input, bool? caseInSensitive = null, bool? ignoreReadOnlyElements = null) {
            var options = GetOptionsWithOverride(false, caseInSensitive, ignoreReadOnlyElements);
            return FromJsonInternal<T>(input, options);
        }

        public static T FromJson<T>(this string input, JsonSerializerOptions options) {
            return FromJsonInternal<T>(input, options);
        }

        public static object FromJson(this string input, Type targetType, bool? caseInSensitive = null, bool? ignoreReadOnlyElements = null) {
            var options = GetOptionsWithOverride(false, caseInSensitive, ignoreReadOnlyElements);
            return FromJsonInternal(input, options, targetType);
        }

        public static object FromJson(this string input, JsonSerializerOptions options, Type targetType) {
            return FromJsonInternal(input, options, targetType);
        }

        private static object FromJsonInternal(string input, JsonSerializerOptions option, Type targetType) {
            if (targetType == null) throw new ArgumentException("Targettype cannot be null");
            return JsonSerializer.Deserialize(input, targetType, options: option);
        }
        private static T FromJsonInternal<T>(string input, JsonSerializerOptions option) {
            return JsonSerializer.Deserialize<T>(input, options: option);
        }

        public static string ToJson(this object source, bool generateNew = false, bool? caseInSensitive = null, bool? ignoreReadOnlyElements = null, bool? writeIntended = null) {
            var options = GetOptionsWithOverride(generateNew, caseInSensitive, ignoreReadOnlyElements, writeIntended);
            return ToJsonInternal(source, ref options, null);
        }

        public static string ToJson(this object source, List<JsonConverter> converters, bool generateNew = false, bool? caseInSensitive = null, bool? ignoreReadOnlyElements = null, bool? writeIntended = null) {
            var options = GetOptionsWithOverride(generateNew, caseInSensitive, ignoreReadOnlyElements, writeIntended);
            return ToJsonInternal(source, ref options, converters);
        }

        public static string ToJson(this object source, JsonSerializerOptions options) {
            return ToJsonInternal(source, ref options, null);
        }
        private static string ToJsonInternal(object source, ref JsonSerializerOptions options, List<JsonConverter> converters = null) {
            try {
                try {
                    do {
                        if (converters == null || converters?.Count == 0) break;

                        foreach (var item in converters) {
                            try {
                                options.Converters.Add(item);
                            } catch (Exception) {
                                continue;
                            }
                        }
                    } while (false);
                } catch (Exception) {

                }
                return JsonSerializer.Serialize(source, source.GetType(), options);
            } catch (Exception ex) {
                return ex.ToString();
            }
        }
        #endregion

        public static Dictionary<string,object> AsDictionary(this object source) {
            //If given object is dictionary return as is
            if (source == null) throw new ArgumentNullException(nameof(source));

            if (source is Dictionary<string, object> inputDic) return inputDic;

            var type = source.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var dict = new Dictionary<string, object>();

            foreach (var prop in properties) {
                var value = prop.GetValue(source);
                dict[prop.Name] = value;
            }
            return dict;
        }

        public static IEnumerable<KeyValuePair<string,string>> AsURLEncodedInput(this object source) {
            var result = new List<KeyValuePair<string, string>>();
            var dic = source.AsDictionary();
            foreach (var kvp in dic) {
                if (kvp.Value is IEnumerable<string> stringList) {
                    foreach (var val in stringList) {
                        result.Add(new KeyValuePair<string, string>(kvp.Key, val));
                    }
                } else if (kvp.Value is IEnumerable<object> objList && !(kvp.Value is string)) {
                    foreach (var val in objList) {
                        result.Add(new KeyValuePair<string, string>(kvp.Key, val?.ToString() ?? ""));
                    }
                } else {
                    result.Add(new KeyValuePair<string, string>(kvp.Key, kvp.Value?.ToString() ?? ""));
                }
            }
            return result;
        }
        public static JsonNode AsJsonValue(this object input) {
            return input == null ? null : JsonValue.Create(input);
        }
        public static string AsString(this object value) {
            if (value == null) return null;
            Type InputType = value.GetType();
            return (Convert.ChangeType(value, InputType))?.ToString() ?? null;
        }

        public static T As<T>(this object value) => value.ChangeType<T>();

        public static T DeepClone<T>(this T obj) {
            return obj.ToJson().FromJson<T>();
            //var bytes = JsonSerializer.SerializeToUtf8Bytes(obj, commonOptions);
            //return JsonSerializer.Deserialize<T>(new ReadOnlySpan<byte>(bytes) ,commonOptions);
            //return obj.ProtoSerialize().ProtoDeserialize<T>();
        }

    }
 }
