using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Haley.Utils {
    public static class MimeMapExtension {
        private const string DefaultMime = "application/octet-stream";
        private static readonly Dictionary<string, string> _mappings =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // --- Documents ---
            { ".pdf",  "application/pdf" },
            { ".txt",  "text/plain" },
            { ".csv",  "text/csv" },
            { ".log",  "text/plain" },
            { ".json", "application/json" },
            { ".xml",  "text/xml" },

            // Word / Excel / PowerPoint (OOXML)
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".doc",  "application/msword" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".xls",  "application/vnd.ms-excel" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { ".ppt",  "application/vnd.ms-powerpoint" },

            // --- Images ---
            { ".jpg",  "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png",  "image/png" },
            { ".gif",  "image/gif" },
            { ".bmp",  "image/bmp" },
            { ".webp", "image/webp" },

            // --- Video ---
            { ".mp4",  "video/mp4" },
            { ".mkv",  "video/x-matroska" },

            // ---- Audio ----
            { ".mp3",  "audio/mpeg" },
            { ".wav",  "audio/wav" },
            { ".m4a",  "audio/mp4" },

            // --- Archives ---
            { ".zip",  "application/zip" },
            { ".rar",  "application/x-rar-compressed" },
            { ".gz",   "application/gzip" },
            { ".tar",  "application/x-tar" },

            // Optional Adobe formats (only if needed)
            { ".psd",  "image/vnd.adobe.photoshop" },
            { ".ai",   "application/postscript" }
        };

        /// <summary>
        /// Multi-dot smart extension extractor:
        /// file.tar.gz      → .tar.gz
        /// file.config.json → .json
        /// file.dll.config  → .config
        /// file.jpg.php     → .php (correct for detection)
        /// </summary>
        private static string ExtractExtension(string input) {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            input = input.Trim().ToLowerInvariant();

            // strip ?query
            var qIndex = input.IndexOf("?", StringComparison.Ordinal);
            if (qIndex > 0)
                input = input.Substring(0, qIndex);

            // Force dot-prefix if missing
            if (!input.StartsWith(".")) {
                var idx = input.LastIndexOf('.');
                if (idx >= 0)
                    input = input.Substring(idx);
                else
                    input = "." + input;
            }

            // Multi-dot known formats
            string[] multiExt = { ".tar.gz", ".tar.bz2", ".tar.xz" };
            foreach (var m in multiExt) {
                if (input.EndsWith(m))
                    return m;
            }

            // Regular extension (last segment)
            var lastDot = input.LastIndexOf('.');
            return lastDot >= 0 ? input.Substring(lastDot) : input;
        }


        public static bool TryGetMimeType(string input, out string mime) {
            mime = DefaultMime;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // If input already looks like a MIME type → reject
            if (input.Contains("/") && !input.StartsWith("."))
                return false; // to prevent reverse mapping vulnerabilities

            var ext = ExtractExtension(input);
            if (string.IsNullOrWhiteSpace(ext))
                return false;

            if (_mappings.TryGetValue(ext, out var found)) {
                mime = found;
                return true;
            }

            return false;
        }

        public static string GetMimeType(string input) =>
            TryGetMimeType(input, out var mime) ? mime : DefaultMime;
    }
}
