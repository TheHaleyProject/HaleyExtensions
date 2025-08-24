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

        public static ICompositeObj<T> BuildTree<T>(this ICompositeObj<T> source) where T: ICompositeObj<T> {
            var lookup = source.Children.ToDictionary(p => p.Id); //Get all the children with their ID.
            foreach (var node in source.Children) {
                if (node.ParentId > 0 && lookup.TryGetValue(node.ParentId, out var parent)) {
                    if (parent.Children.Any(q => q.Id == node.Id)) continue; //It already contains a similar children with the same code.
                    parent.Children.Add(node);
                }
            }
            source.Children.RemoveAll(p => p.ParentId > 1); //Remove the child items from the flat list
            return source;
        }


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
