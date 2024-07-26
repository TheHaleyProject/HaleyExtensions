using Haley.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Haley.Utils
{
    public static class StringHelpers
    {
        public static CompareStatus? CompareWith(this string source, string target) {
            var _pattern = @"\d+";

            //If either source or target is null, return null. Donot compare.
            if (string.IsNullOrWhiteSpace(source)) return null;
            if (string.IsNullOrWhiteSpace(target)) return CompareStatus.Greater; //If target is null, then we are already greater.

            List<string> _overall = new List<string>() { source, target };

            int max = _overall
                .SelectMany(p =>
                Regex.Matches(p, _pattern) //Matches searches for all occurences in the string and produces as a match collection
                .Cast<Match>()
                .Select(m => (int?)m.Value.Length)) //from the match collection select all the length of values (to find out how many digits we have continuously (lets say we get 1, 0, 9531 for 1.0.9531)
                .Max() ?? 0;
            //The numeric value can be anywhere inside a string. It could be at end or middle or at start.
            //among the collection, find which has maximum length.

            var _source_padded = Regex.Replace(source, _pattern, m => m.Value.PadLeft(max, '0'));
            var _target_padded = Regex.Replace(target, _pattern, m => m.Value.PadLeft(max, '0'));

            switch (_source_padded.CompareTo(_target_padded)) {
                case 0:
                    return CompareStatus.Equal;
                case 1:
                    return CompareStatus.Greater;
                case -1:
                    return CompareStatus.Lesser;
                }

            return null;
            }

        public static string DeSanitizeJWT(this string input) {
            //this cannot be base 64
            if (input.IsBase64()) return input;
            string result = input.Trim().Replace('_', '/').Replace('-', '+');
            switch (result.Length % 4) {
                case 2: result += "=="; break; //add two equl signs. so we have 4 character.
                case 3: result += "="; break; //add one equal sign. so we have 4 character.
                }
            return result;
            }

        public static bool IsBase64(this string input) {
            if (string.IsNullOrEmpty(input) || input.Length % 4 != 0
               || input.Contains(" ") || input.Contains("\t") || input.Contains("\r") || input.Contains("\n"))
                return false;
            try {
                Convert.FromBase64String(input);
                return true;
                } catch (Exception) {
                return false;
                }
            }

        public static bool IsMD5(this string input) {
            return Regex.IsMatch(input, "^[0-9a-fA-F]{32}$", RegexOptions.Compiled);
        }

        public static bool IsValidJson(this string json) {
            try {
                if (string.IsNullOrWhiteSpace(json)) return false;
                string jsonContent = json;
                if (json.IsArray()) {
                    jsonContent = json.Substring(1, json.Length - 2).Trim();
                }

                if (jsonContent.StartsWith("{") && jsonContent.EndsWith("}")) return true;
                //JsonDocument.Parse(json.Trim()); return true; //too much time consuming.
            } catch (Exception ex) {
                }
            return false;
        }

        static bool IsArray(this string json) {
            //Could be json array or a normal string array
            try {
                var jsonStr = json.Trim();
                if (!(jsonStr.StartsWith("[") && jsonStr.EndsWith("]"))) return false;
                return true;
            } catch (Exception) {
                return false;
            }
        }

        static bool IsStringArray(this string json,out string[] result) {
            result = null;
            try {
                var jsonStr = json.Trim();
                if (!(jsonStr.StartsWith("[") && jsonStr.EndsWith("]"))) return false;
                //It looks like it is a Json array. Lets check the content and see if it starts with curly braces.
                var arrayContent = jsonStr.Substring(1, jsonStr.Length - 2); //Removing first and last letter.
                if (arrayContent.StartsWith("{") && arrayContent.EndsWith("}")) return false;
                result = arrayContent.Split(',');
                return true; 
            } catch (Exception ex) {
                return false;
            }
        }

        public static string Separate(this string input, int splitLength, int depth, string delimiter = "\\", bool addPadding = true,  char padChar = '0', bool isDirPath = false) {
            if (string.IsNullOrWhiteSpace(input)) { throw new ArgumentNullException("Input is empty. Nothing to split."); }
            if (depth < 0) depth = 0; //We cannot have less than 0
            if (splitLength < 1) splitLength = 2; //we need a minimum 1 split.
            if (splitLength > 7) splitLength = 7;
            List<string> pathBuilder = new List<string>();
            var wval = Path.GetFileNameWithoutExtension(input)?.Trim()?.Replace(" ",""); //Just a precaution to exclude extension
            var extension = Path.GetExtension(input);

            //Go down until we have reached the desired directory level
            int currentLevel = 1;
            bool isLastPart = false;

            //for number or for hash, we need to ensure that, we need a padding. padding will be with 0. Should the padding happen at the right or left?
            if (addPadding ) {
                var padLength = splitLength - (wval.Length % splitLength);
                if (padLength != 0 && padLength != splitLength) {
                    wval =  wval.PadLeft(wval.Length + padLength, padChar);
                }
            }

            for (int i = 0; i < wval.Length && !isLastPart; i = i + splitLength) {
                //if i+2 is greater than idString.Length, then we are at the last part.
                if (depth > 0 && currentLevel > depth) {
                    isLastPart = true; //we will not rerun again.
                } else {
                    isLastPart = i + splitLength > wval.Length - 1;
                }

                string idPart = isLastPart ? wval.Substring(i) : wval.Substring(i, splitLength);

                if (isLastPart && !string.IsNullOrWhiteSpace(extension)){
                    idPart = idPart + extension;
                }
                pathBuilder.Add(idPart);
                currentLevel++; //add one more depth to the directory desired.
            }
            if (isDirPath) return Path.Combine(pathBuilder.ToArray()); //because the delimiter '//' will not work for linux.. it expects \\
            return string.Join(delimiter, pathBuilder.ToArray());
        }

        public static string PadCenter(this string source, int length, char character = '\u0000') {
            int space_available = length - source.Length;
            int pad_left = (space_available / 2) + source.Length; //Amount to pad left.
            if (character == '\u0000')
            {
                return source.PadLeft(pad_left).PadRight(length);
            }
            else
            {
                return source.PadLeft(pad_left, character).PadRight(length, character);
            }
        }
        /// <summary>
        /// When a base64 string is provided as input, it will replace the "/ + = " signs.
        /// </summary>
        /// <returns> if input is not base64, it will return same input. Else it will return sanitized value.</returns>
        public static string SanitizeJWT(this string input) {
            if (!input.IsBase64()) return input;
            string result = input.Replace('+', '-').Replace('/', '_').Replace("=", "");
            return result;
        }
        /// <summary>
        /// Convert the Json to dictionary
        /// </summary>
        /// <param name="input">Input json file</param>
        /// <param name="result">output converted result</param>
        /// <param name="searchlevel">0 - Makes search all levels.</param>
        /// <returns></returns>
        public static bool JsonToDictionary(this string jsonInput, out object result, int searchlevel = 1, string[] ignoreKeys = null) {
            if (searchlevel < 0) searchlevel = 0;
            return JsonToDictionary(jsonInput, searchlevel, 1,ignoreKeys, out result); //Current search level is always 1.
            }

        public static string ToNumber(this string input) {
            string numbered_key = string.Empty;
            foreach (var _char in input)
            {
                int index = (int)_char % 32;
                numbered_key += index;
            }
            return numbered_key;
        }

        static void DeepConvertJson(Dictionary<string, object> dic,int searchlevel,int currentlevel, string[] ignoreKeys) {

            if (dic == null || dic.Count < 1) return;
            //Check for levels
            if (searchlevel != 0) {
                //Compare with currentlevel
                if (currentlevel > searchlevel) return; //We already reached the search end.
            }

            foreach (var item in dic) {
                try {
                    if (ignoreKeys != null && ignoreKeys.Contains(item.Key, StringComparer.OrdinalIgnoreCase)) continue; //Do not try to change this value.
                    var valueStr = item.Value?.ToString();
                    if (valueStr == null || !IsValidJson(valueStr)) continue;
                    //Now if this is a json but not able to be converted, then there is a possibility, this could be a string array
                    //If this is a string array, convert and replace the value, else proceed
                    if (valueStr.IsStringArray(out var strArray)) {
                        dic[item.Key] = strArray;
                        continue;
                    }

                    //We have got a valid json, which is not a json array as well. Let's proceed to convert it.
                    if (valueStr.JsonToDictionary(searchlevel,currentlevel, ignoreKeys, out var result)) {
                        dic[item.Key] = result;
                    }
                } catch (Exception ex) {
                    //log it
                    continue;
                }
            }
        }

        //    }
        //}
        /// <summary>
        /// Convert the Json to dictionary
        /// </summary>
        /// <param name="input">Input json file</param>
        /// <param name="result">output converted result</param>
        /// <param name="searchlevel">0 - Makes search all levels.</param>
        /// <returns></returns>
        static bool JsonToDictionary(this string jsonInput, int searchlevel,int currentlevel, string[] ignoreKeys, out object result)
        {
            //Input could be json array or single json.
            result = null;
            if (jsonInput == null) return false;
            
            try
            {
                var jsonStr = jsonInput.Trim();
                if (string.IsNullOrWhiteSpace(jsonStr) || !jsonStr.IsValidJson()) return false;  //For invalid jsons do not proceed further

                if (IsArray(jsonStr))
                {
                    //Even if it is an array value, it will return as valid json.
                    //check if this is a json array
                    if (jsonStr.IsStringArray(out _)) return false; //no need to process string array.
                    //Array
                    var jsonStrList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonStr);
                    if (jsonStrList != null && jsonStrList is List<Dictionary<string, object>> inputList)
                    {
                        //Current level is already converted to inputlist
                        foreach (var dicLocal in inputList)
                        {
                            DeepConvertJson(dicLocal,searchlevel,currentlevel+1, ignoreKeys); //We are direclty modifying in the dictionary.
                        }
                        result = inputList;
                    }
                } else if (jsonStr.StartsWith("{") && jsonStr.EndsWith("}"))
                {
                    //object
                    var jsonSingle = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonStr);
                    if (jsonSingle != null && jsonSingle is Dictionary<string, object> inputDic)
                    {
                        //currentlevel is already converted to inputDic
                        DeepConvertJson(inputDic, searchlevel, currentlevel+1, ignoreKeys);  //We are direclty modifying in the dictionary.
                        result = inputDic; //Single dictionary
                    }
                }

                //SchemaProps = JsonObject.Parse(SchemaProps.ToString());
            } catch (Exception ex)
            {
                //throw;
                return false;
            }
            return true;
        }
    }
}
