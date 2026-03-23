using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;

namespace Haley.Utils
{
    public static class FeedbackExtensions {
       public static IFeedback<T> Rollback<T>(this IFeedback<T> fb,ITransactionHandler handler) {
            try {
                if (handler != null) handler.Rollback();
            } catch (Exception) {
            }
            return fb;
        }

        public static IFeedback AsFeedBack<T>(this IFeedback<T> fb) {
            return new Feedback(fb.Status, fb.Message) { Result = fb.Result };
        }

        public static IFeedback AsJsonResult(this IFeedback fb) {
            try {
                if (fb == null || string.IsNullOrWhiteSpace(fb.Result?.ToString())) return fb;
                if (fb.Result is string resString && resString.IsValidJson()) {
                    fb.Result = JsonNode.Parse(resString);
                } 
                return fb;

            } catch (Exception) {
            }
            return fb;
        }


        public static IFeedback<T> Commit<T>(this IFeedback<T> fb,ITransactionHandler handler) {
            try {
                if (handler != null) handler.Commit();
            } catch (Exception) {
            }
            return fb;
        }


        public static void Throw(this IFeedback input) {
            if (input.Status) return;
            throw new ArgumentException($@"Fail: {input.Message}");
        }

        public static IFeedback<T> TryAs<T>(this object result) {
            var fb = new Feedback<T>();
            try {
                if (result is null || result is DBNull) return fb.SetStatus(true).SetResult(default!).SetMessage("No result returned."); //if result is null, we still need to return the result, we cannot call it as false. May be leave the result empty for the application to process.

                var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                // bool first (covers BIT(1), TINYINT(1), string, byte[])
                if (target == typeof(bool)) {
                    if (result.TryToBool(out var bv)) return fb.SetStatus(true).SetResult((T)(object)bv);
                    return fb.SetMessage($"Unexpected object type for bool. Got {result.GetType().Name} value '{result}'.");
                }

                if (target == typeof(long)) {
                    if (result.TryConvertToLong(out var lv))
                        return fb.SetStatus(true).SetResult((T)(object)lv);
                    return fb.SetMessage($"Failed to convert scalar to long. Got {result.GetType().Name} value '{result}'.");
                }

                if (target == typeof(int)) {
                    if (result.TryConvertToInt(out var iv))
                        return fb.SetStatus(true).SetResult((T)(object)iv);
                    return fb.SetMessage($"Failed to convert scalar to int. Got {result.GetType().Name} value '{result}'.");
                }

                if (target == typeof(double)) {
                    if (result.TryConvertToDouble(out var dv))
                        return fb.SetStatus(true).SetResult((T)(object)dv);
                    return fb.SetMessage($"Failed to convert scalar to double. Got {result.GetType().Name} value '{result}'.");
                }

                // Fast-path numeric conversions commonly used (int/long)

                if (result is T typed) return fb.SetStatus(true).SetResult(typed);

                //One final ditch attempt to convert to string. Reason is, we might sometimes, get GUID but expect it to be converted to string.. In those cases, we can directly ToString().
                if (target == typeof(string)) return fb.SetStatus(true).SetResult((T)(object)result.ToString()!);

                return fb.SetMessage($"Unexpected scalar type. Expected {typeof(T).Name}, got {result.GetType().Name}.");
            } catch (Exception ex) {
                return fb.SetStatus(false).SetMessage(ex.ToString());
            }
        }
    }
}
