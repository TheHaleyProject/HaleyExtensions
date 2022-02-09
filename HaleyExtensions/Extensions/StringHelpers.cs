using System;
using System.Collections.Generic;
using System.Text;
using Haley.Enums;
using System.Text.RegularExpressions;
using System.Linq;

namespace Haley.Utils
{
    public static class StringHelpers
    {
        public static string PadCenter(this string source, int length, char character = '\u0000')
        {
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
        public static bool IsBase64(this string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length % 4 != 0
               || input.Contains(" ") || input.Contains("\t") || input.Contains("\r") || input.Contains("\n"))
                return false;
            try
            {
                Convert.FromBase64String(input);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static string ToNumber(this string input)
        {
            string numbered_key = string.Empty;
            foreach (var _char in input)
            {
                int index = (int)_char % 32;
                numbered_key += index;
            }
            return numbered_key;
        }
        public static CompareStatus? CompareWith(this string source, string target)
        {
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

            switch (_source_padded.CompareTo(_target_padded))
            {
                case 0:
                    return CompareStatus.Equal;
                case 1:
                    return CompareStatus.Greater;
                case -1:
                    return CompareStatus.Lesser;
            }

            return null;
        }
    }
}
