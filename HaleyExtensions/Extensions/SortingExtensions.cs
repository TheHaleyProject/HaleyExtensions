using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Haley.Utils
{
    public static class SortingExtensions
    {
        /// <summary>
        /// Order by alphanumeric (to consider values at end).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IOrderedEnumerable<T> OrderByAlphaNumeric<T>(this IEnumerable<T> source, Func<T, string> selector)
        {
            var _pattern = @"\d+"; //One or more digits (consecuetively)

            //The numeric value can be anywhere inside a string. It could be at end or middle or at start.
            int max = source
                .SelectMany(i =>
                Regex.Matches(selector(i), _pattern) //Matches searches for all occurences in the string and produces as a match collection. So any continuos digits found in the string will be captured.
                .Cast<Match>()
                .Select(m => (int?)m.Value.Length)) //from the match collection select the length of all values (to find out how many digits we have continuously (lets say we get 1, 0, 9531 for 1.0.9531)
                .Max() ?? 0; //among the collection, find which has maximum length.

            //use the maximum and replace all the values with 0 (pad left) so that when comparing as string, it will be correctly sorted.
            return source.OrderBy(i => Regex.Replace(selector(i), _pattern, m => m.Value.PadLeft(max, '0')));
        }
    }
}
