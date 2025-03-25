using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Utils {
    public static class NumericExtensions {
        static string[] mUnits = new string[] {"B","KB","MB","GB","TB","PB","EB"};
        static string[] biUnits = new string[] { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB" };
        static int biSize = 1024;
        static int mSize = 1000;
        public static string ToFileSize(this long input, bool binarySize = true) {
            return System.Convert.ToDouble(input).ToFileSize(binarySize);
        }
        public static string ToFileSize(this int input, bool binarySize = true) {
            return System.Convert.ToDouble(input).ToFileSize(binarySize);
        }

        public static string ToFileSize(this double input, bool binarySize = true) {
            double inputW = input;
            int depth = 0;
            int sizeSeparator = binarySize ? biSize : mSize;
            int length = binarySize ? biUnits.Length : mUnits.Length;
            while (inputW > sizeSeparator && depth < length) {
                inputW = Math.Round(inputW / sizeSeparator, 2);
                depth++;
            }

            //what if we reached the end of depth?
            if (depth >= length) depth = length - 1; //to avoid array size error.
            return inputW.ToString() + " " + (binarySize ? biUnits[depth] : mUnits[depth]);
        }

        public static string Convert(this short[] array) {
            List<char> chars = new List<char>();
            for (int i = 0; i < array.Length; i++) {
                if (array[i] < 1) continue;
                chars.Add((char)array[i]); // Convert each Int16 to a char
            }
            return new string(chars.ToArray()); // Create a string from the char array
        }
    }
}
