using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Utils {
    public static class NumericExtensions {
        //static string[] mUnits = new string[] {"B","KB","MB","GB","TB","PB","EB"};
        //static string[] biUnits = new string[] { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB" };
        static int biUnitsLength = Enum.GetNames(typeof(DataSizeBinary)).Length;
        static int deUnitsLength = Enum.GetNames(typeof(DataSizeDecimal)).Length;
        const int biSize = 1024;
        const int deSize = 1000;

        public static string ToFileSize(this long input, bool binaryResult = true) {
            return System.Convert.ToDouble(input).ToFileSize(binaryResult);
        }
        public static string ToFileSize(this long input, DataSizeDecimal currentUnit, bool binaryResult = true) {
            return System.Convert.ToDouble(input).ToFileSize(currentUnit, binaryResult);
        }
        public static string ToFileSize(this long input, DataSizeBinary currentUnit, bool binaryResult = true) {
            return System.Convert.ToDouble(input).ToFileSize(currentUnit,binaryResult);
        }
        public static string ToFileSize(this int input, bool binaryResult = true) {
            return System.Convert.ToDouble(input).ToFileSize(binaryResult);
        }
        public static string ToFileSize(this int input, DataSizeDecimal currentUnit, bool binaryResult = true) {
            return System.Convert.ToDouble(input).ToFileSize(currentUnit, binaryResult);
        }
        public static string ToFileSize(this int input, DataSizeBinary currentUnit, bool binaryResult = true) {
            return System.Convert.ToDouble(input).ToFileSize(currentUnit, binaryResult);
        }
        //Double
        public static string ToFileSize(this double input, bool binaryResult = true) {
            return ToFileSizeInternal(input, binaryResult);
        }
        public static string ToFileSize(this double input, DataSizeDecimal currentUnit, bool binaryResult = true) {
            //Convert the given input to its byte equivalent
            return ToFileSizeInternal(input * Math.Pow(deSize, (int)currentUnit),  binaryResult);
        }
        public static string ToFileSize(this double input, DataSizeBinary currentUnit, bool binaryResult = true) {
            //Convert the given input to its byte equivalent
            return ToFileSizeInternal(input * Math.Pow(biSize, (int)currentUnit), binaryResult);
        }

        static string ToFileSizeInternal(this double input, bool binaryResult = true) {
            //We assume the input is in double byte.
            double inputW = input;
            int depth = 0;
            int sizeSeparator = binaryResult ? biSize : deSize;
            int length = binaryResult ? biUnitsLength : deUnitsLength;
            while (inputW > sizeSeparator && depth < length) {
                inputW = Math.Round(inputW / sizeSeparator, 2);
                depth++;
            }

            //what if we reached the end of depth?
            if (depth >= length) depth = length - 1; //to avoid array size error.

            //We can also try Enum.IsDefined to find if the integer is defined in the enum or not. However, since we are checking the depth/length above, it's not required.
            return inputW.ToString() + " " + (binaryResult ? $@"{(DataSizeBinary)depth}" : $@"{(DataSizeDecimal)depth}");
        }

        public static string Convert(this short[] array) {
            List<char> chars = new List<char>();
            for (int i = 0; i < array.Length; i++) {
                if (array[i] < 1) continue;
                chars.Add((char)array[i]); // Convert each Int16 to a char
            }
            return new string(chars.ToArray()); // Create a string from the char array
        }

        public static DataSizeBinary AsBinary(this DataSizeDecimal input) {
            return (DataSizeBinary)((int)input);
        }
        public static DataSizeDecimal AsDecimal(this DataSizeBinary input) {
            return (DataSizeDecimal)((int)input);
        }
    }
}
