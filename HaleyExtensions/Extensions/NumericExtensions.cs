using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Extensions {
    public static class NumericExtensions {
        static string[] units = new string[] {"KB","MB","GB","TB","PB","EB"};
        static int sizeSeparator = 1024;
        public static string ToFileSize(this long input) {
            if (input < sizeSeparator) return input.ToString() + " B";
            long inputW = input;
            int depth = 0;
            bool parsed = false;
            while (depth < units.Length && !parsed) {
                inputW = Math.Round(inputW/)
            }
        }
    }
}
