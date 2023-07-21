using Haley.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Haley.Enums;
using System.Text.Json.Nodes;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Diagnostics.SymbolStore;

namespace Haley.Utils
{
    public static class JsonNodeMapping {

        public static IEnumerable<TTarget> MapArray<TTarget>(this JsonNode source, MappingInfo mapping_info = default(MappingInfo),FlattenInfo flatten_info = default(FlattenInfo)) where TTarget : class, new() {

           return source.MapArray(typeof(TTarget), mapping_info, flatten_info)?.ChangeEnumerableType(typeof(TTarget)) as IEnumerable<TTarget>;
        }
        public static IEnumerable<object> MapArray(this JsonNode source, Type target_type, MappingInfo mapping_info = default(MappingInfo), FlattenInfo flatten_info = default(FlattenInfo)) {

            if (source == null || !(source is JsonArray)) return null;
            JsonArray jarr = source.AsArray();
            //If TT is a direct type, get it, else, get the generic argument type
            List<object> _targets = new List<object>();
            foreach (var item in jarr) {
                var target = Activator.CreateInstance(target_type); 
                if (target.IsList()) throw new ArgumentException("For mapping an array to an enumerable, the target type should be the generic argument type and not a type of list.");
                item.Map(ref target, mapping_info, flatten_info);
                _targets.Add(target);
            }
            return _targets;
        }

        public static TTarget Map<TTarget>(this JsonNode source, MappingInfo mapping_info = default(MappingInfo), FlattenInfo flatten_info = default(FlattenInfo))
            where TTarget : class, new() {
            var result = source.Map(typeof(TTarget), mapping_info, flatten_info);
            //if incoming expectationg is a list. change the type.
            if (typeof(TTarget).IsList()) {
                //expectation is a list output
                return result.ChangeType<TTarget>();
            }
            return  result as TTarget;
        }

        public static object Map(this JsonNode source, Type target_type, MappingInfo mapping_info = default(MappingInfo), FlattenInfo flatten_info = default(FlattenInfo)) {
            object result = Activator.CreateInstance(target_type);
            source.Map(ref result, mapping_info, flatten_info);
            return result;
        }

        public static void Map(this JsonNode source, ref object target, MappingInfo mapping_info = default(MappingInfo), FlattenInfo flatten_info = default(FlattenInfo)) {

            if (source == null || target == null) return;
            if (source is JsonArray) {

                Type target_type = target.GetType();
                if (target.IsList()) {
                    target_type = target.GetType().GetGenericArguments()[0];
                }
                //If source is array, then expected return object should be a list.
                target = source.AsArray()?.MapArray(target_type, mapping_info, flatten_info); //Only for direct call by clients
                return;
            } 

            //single node will fail.. source (Jsonnode) has to be jsonobject (like an array), if source is a jsonvalue,then "AsObject" will throw exception
            List<string> mapped_keys = new List<string>();
            var dic = PopulateValueDictionary(source, ref mapped_keys, 0, flatten_info); //populate as is based on flatten info
            if (dic == null) return;
            ObjectMapping.Map(dic, ref target, mapping_info);
        }

        public static string GetNodePath(this JsonNode source) {
            try {
                var key = source.GetPath();
                if (key.StartsWith("$")) {
                    key = key.Split('.').Last();
                }
                return key;
            } catch (Exception) {
                return source.GetPath();
            }
        }

        public static (string key,object value) GetNodeKVP(this JsonValue source) {
            try {
                return (source.GetNodePath(), source.GetValue<object>() as object);
            } catch (Exception) {
                return (string.Empty, null);
            }
        }

        private static void FillJsonValue(JsonValue source, ref List<string> mapped_keys, ref Dictionary<string,object> result) {
            try {
                if (source == null) return;
                var kvp = source.GetNodeKVP();
                if (string.IsNullOrWhiteSpace(kvp.key)) return;
                if (!mapped_keys.Contains(kvp.key) && !result.ContainsKey(kvp.key)) {
                    result.Add(kvp.key, kvp.value);
                    mapped_keys.Add(kvp.key);
                }
            } catch (Exception) {
                    throw;
            }
        }

        private static Dictionary<string,object> PopulateValueDictionary(JsonNode source, ref List<string> mapped_keys, int current_level,  FlattenInfo flatten_info) {

            if (source == null) return null;
            Dictionary<string, object> result = new Dictionary<string, object>();

            if (source is JsonValue jvalue) {
                FillJsonValue(jvalue, ref mapped_keys, ref result);
                return result;
            } else if (source is JsonArray array) {
                return null;
                //throw new NotImplementedException("Input is of type json array, which will provide an ienumerable of results. Convert the input to JsonArray and then try to map. Or try to pass in ref object to fetch the result");
            } else if (source is JsonObject) {
                //Validate child nodes.
                if (current_level != 0) {
                    if (flatten_info.Mode == FlattenMode.SelectedNodes) {
                        //1. Child node path  is null or with no values
                        //2. Child node paths doesn't contain this node name.
                        if (flatten_info.ChildNodePaths == null || flatten_info.ChildNodePaths.Count  < 1 || !flatten_info.ChildNodePaths.Any(p => p.Equals(source.GetNodePath()))) return null;
                    } 
                }

                if (current_level > flatten_info.Level) return null; //level constraint only for child nodes.

                var jobject = source.AsObject();
                if (jobject == null) return null;

                //Map CURRENT LEVEL VALUES first to give priority
                foreach (var item in jobject.Where(p => p.Value is JsonValue)) {
                    FillJsonValue(item.Value as JsonValue, ref mapped_keys, ref result);
                }

                //Map OTHER LEVEL VALUES (VIA CHILD NODE) 
                foreach (var item in jobject.Where(p=> !(p.Value is JsonValue))) {
                    //recursive for each node (which could be a value or array or object
                    var childdic = PopulateValueDictionary(item.Value, ref mapped_keys, current_level + 1, flatten_info);
                    if (childdic != null && childdic.Count > 0) {

                        foreach (var kvp in childdic) {
                            if (!result.ContainsKey(kvp.Key)) {
                                result.Add(kvp.Key, kvp.Value);
                            }
                        }
                    }
                }
            }
            return result; 
        }
    }
}