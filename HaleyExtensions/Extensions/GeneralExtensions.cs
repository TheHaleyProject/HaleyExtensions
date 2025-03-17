using Haley.Abstractions;
using System;

namespace Haley.Utils
{
    public static class GeneralExtensions {
       public static void Throw(this IFeedback input) {
            if (input.Status) return;
            throw new ArgumentException($@"Fail: {input.Message}");
        }
    }
}
