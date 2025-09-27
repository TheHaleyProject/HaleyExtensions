using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    }
}
