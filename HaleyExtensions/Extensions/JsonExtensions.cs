using Haley.Abstractions;
using Haley.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Haley.Utils {
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
        public static JsonNode Canonicalize(this JsonNode? node, StringComparer? comparer = null) {
            if (node is null) return JsonValue.Create((string?)null)!;
            comparer ??= StringComparer.OrdinalIgnoreCase;
            if (node is JsonObject o) {
                var sorted = new JsonObject();
                // IMPORTANT: sort by the trimmed key, not the original key
                foreach (var kv in o.Select(kv => new { Key = (kv.Key ?? string.Empty).Trim(), Value = kv.Value })
                .OrderBy(x => x.Key, comparer)) {
                    sorted[kv.Key] = Canonicalize(kv.Value, comparer);
                }
                return sorted;
            }

            if (node is JsonArray a) {
                // NOTE: assumes array order is not semantically important
                var items = a.Select(n => Canonicalize(n, comparer))
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

        public static string? GetString(this JsonElement e, string prop) {
            if (!e.TryGetProperty(prop, out var v)) return null;
            return v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString();
        }

        public static int? GetInt(this JsonElement e, string prop) {
            if (!e.TryGetProperty(prop, out var v)) return null;
            return v.GetInt();
        }

        public static int? GetInt(this JsonElement e) {
            if (e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var i)) return i;
            if (e.ValueKind == JsonValueKind.String && int.TryParse(e.GetString(), out var j)) return j;
            return null;
        }

        public static bool? GetBool(this JsonElement e, string prop) {
            if (!e.TryGetProperty(prop, out var v)) return null;
            return v.GetBool();
        }

        public static bool? GetBool(this JsonElement e) {
            if (e.ValueKind == JsonValueKind.True) return true;
            if (e.ValueKind == JsonValueKind.False) return false;
            if (e.ValueKind == JsonValueKind.String && bool.TryParse(e.GetString(), out var b)) return b;
            return null;
        }

        public static DateTimeOffset? GetDatetimeOffset(this JsonElement obj, string prop) {
            if (!obj.TryGetProperty(prop, out var el) || el.ValueKind != JsonValueKind.String) return null;
            return el.GetDatetimeOffset();
        }

        public static DateTimeOffset? GetDatetimeOffset(this JsonElement el) {
            return DateTimeOffset.TryParse(el.GetString(), out var dt) ? dt : null;
        }

        public static IReadOnlyDictionary<string, object?>? GetDictionary(this JsonElement obj, string prop) {
            if (string.IsNullOrWhiteSpace(prop) || !obj.TryGetProperty(prop, out var pEl) || pEl.ValueKind == JsonValueKind.Null || pEl.ValueKind == JsonValueKind.Undefined) return null;
            return pEl.GetDictionary();
        }

        public static IReadOnlyDictionary<string, object?> GetDictionary(this JsonElement el) {
            if (el.ValueKind == JsonValueKind.Object) {
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in el.EnumerateObject()) dict[p.Name] = p.Value.GetObject();
                return dict;
            }
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["value"] = el.GetObject() };
        }

        public static object? GetObject(this JsonElement el) {
            switch (el.ValueKind) {
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                return null;
                case JsonValueKind.String:
                return el.GetString();
                case JsonValueKind.Number:
                if (el.TryGetInt64(out var l)) return l;
                if (el.TryGetDouble(out var d)) return d;
                return el.ToString();
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var x in el.EnumerateArray()) list.Add(x.GetObject()); //Recursive call
                return list;
                case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in el.EnumerateObject()) dict[p.Name] = p.Value.GetObject();
                return dict;
                default: return el.ToString();
            }
        }
    }
}
