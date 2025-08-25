using Haley.Abstractions;
using Haley.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        
    }
}
