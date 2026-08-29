using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using YoutubeAPI.Cli.Models;
using YoutubeAPI.Cli.Options;
using YoutubeAPI.Cli.Serialization;
using YoutubeAPI.Models.Continuations;

namespace YoutubeAPI.Cli.Formatting;

public static class OutputFormatter
{
    private static void WriteJson<T>(T value, JsonTypeInfo<T> typeInfo, bool pretty)
    {
        var stream = Console.OpenStandardOutput();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = pretty });
        JsonSerializer.Serialize(writer, value, typeInfo);
        writer.Flush();
        Console.Out.WriteLine();
    }

    private static void WriteNdjsonLine<T>(T value, JsonTypeInfo<T> typeInfo)
    {
        var json = JsonSerializer.Serialize(value, typeInfo);
        Console.Out.WriteLine(json);
    }

    public static void WriteError(string format, string type, string message)
    {
        if (string.Equals(format, "table", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Error ({type}): {message}");
        }
        else
        {
            var errorEnvelope = new CliErrorEnvelope(new CliError(type, message));
            var json = JsonSerializer.Serialize(errorEnvelope, CliJsonContext.Default.CliErrorEnvelope);
            Console.Error.WriteLine(json);
        }
    }

    public static void ExecuteSingle<T>(
        GlobalOptions options,
        T value,
        JsonTypeInfo<T> typeInfo,
        Action<T, TextWriter> renderTable)
    {
        if (string.Equals(options.Format, "table", StringComparison.OrdinalIgnoreCase))
            renderTable(value, Console.Out);
        else if (string.Equals(options.Format, "ndjson", StringComparison.OrdinalIgnoreCase))
            WriteNdjsonLine(value, typeInfo);
        else
            WriteJson(value, typeInfo, options.Pretty);
    }

    public static void ExecuteList<T>(
        GlobalOptions options,
        IReadOnlyList<T> list,
        JsonTypeInfo<IReadOnlyList<T>> listTypeInfo,
        JsonTypeInfo<T> itemTypeInfo,
        Action<IReadOnlyList<T>, TextWriter> renderTable)
    {
        if (string.Equals(options.Format, "table", StringComparison.OrdinalIgnoreCase))
            renderTable(list, Console.Out);
        else if (string.Equals(options.Format, "ndjson", StringComparison.OrdinalIgnoreCase))
            foreach (var item in list)
                WriteNdjsonLine(item, itemTypeInfo);
        else
            WriteJson(list, listTypeInfo, options.Pretty);
    }

    public static async Task ExecuteCollectionAsync<TItem, TContinuation>(
        GlobalOptions options,
        Func<CancellationToken, Task<Page<TItem, TContinuation>>> fetchFirstPage,
        Func<TContinuation, CancellationToken, Task<Page<TItem, TContinuation>>> fetchNextPage,
        TContinuation? initialContinuation,
        JsonTypeInfo<PageEnvelope<TItem>> pageTypeInfo,
        JsonTypeInfo<TItem> itemTypeInfo,
        Action<IReadOnlyList<TItem>, string?, TextWriter> renderTable,
        Func<TContinuation, string> exportContinuation,
        CancellationToken cancellationToken)
        where TContinuation : class
    {
        var maxPages = options.All ? int.MaxValue : options.Pages;
        var isNdjson = string.Equals(options.Format, "ndjson", StringComparison.OrdinalIgnoreCase);
        var isTable = string.Equals(options.Format, "table", StringComparison.OrdinalIgnoreCase);

        var allItems = isNdjson ? null : new List<TItem>();

        Page<TItem, TContinuation> currentPage;
        if (initialContinuation is not null)
            currentPage = await fetchNextPage(initialContinuation, cancellationToken);
        else
            currentPage = await fetchFirstPage(cancellationToken);

        var pagesFetched = 1;

        if (isNdjson)
        {
            foreach (var item in currentPage.Items) WriteNdjsonLine(item, itemTypeInfo);
            await Console.Out.FlushAsync(cancellationToken);
        }
        else
        {
            allItems!.AddRange(currentPage.Items);
        }

        while (pagesFetched < maxPages && currentPage.Next is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            currentPage = await fetchNextPage(currentPage.Next, cancellationToken);
            pagesFetched++;

            if (isNdjson)
            {
                foreach (var item in currentPage.Items) WriteNdjsonLine(item, itemTypeInfo);
                await Console.Out.FlushAsync(cancellationToken);
            }
            else
            {
                allItems!.AddRange(currentPage.Items);
            }
        }

        if (isNdjson)
        {
            if (currentPage.Next is not null)
            {
                var export = exportContinuation(currentPage.Next);
                var control = new NdjsonContinuationControl(export);
                WriteNdjsonLine(control, CliJsonContext.Default.NdjsonContinuationControl);
            }
        }
        else
        {
            var nextExport = currentPage.Next is not null ? exportContinuation(currentPage.Next) : null;
            if (isTable)
            {
                renderTable(allItems!, nextExport, Console.Out);
            }
            else
            {
                var envelope = new PageEnvelope<TItem>(allItems!, nextExport);
                WriteJson(envelope, pageTypeInfo, options.Pretty);
            }
        }
    }
}