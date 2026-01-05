using Haley.Abstractions;
using Haley.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Haley.Utils
{
    public static class JsonExtensions {
      
        public static List<T> FetchNodeValues<T, K>(this List<JsonNode> input, string filter_key, K filter_value, string target_key) {
            var results = new List<T>();

            foreach (var node in input) {
                var filterNode = node?[filter_key];
                //First find the nodes matching the filter
                if (filterNode is JsonValue filterValueNode &&
                    filterValueNode.TryGetValue(out K val) &&
                    EqualityComparer<K>.Default.Equals(val, filter_value)) {
                    //Now fetch the value of the targetKey from the matching nodes.
                    var targetNode = node?[target_key];
                    if (targetNode is JsonValue targetValueNode &&
                        targetValueNode.TryGetValue(out T result)) {
                        results.Add(result);
                    }
                }
            }
            return results;
        }

        // Canonicalize means converting data into a single, standard or preferred format (a 'canonical form') to eliminate variations that do not affect the meaning.
        //Here we sort object properties by key and array items by their serialized string representation.
        public static JsonNode Canonicalize(this JsonNode? node, StringComparer? comparer=null) {
            if (node is null) return JsonValue.Create((string?)null)!;
            comparer ??= StringComparer.OrdinalIgnoreCase;
            if (node is JsonObject o) {
                var sorted = new JsonObject();
                // IMPORTANT: sort by the trimmed key, not the original key
                foreach (var kv in o.Select(kv => new { Key = (kv.Key ?? string.Empty).Trim(), Value = kv.Value })
                .OrderBy(x => x.Key, comparer)) {
                    sorted[kv.Key] = Canonicalize(kv.Value,comparer);
                }
                return sorted;
            }

            if (node is JsonArray a) {
                // NOTE: assumes array order is not semantically important
                var items = a.Select(n=> Canonicalize(n,comparer))
                              //.Select(n => n.ToJsonString(new JsonSerializerOptions { WriteIndented = false }))
                              .Select(n => n.ToJsonString()) // deterministic enough for hashing if we keep same serializer version
                             .OrderBy(s => s, StringComparer.Ordinal)
                             .ToList();

                var arr = new JsonArray();
                foreach (var s in items) {
                    var parsed = JsonNode.Parse(s);
                    if (parsed != null) {
                        arr.Add(parsed);
                    } else {
                        arr.Add(JsonValue.Create((string?)null));
                    }
                }

                return arr;
            }

            // primitives
            return node.DeepClone();
        }

        public static string? ReqString(this JsonElement e, string prop) => GetString(e, prop);

        public static string? GetString(this JsonElement e, string prop) {
            if (!e.TryGetProperty(prop, out var v)) return null;
            return v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString();
        }

        public static int? GetInt(this JsonElement e, string prop) {
            if (!e.TryGetProperty(prop, out var v)) return null;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)) return i;
            if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var j)) return j;
            return null;
        }

        public static bool? GetBool(this JsonElement e, string prop) {
            if (!e.TryGetProperty(prop, out var v)) return null;
            if (v.ValueKind == JsonValueKind.True) return true;
            if (v.ValueKind == JsonValueKind.False) return false;
            if (v.ValueKind == JsonValueKind.String && bool.TryParse(v.GetString(), out var b)) return b;
            return null;
        }
    }
}
