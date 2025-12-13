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
                if (args == null || string.IsNullOrWhiteSpace(args.Key)) throw new ArgumentNullException("Key cannot be empty.");
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

            var exists = await agw.Scalar(new AdapterArgs(args.Key) { ExcludeDBInConString = true, Query = QRY_MARIA.SCHEMA_EXISTS }, (NAME, args.DBName));
            if (exists != null && exists.IsNumericType()) return fb.SetStatus(true).SetMessage($@"Database {args.DBName} already exists. No action taken.").SetCode((int)HttpStatusCode.Continue);

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

            //?? Should we run everything in one go or run as separate statements???
            //if the input contains any delimiter or procedure, remove them.

            object queryContent = content;
            List<string> procedures = new List<string>();
            //if (content.Contains("Delimiter", StringComparison.InvariantCultureIgnoreCase)) //Doesn't work in .net standard.
            if (content?.IndexOf("Delimiter", StringComparison.OrdinalIgnoreCase) >= 0) {
                //Step 1 : Remove delimiter lines
                content = Regex.Replace(content, @"DELIMITER\s+\S+", "", RegexOptions.IgnoreCase); //Remove the delimiter comments

                //Step 2 : Remove version-specific comments
                content = Regex.Replace(content, @"/\*!.*?\*/;", "", RegexOptions.Singleline);
                //Step 3 : Extract all Procedures
                string pattern = @"CREATE\s+PROCEDURE.*?END\s*//";
                var matches = Regex.Matches(content, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

                foreach (Match match in matches) {
                    string proc = match.Value;
                    proc = proc.Replace("//", ";").Trim();
                    procedures.Add(proc);
                    content = content.Replace(match.Value, "");
                }
                // Step 4: Split remaining SQL by semicolon
                queryContent = Regex.Split(content, @";\s*(?=\n|$)", RegexOptions.Multiline);
                //queryContent = Regex.Split(content, @";\s*(?=\n|$)", RegexOptions.Multiline);
            }

            var handler = agw.GetTransactionHandler(args.Key);
            using (handler.Begin(true)) {
                await agw.NonQuery(new AdapterArgs(args.Key) { ExcludeDBInConString = true, Query = queryContent }.ForTransaction(handler));
                if (procedures.Count > 0) {
                    await agw.NonQuery(new AdapterArgs(args.Key) { ExcludeDBInConString = true, Query = procedures.ToArray() }.ForTransaction(handler));
                }
            }

            return fb.SetStatus(true).SetCode((int)HttpStatusCode.Created);
        }
    }
}
