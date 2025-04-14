using Haley.Abstractions;
using Haley.Enums;
using System;

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
    }
}
