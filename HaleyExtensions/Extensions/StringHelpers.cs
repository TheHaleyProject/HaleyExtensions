using Haley.Enums;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;
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

        public static bool IsValidJson(this string json) {
            try {
                JsonDocument.Parse(json); return true;
                } catch (Exception ex) {
                return false;
                }
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
        public static bool ToDictionary(string jsonInput, out List<Dictionary<string, object>> result, int searchlevel = 1) {
            if (searchlevel < 0) searchlevel = 0;
            return ToDictionary(jsonInput, searchlevel, 1, out result); //Current search level is always 1.
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
        //static bool IsJsonArray(this string json)
        //{
        //    try
        //    {
        //    } catch (Exception)
        //    {

        static void DeepConvertJson(Dictionary<string, object> dic) {
            string[] localkeys = dic.Keys.ToArray(); ;


            //Loop each item to check if value is json.
            foreach (var key in localkeys) {
                if (dic.ContainsKey(key) && dic[key] != null) {
                    //Confirm if the value is a string first.
                    try {
                        var valueStr = dic[key].ToString();
                        if (!(valueStr.StartsWith("{") || valueStr.StartsWith("["))) continue; //No need to change this dictionary value.
                        if (valueStr.Trim().StartsWith("{") && valueStr.Trim().EndsWith("}")) {
                            dic[key] = JsonSerializer.Deserialize<Dictionary<string, object>>(dic[key].ToString());
                            } else if (valueStr.Trim().StartsWith("[") && valueStr.Trim().EndsWith("]")) {
                            //dic[key] =
                            }


                        } catch (Exception ex) {
                        //log it
                        continue;
                        }

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
        static bool ToDictionary(string jsonInput, int searchlevel,int currentlevel, out List<Dictionary<string, object>> result)
        {
            //Input could be json array or single json.
            result = new List<Dictionary<string, object>>();
            if (jsonInput == null) return false;
            
            try
            {
                var jsonStr = jsonInput.Trim();
                if (string.IsNullOrWhiteSpace(jsonStr) || !jsonStr.IsValidJson()) return false;

                if (jsonStr.StartsWith("[") && jsonStr.EndsWith("]"))
                {
                    //Even if it is a normal value, it will 
                    //Array
                    var jsonStrList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonStr);
                    if (jsonStrList != null && jsonStrList is List<Dictionary<string, object>> inputList)
                    {
                        foreach (var dicLocal in inputList)
                        {
                            DeepConvertJson(dicLocal, childConvertKeys, forallkeys);
                        }
                        result = inputList;
                    }
                } else if (jsonStr.StartsWith("{") && jsonStr.EndsWith("}"))
                {
                    //object
                    var resultRawDic = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonStr);
                    if (resultRawDic != null && resultRawDic is Dictionary<string, object> inputDic)
                    {
                        DeepConvertJson(inputDic, childConvertKeys, forallkeys);
                        result.Add(inputDic);
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
