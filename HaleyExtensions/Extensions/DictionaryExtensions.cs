using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json.Nodes;

namespace Haley.Utils
{
    public static class DictionaryExtensions {
        /// <summary>
        /// Builds a URL address from the given components 
        /// </summary>
        /// <param name="dic">keys: base, route, suffix</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string GenerateBaseURLAddress(this Dictionary<string, object> dic) {
            if (dic == null) throw new ArgumentNullException("Dictionary cannot be null");
            if (!dic.TryGetValue("base", out var baseObj) || baseObj == null) throw new ArgumentNullException("Key base= is required to define the base address");
            dic.TryGetValue("route", out var routeObj);
            dic.TryGetValue("suffix", out var suffixObj);
            string baseNew = baseObj.ToString();
            if (routeObj != null && !string.IsNullOrWhiteSpace(routeObj.ToString())) baseNew += routeObj.ToString();
            if (suffixObj != null && !string.IsNullOrWhiteSpace(suffixObj.ToString())) baseNew += suffixObj.ToString();
            return baseNew;
        }

        public static string Join(this IDictionary<string, object> dic, char delimiter = '&') {
            if (dic == null || dic.Count == 0) return string.Empty;
            return string.Join(delimiter.ToString(), dic.Where(p => !string.IsNullOrWhiteSpace(p.Key)).Select(q => $@"{q.Key}={q.Value}"));
        }

        public static bool ContainsAllEntries<TKey, TValue>(this IDictionary<TKey, TValue> superset, IDictionary<TKey, TValue> subset) {
            return ContainsAllEntries(superset, subset, null);
        }

        public static bool ContainsAllEntries<TKey, TValue>(this IDictionary<TKey, TValue> superset, IDictionary<TKey, TValue> subset, Dictionary<TKey, TKey> keyMapping) {
            foreach (var kvp in subset) {
                var key = kvp.Key;
                if (keyMapping != null && keyMapping.ContainsKey(key)) {
                    key = keyMapping[key]; //Get the key from the mapping.
                }
                if (!superset.TryGetValue(key, out var value) || !EqualityComparer<TValue>.Default.Equals(value, kvp.Value)) {
                    return false;
                }
            }
            return true;
        }

        public static int GetInt(this IDictionary<string, object> row, string key) {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (!row.TryGetValue(key, out var value) || value == null) return 0;
            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is short s) return s;
            if (int.TryParse(Convert.ToString(value), out var parsed)) return parsed;
            return 0;
        }

        public static long GetLong(this IDictionary<string, object> row, string key) {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (!row.TryGetValue(key, out var value) || value == null) return 0L;
            if (value is long l) return l;
            if (value is int i) return i;
            if (long.TryParse(Convert.ToString(value), out var parsed)) return parsed;
            return 0L;
        }

        public static string GetString(this IDictionary<string, object> row, string key) {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (!row.TryGetValue(key, out var value) || value == null) return null;
            return Convert.ToString(value);
        }

        public static Guid? GetGuid(this IDictionary<string, object> row, string key) {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (!row.TryGetValue(key, out var value) || value == null || value == DBNull.Value) return null;
            if (value is Guid g) return g;
            if (value is string s && Guid.TryParse(s, out var gs)) return gs;
            if (Guid.TryParse(Convert.ToString(value), out var parsed)) return parsed;
            return null;
        }


        public static int? GetNullableInt(this IDictionary<string, object> row, string key) {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (!row.TryGetValue(key, out var value) || value == null || value == DBNull.Value) return null;
            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is short s) return s;
            if (int.TryParse(Convert.ToString(value), out var parsed)) return parsed;
            return null;
        }

        public static long? GetNullableLong(this IDictionary<string, object> row, string key) {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (!row.TryGetValue(key, out var value) || value == null || value == DBNull.Value) return null;
            if (value is long l) return l;
            if (value is int i) return i;
            if (long.TryParse(Convert.ToString(value), out var parsed)) return parsed;
            return null;
        }

        public static DateTime? GetDateTime(this IDictionary<string, object> row, string key) {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (!row.TryGetValue(key, out var value) || value == null || value == DBNull.Value) return null;
            if (value is DateTime dt) return dt;
            if (value is DateTimeOffset dto) return dto.UtcDateTime;
            if (DateTime.TryParse(Convert.ToString(value), out var parsed)) return parsed;
            return null;
        }

        public static DateTimeOffset? GetDateTimeOffset(this IDictionary<string, object> row, string key) {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (!row.TryGetValue(key, out var value) || value == null || value == DBNull.Value) return null;
            if (value is DateTimeOffset dto) return dto;
            if (value is DateTime dt) return new DateTimeOffset(dt);
            if (DateTimeOffset.TryParse(Convert.ToString(value), out var parsed)) return parsed;
            return null;
        }

        public static bool GetBool(this IDictionary<string, object> row, string key) {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (!row.TryGetValue(key, out var value) || value == null || value == DBNull.Value) return false;
            if (value is bool b) return b;
            if (value is sbyte sb) return sb != 0;
            if (value is byte by) return by != 0;
            if (value is short s) return s != 0;
            if (value is int i) return i != 0;
            if (value is long l) return l != 0;
            var str = Convert.ToString(value);
            if (string.IsNullOrWhiteSpace(str)) return false;
            if (bool.TryParse(str, out var pb)) return pb;
            if (int.TryParse(str, out var pi)) return pi != 0;
            return false;
        }
    }
}