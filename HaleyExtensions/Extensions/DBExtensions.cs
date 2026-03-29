using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using Haley.Internal;
using static Haley.Internal.QueryFields;

namespace Haley.Utils
{
    public static class DBExtensions{
        public static IAdapterArgs SetFilter(this IAdapterArgs args,ResultFilter filter) {
            if (args == null) return args;
            args.Filter = filter;
            return args;
        }

        public static IAdapterArgs SetOutputName(this IAdapterArgs args, string output_name) {
            if (args == null) return args;
            args.OutputName = output_name;
            return args;
        }

        public static IAdapterArgs ToAdapterArgs(this Dictionary<string,object> input,string adapterKey, string query) {
            if (input == null || input.Count == 0) throw new ArgumentNullException($@"Input cannot be null or empty for conversion");
            var db = new AdapterArgs(adapterKey) { Query = query };
            db.SetParameters(input);
            return db;
        }

        public static IAdapterArgs ToAdapterArgs(this Dictionary<string, object> input, string query) {
            if (input == null || input.Count == 0) throw new ArgumentNullException($@"Input cannot be null or empty for conversion");
            var db = new AdapterArgs() { Query = query };
            db.SetParameters(input);
            return db;
        }

        public static IModuleArgs ToModuleArgs(this Dictionary<string, object> input) {
            if (input == null || input.Count == 0) throw new ArgumentNullException($@"Input cannot be null or empty for conversion");
            var db = new ModuleArgs().SetParameters(input);
            return db;
        }

        public static IAdapterArgs ToAdapterArgs(this IParameterBase input) {
            return input.ToAdapterArgs(string.Empty);
        }
        public static IAdapterArgs ToAdapterArgs(this IParameterBase input, string query) {
            return input.ToAdapterArgs(query, string.Empty);
        }
        public static IAdapterArgs ToAdapterArgs(this IParameterBase input, string query,string groupKey) {
            if (input == null) throw new ArgumentNullException($@"Input cannot be null for conversion");
            var db = new AdapterArgs(input.Key) { Query = query};

            var rodic = string.IsNullOrWhiteSpace(groupKey) ? input.Parameters : input.GetGroupParameters(groupKey);
            db.SetParameters(new Dictionary<string, object>(rodic.ToDictionary(p=> p.Key, q => q.Value))); //since parameter set is protected.

            if (input is ModuleArgs mdp) {
                db.Adapter = mdp.Adapter; //set the target
                db.TransactionMode = mdp.TransactionMode;
            }
            return db;
        }

        public static IAdapterConfig AsAdapterConfig(this string input) {
            if (string.IsNullOrWhiteSpace(input)) return null;
            AdapterConfig result = new AdapterConfig();
            
            return result;
        }

        public static P ForTransaction<P>(this IModuleArgs input, ITransactionHandler handler, bool throwInvalid = true) where P: IModuleArgs {
            return (P)ForTransaction(input, handler,throwInvalid);
        }
        public static IModuleArgs ForTransaction(this IModuleArgs input, ITransactionHandler handler, bool throwInvalid = true) {
            if (handler == null) {
                if (!throwInvalid) {
                    Console.WriteLine("Hander not found. Returning the same module args");
                    return input;
                }
                throw new ArgumentNullException("Handler cannot be null. Cannot include transaction information in the adatper argument");
            } 
            return handler.CreateDBInput(input);
        }

        public static IAdapterArgs ForTransaction(this IAdapterArgs input, ITransactionHandler handler, bool throwInvalid = true) {
            if (handler == null) {
                if (throwInvalid) throw new ArgumentNullException("Handler cannot be null. Cannot include transaction information in the adatper argument");
                return input;
            }

            if (input is AdapterArgs db && handler is IDBAdapter th) {
                db.Adapter = th; //set the target
                db.TransactionMode = true;
            }
            return input;
        }
        public static P ForTransaction<P>(this IAdapterArgs input, ITransactionHandler handler, bool throwInvalid = true) where P : IAdapterArgs{
            return (P)ForTransaction(input, handler,throwInvalid);
        }

