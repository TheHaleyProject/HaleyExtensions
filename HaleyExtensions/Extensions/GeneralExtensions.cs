using Haley.Abstractions;
using Haley.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Nodes;

namespace Haley.Utils
{
    public static class GeneralExtensions {
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

       


        public static IEnumerable<object> GetContentAtLevels<T>(this List<T> roots, int[] targetLevels, Func<T,int, object> dataProcesor) where T : ICompositeObj<T> {
            int stoplevel = 0;
            if (targetLevels != null && targetLevels.Length > 0) stoplevel = targetLevels.Max();
            return GetContentAtLevels(roots, targetLevels, stoplevel, dataProcesor);
        }

        public static IEnumerable<object> GetContentAtLevels<T>(this List<T> roots, int[] targetLevels,int stoplevel, Func<T, int, object> dataProcesor) where T : ICompositeObj<T> {
            return GetContentRecursive(roots, 1, stoplevel, targetLevels, dataProcesor)
                //.Where(x => targetLevels.Contains(x.level))
                .Select(x => x.data);
        }

        static IEnumerable<(object data, int level)> GetContentRecursive<T>(IEnumerable<T> nodes, int level, int stoplevel, int[] targetLevels, Func<T,int, object> dataProcesor) where T : ICompositeObj<T> {
            if (stoplevel != 0 && level > stoplevel) {
                yield break; // Stop recursion if the current level exceeds the stopping level
            }

            foreach (var node in nodes) {
                if ((targetLevels== null || targetLevels.Count() == 0) || targetLevels.Contains(level)) {
                    var data = dataProcesor(node, level);
                    if (data != null) {
                        yield return (data, level); //Yield only for the target levels and not for everything else
                    }
                }

                if (node.Children?.Any() == true) {
                    foreach (var childresult in GetContentRecursive(node.Children, level + 1, stoplevel,targetLevels, dataProcesor))
                        yield return childresult;
                }
            }
        }
    }
}
