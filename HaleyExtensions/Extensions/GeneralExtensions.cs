using Haley.Abstractions;
using Haley.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Haley.Utils
{
    public static class GeneralExtensions {
       public static void Throw(this IFeedback input) {
            if (input.Status) return;
            throw new ArgumentException($@"Fail: {input.Message}");
        }

        public static string ToYesNo(this bool? input) {
            if (!input.HasValue) return "None";
            return ToYesNo(input.Value);
        }

        public static string ToYesNo(this bool input) {
            return input ? "Yes" : "No";
        }

        public static bool ContainsAllEntries<TKey, TValue>(this IDictionary<TKey, TValue> superset, IDictionary<TKey, TValue> subset) {
           return ContainsAllEntries(superset, subset, null);
        }

        public static bool ContainsAllEntries<TKey, TValue>(this IDictionary<TKey, TValue> superset, IDictionary<TKey, TValue> subset,Dictionary<TKey, TKey> keyMapping) {
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


        public static IEnumerable<object> GetPropAtLevels<T>(this List<T> roots, int[] targetLevels, Func<T,int, object> dataProcesor) where T : ICompositeObj<T> {
            return GetPropRecursive(roots, 1, targetLevels.Max(), targetLevels, dataProcesor)
                //.Where(x => targetLevels.Contains(x.level))
                .Select(x => x.data);
        }

        static IEnumerable<(object data, int level)> GetPropRecursive<T>(IEnumerable<T> nodes, int level, int stoplevel, int[] targetLevels, Func<T,int, object> dataProcesor) where T : ICompositeObj<T> {
            if (level > stoplevel) {
                yield break; // Stop recursion if the current level exceeds the stopping level
            }

            foreach (var node in nodes) {
                if (targetLevels.Contains(level)) {
                    var data = dataProcesor(node, level);
                    if (data != null) {
                        yield return (data, level); //Yield only for the target levels and not for everything else
                    }
                }

                if (node.Children?.Any() == true) {
                    foreach (var childresult in GetPropRecursive(node.Children, level + 1, stoplevel,targetLevels, dataProcesor))
                        yield return childresult;
                }
            }
        }
    }
}
