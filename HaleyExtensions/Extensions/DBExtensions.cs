using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Haley.Utils;

public static class DBExtensions
{
    public static IAdapterArgs SetFilter(this IAdapterArgs args, ResultFilter filter)
    {
        if (args is null) return args;
        args.Filter = filter;
        return args;
    }

    public static IAdapterArgs SetOutputName(this IAdapterArgs args, string outputName)
    {
        if (args is null) return args;
        args.OutputName = outputName;
        return args;
    }

    public static IAdapterArgs ToAdapterArgs(
        this Dictionary<string, object> input,
        string adapterKey,
        string query)
    {
        if (input is null || input.Count == 0)
            throw new ArgumentNullException(nameof(input), "Input cannot be null or empty for conversion.");
        var result = new AdapterArgs(adapterKey) { Query = query };
        result.SetParameters(input);
        return result;
    }

    public static IAdapterArgs ToAdapterArgs(this Dictionary<string, object> input, string query)
    {
        if (input is null || input.Count == 0)
            throw new ArgumentNullException(nameof(input), "Input cannot be null or empty for conversion.");
        var result = new AdapterArgs { Query = query };
        result.SetParameters(input);
        return result;
    }

    public static IModuleArgs ToModuleArgs(this Dictionary<string, object> input)
    {
        if (input is null || input.Count == 0)
            throw new ArgumentNullException(nameof(input), "Input cannot be null or empty for conversion.");
        return new ModuleArgs().SetParameters(input);
    }

    public static IAdapterArgs ToAdapterArgs(this IParameterBase input) =>
        input.ToAdapterArgs(string.Empty);

    public static IAdapterArgs ToAdapterArgs(this IParameterBase input, string query) =>
        input.ToAdapterArgs(query, string.Empty);

    public static IAdapterArgs ToAdapterArgs(this IParameterBase input, string query, string groupKey)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        var result = new AdapterArgs(input.Key) { Query = query };
        var parameters = string.IsNullOrWhiteSpace(groupKey)
            ? input.Parameters
            : input.GetGroupParameters(groupKey);
        result.SetParameters(parameters.ToDictionary(pair => pair.Key, pair => pair.Value));
        if (input is ModuleArgs module)
        {
            result.Adapter = module.Adapter;
            result.TransactionMode = module.TransactionMode;
        }
        return result;
    }

    public static IAdapterConfig? AsAdapterConfig(this string input) =>
        string.IsNullOrWhiteSpace(input) ? null : new AdapterConfig();

    public static P ForTransaction<P>(
        this IModuleArgs input,
        ITransactionHandler handler,
        bool throwInvalid = true)
        where P : IModuleArgs => (P)input.ForTransaction(handler, throwInvalid);

    public static IModuleArgs ForTransaction(
        this IModuleArgs input,
        ITransactionHandler handler,
        bool throwInvalid = true)
    {
        if (handler is not null) return handler.CreateDBInput(input);
        if (throwInvalid)
            throw new ArgumentNullException(nameof(handler),
                "Handler is required to include transaction information.");
        return input;
    }

    public static IAdapterArgs ForTransaction(
        this IAdapterArgs input,
        ITransactionHandler handler,
        bool throwInvalid = true)
    {
        if (handler is null)
        {
            if (throwInvalid)
                throw new ArgumentNullException(nameof(handler),
                    "Handler is required to include transaction information.");
            return input;
        }
        if (input is AdapterArgs adapterArgs && handler is IDBAdapter adapter)
        {
            adapterArgs.Adapter = adapter;
            adapterArgs.TransactionMode = true;
        }
        return input;
    }

    public static P ForTransaction<P>(
        this IAdapterArgs input,
        ITransactionHandler handler,
        bool throwInvalid = true)
        where P : IAdapterArgs => (P)input.ForTransaction(handler, throwInvalid);
}