        public static async Task<IFeedback> CreateDatabase(this IAdapterGateway agw, DbCreationArgs args) {
            var fb = new Feedback().SetSource("HALEY-DB-UTILS");
            try {
                //Agw also has the default adapter key. but dont take it.. Sometimes, user might make mistakes and forget to send the key.. We dont' want any side effects.. What if they are working on production application and set up the default key?.. Dont mess up.. Always expect the key.
                if (args == null || string.IsNullOrWhiteSpace(args.Key)) throw new ArgumentNullException("Adapter Key is needed to generate a database.");

                //First pass
                if (!agw.ContainsKey(args.Key)) {
                    //Check if cloning is present or not.
                    if (string.IsNullOrWhiteSpace(args.CloningAdapterKey)) throw new ArgumentNullException($@"No adapter found for the given key {args.Key}");
                    if (!agw.ContainsKey(args.CloningAdapterKey)) throw new ArgumentNullException($@"No adapter found for the provided cloning key {args.CloningAdapterKey}.");
                    //In this case, we just duplicate and create the adapter.
                    agw.DuplicateAdapter(args.CloningAdapterKey, args.Key, ("database", args.DBName)); //this will just generate the adapter but not create the database.
                }
                //If the service or the db doesn't exist, we throw exception or else the system would assume that nothing is wrong. If they wish , they can still turn of the indexing.
                if (!agw.ContainsKey(args.Key)) throw new ArgumentException($@"Load SQL Failed.No adapter found for the given key {args.Key}");
                //Next step is to find out if the database exists or not? Should we even try to check if the database exists or directly run the sql script and create the database if it doesn't exists?
                var adapterInfo = agw[args.Key].Info;
                if (adapterInfo == null) throw new ArgumentNullException($@"Adapter info is null for the given key {args.Key}. Cannot proceed.");

                //What if the connection string doesn't contain the database name?

                args.DBName = adapterInfo.DBName ?? adapterInfo.ConnectionString?.GetValue("database")?.ToString() ??args.FallBackDBName; //This is supposedly our db name.

                switch (adapterInfo.DBType) {
                    case Enums.TargetDB.maria:
                    case Enums.TargetDB.mysql:
                    return await agw.CreateDatabaseMaria(args);
                    default:
                    throw new NotImplementedException($@"The target db type {adapterInfo.DBType} is not implemented for loading the initial sql.");
                }
            } catch (Exception ex) {
                return fb.SetMessage(ex.Message).SetTrace(ex.StackTrace).SetCode((int)HttpStatusCode.InternalServerError);
            }
        }
        static async Task<IFeedback> CreateDatabaseMaria(this IAdapterGateway agw, DbCreationArgs args) {
            var fb = new Feedback().SetSource("HALEY-DB-MARIA");

            var existsFeedback = await agw.ScalarAsync<int?>(new AdapterArgs(args.Key) {
                ExcludeDBInConString = true,
                Query = QRY_MARIA.SCHEMA_EXISTS
            }, (NAME, args.DBName));

            if (!existsFeedback.Status) {
                throw new InvalidOperationException(
                    $@"Unable to check whether database '{args.DBName}' exists. {existsFeedback.Message}");
            }

            if (existsFeedback.Result.HasValue)
                return fb.SetStatus(true).SetMessage($@"Database {args.DBName} already exists. No action taken.").SetCode((int)HttpStatusCode.Continue);

            string content = args.SQLContent;

            if (string.IsNullOrWhiteSpace(content)) {
                if (string.IsNullOrWhiteSpace(args.SQLPath)) throw new ArgumentNullException("Either SQL content or SQL path must be provided.");

                if (!File.Exists(args.SQLPath)) throw new ArgumentException($@"SQL file is not found in {args.SQLPath}. Please check..");
                //if the file exists, then run this file against the adapter gateway but ignore the db name.
                content = File.ReadAllText(args.SQLPath);

                if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException($@"SQL file is empty in {args.SQLPath}. Please check..");
            }

            if (args.VariablesToReplace != null && args.VariablesToReplace.Count > 0) {
                foreach (var kvp in args.VariablesToReplace) {
                    if (string.IsNullOrWhiteSpace(kvp.Key)) continue;
                    content = content.Replace(kvp.Key, kvp.Value);
                }
            }

            var processed = args.ContentProcessor?.Invoke(content, args.DBName);
            if (!string.IsNullOrWhiteSpace(processed)) content = processed;
            content = NormalizeMariaDumpSyntax(content);

            //?? Should we run everything in one go or run as separate statements???
            //if the input contains any delimiter or procedure, remove them.

            object queryContent = content;
            List<string> procedures = new List<string>();
            //if (content.Contains("Delimiter", StringComparison.InvariantCultureIgnoreCase)) //Doesn't work in .net standard.
            if (content?.IndexOf("Delimiter", StringComparison.OrdinalIgnoreCase) >= 0) {
                //Step 1 : Remove delimiter lines
                content = Regex.Replace(content, @"DELIMITER\s+\S+", "", RegexOptions.IgnoreCase); //Remove the delimiter comments

                //Step 2 : Extract all Procedures
                string pattern = @"CREATE\s+PROCEDURE.*?END\s*//";
                var matches = Regex.Matches(content, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

                foreach (Match match in matches) {
                    string proc = match.Value;
                    proc = proc.Replace("//", ";").Trim();
                    procedures.Add(proc);
                    content = content.Replace(match.Value, "");
                }
            }

            // Step 4: Split SQL statements using a lightweight scanner instead of raw regex.
            // This avoids breaking on semicolons inside strings/comments and keeps dump-style
            // executable comments stable after normalization.
            queryContent = SplitSqlStatements(content);

            var handler = agw.GetTransactionHandler(args.Key);
            using (handler.Begin(true)) {
                try {
                    var createFeedback = await agw.NonQueryAsync(
                        new AdapterArgs(args.Key) { ExcludeDBInConString = true, Query = queryContent }.ForTransaction(handler));
                    if (!createFeedback.Status) {
                        throw new InvalidOperationException(
                            $@"Database bootstrap SQL failed for '{args.DBName}'. {createFeedback.Message}");
                    }

                    if (procedures.Count > 0) {
                        var procFeedback = await agw.NonQueryAsync(
                            new AdapterArgs(args.Key) { ExcludeDBInConString = true, Query = procedures.ToArray() }.ForTransaction(handler));
                        if (!procFeedback.Status) {
                            throw new InvalidOperationException(
                                $@"Database bootstrap procedure creation failed for '{args.DBName}'. {procFeedback.Message}");
                        }
                    }
                } catch (Exception) {
                    handler.Rollback();
                    // DDL (CREATE TABLE) causes implicit commits in MariaDB — rollback cannot undo them.
                    // Drop the partially-created database so no half-cooked schema is left behind.
                    try {
                        await agw.NonQueryAsync(new AdapterArgs(args.Key) {
                            ExcludeDBInConString = true,
                            Query = $"DROP DATABASE IF EXISTS `{args.DBName}`;"
                        });
                    } catch { /* best-effort cleanup — swallow so original exception propagates */ }
                    throw;
                }
            }

            return fb.SetStatus(true).SetCode((int)HttpStatusCode.Created);
        }

        // HeidiSQL / MySQL dumps sometimes emit:
        //   CREATE DATABASE IF NOT EXISTS `name`; /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci */;
        // That is fine for dump tooling, but when we split and execute statements ourselves it becomes invalid.
        // Normalize it into a single standard CREATE DATABASE statement before splitting.
        static string NormalizeMariaDumpSyntax(string content) {
            if (string.IsNullOrWhiteSpace(content)) return content;

            return Regex.Replace(
                content,
                @"CREATE\s+DATABASE\s+IF\s+NOT\s+EXISTS\s+`(?<db>[^`]+)`\s*;\s*/\*!\d+\s+(?<opts>.*?)\*/\s*;",
                m => $"CREATE DATABASE IF NOT EXISTS `{m.Groups["db"].Value}` {m.Groups["opts"].Value.Trim()};",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        static string[] SplitSqlStatements(string content) {
            if (string.IsNullOrWhiteSpace(content)) return Array.Empty<string>();

            var result = new List<string>();
            var sb = new StringBuilder(content.Length);

            var inSingleQuote = false;
            var inDoubleQuote = false;
            var inBacktick = false;
            var inLineComment = false;
            var inBlockComment = false;

            for (var i = 0; i < content.Length; i++) {
                var c = content[i];
                var next = i + 1 < content.Length ? content[i + 1] : '\0';

                if (inLineComment) {
                    sb.Append(c);
                    if (c == '\n') inLineComment = false;
                    continue;
                }

                if (inBlockComment) {
                    sb.Append(c);
                    if (c == '*' && next == '/') {
                        sb.Append(next);
                        i++;
                        inBlockComment = false;
                    }
                    continue;
                }

                if (inSingleQuote) {
                    sb.Append(c);
                    if (c == '\\' && next != '\0') {
                        sb.Append(next);
                        i++;
                        continue;
                    }
                    if (c == '\'') {
                        if (next == '\'') {
                            sb.Append(next);
                            i++;
                        } else {
                            inSingleQuote = false;
                        }
                    }
                    continue;
                }

                if (inDoubleQuote) {
                    sb.Append(c);
                    if (c == '\\' && next != '\0') {
                        sb.Append(next);
                        i++;
                        continue;
                    }
                    if (c == '"') {
                        if (next == '"') {
                            sb.Append(next);
                            i++;
                        } else {
                            inDoubleQuote = false;
                        }
                    }
                    continue;
                }

                if (inBacktick) {
                    sb.Append(c);
                    if (c == '`') {
                        if (next == '`') {
                            sb.Append(next);
                            i++;
                        } else {
                            inBacktick = false;
                        }
                    }
                    continue;
                }

                if (c == '-' && next == '-' && StartsLineComment(content, i + 2)) {
                    sb.Append(c);
                    sb.Append(next);
                    i++;
                    inLineComment = true;
                    continue;
                }

                if (c == '#') {
                    sb.Append(c);
                    inLineComment = true;
                    continue;
                }

                if (c == '/' && next == '*') {
                    sb.Append(c);
                    sb.Append(next);
                    i++;
                    inBlockComment = true;
                    continue;
                }

                if (c == '\'') {
                    sb.Append(c);
                    inSingleQuote = true;
                    continue;
                }

                if (c == '"') {
                    sb.Append(c);
                    inDoubleQuote = true;
                    continue;
                }

                if (c == '`') {
                    sb.Append(c);
                    inBacktick = true;
                    continue;
                }

                if (c == ';') {
                    var statement = sb.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(statement)) result.Add(statement);
                    sb.Clear();
                    continue;
                }

                sb.Append(c);
            }

            var tail = sb.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(tail)) result.Add(tail);
            return result.ToArray();
        }

        static bool StartsLineComment(string content, int index) {
            if (index >= content.Length) return true;
            var c = content[index];
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }
    }
}
