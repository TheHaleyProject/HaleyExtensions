using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Haley.Utils {
    public static class MimeMapExtension {
        private static readonly ConcurrentDictionary<string, string[]> _customMappings =
            new ConcurrentDictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

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

        private static bool TryExtractLookupExtension(string input, out string extension) {
            extension = string.Empty;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // If input already looks like a MIME type → reject
            if (input.Contains("/") && !input.StartsWith("."))
                return false; // to prevent reverse mapping vulnerabilities

            extension = ExtractExtension(input);
            return !string.IsNullOrWhiteSpace(extension);
        }

        private static string[] NormalizeMimeValues(string[] values) {
            if (values == null)
                return Array.Empty<string>();

            var normalized = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values) {
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                var trimmed = value.Trim();
                if (seen.Add(trimmed))
                    normalized.Add(trimmed);
            }

            return normalized.ToArray();
        }

        /// <summary>
        /// Replaces the current runtime custom MIME mappings with the provided values.
        /// Each extension may have one or more valid MIME types.
        /// </summary>
        public static void LoadCustomMappings(IDictionary<string, string[]> mappings) {
            _customMappings.Clear();

            if (mappings == null || mappings.Count == 0)
                return;

            foreach (var entry in mappings) {
                if (string.IsNullOrWhiteSpace(entry.Key) || entry.Value == null)
                    continue;

                var ext = ExtractExtension(entry.Key);
                if (string.IsNullOrWhiteSpace(ext))
                    continue;

                var values = NormalizeMimeValues(entry.Value);
                if (values.Length == 0)
                    continue;

                _customMappings[ext] = values;
            }
        }

        /// <summary>
        /// Returns all known MIME types for the given extension by combining runtime custom mappings
        /// with the built-in defaults.
        /// </summary>
        public static bool TryGetMimeTypes(string input, out string[] mimeTypes) {
            mimeTypes = Array.Empty<string>();
            if (!TryExtractLookupExtension(input, out var ext))
                return false;

            var orderedTypes = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (_mappings.TryGetValue(ext, out var builtInMime)) {
                orderedTypes.Add(builtInMime);
                seen.Add(builtInMime);
            }

            if (_customMappings.TryGetValue(ext, out var customFound)) {
                foreach (var customMime in customFound) {
                    if (string.IsNullOrWhiteSpace(customMime))
                        continue;

                    if (seen.Add(customMime))
                        orderedTypes.Add(customMime);
                }
            }

            if (orderedTypes.Count == 0)
                return false;

            mimeTypes = orderedTypes.ToArray();
            return true;
        }

        public static string[] GetMimeTypes(string input) =>
            TryGetMimeTypes(input, out var mimeTypes) ? mimeTypes : Array.Empty<string>();
    }
}
