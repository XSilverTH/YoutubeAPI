using System.CommandLine;
using YoutubeAPI.Cli.Formatting;
using YoutubeAPI.Cli.Models;
using YoutubeAPI.Cli.Options;
using YoutubeAPI.Cli.Serialization;
using YoutubeAPI.Exceptions;
using YoutubeAPI.Models.Continuations;
using YoutubeAPI.Models.Enums;
using YoutubeAPI.Models.Playlists;
using YoutubeAPI.Models.Search;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, e) =>
        {
            e.Cancel = true;
            try
            {
                // The handler is detached before cts is disposed in the finally block below.
                // ReSharper disable once AccessToDisposedClosure
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The cancellation callback may race with application shutdown.
            }
        };
        Console.CancelKeyPress += cancelHandler;

        var format = GetFormatEarly(args);

        try
        {
            var defs = GlobalOptions.CreateDefinitions();
            var rootCommand = BuildCommandTree(defs, cts.Token);

            var parseResult = rootCommand.Parse(args);
            if (parseResult.Errors.Count <= 0) return await parseResult.InvokeAsync(null, cts.Token);
            var message = string.Join("; ", parseResult.Errors.Select(e => e.Message));
            OutputFormatter.WriteError(format, "UsageError", message);
            return 2;
        }
        catch (Exception ex)
        {
            return HandleException(format, ex);
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
            cts.Dispose();
        }
    }

    private static int HandleException(string format, Exception ex)
    {
        switch (ex)
        {
            case OperationCanceledException:
                OutputFormatter.WriteError(format, "OperationCanceledException", "Operation was canceled.");
                return 130;
            case AuthenticationRequiredException authenticationRequired:
                OutputFormatter.WriteError(format, nameof(AuthenticationRequiredException),
                    authenticationRequired.Message);
                return 3;
            case AuthenticationExpiredException authenticationExpired:
                OutputFormatter.WriteError(format, nameof(AuthenticationExpiredException),
                    authenticationExpired.Message);
                return 3;
            case PermissionDeniedException permissionDenied:
                OutputFormatter.WriteError(format, nameof(PermissionDeniedException), permissionDenied.Message);
                return 3;
            case ResourceNotFoundException notFound:
                OutputFormatter.WriteError(format, nameof(ResourceNotFoundException), notFound.Message);
                return 4;
            case ResourceUnavailableException unavailable:
                OutputFormatter.WriteError(format, nameof(ResourceUnavailableException), unavailable.Message);
                return 4;
            case CommentsUnavailableException commentsUnavailable:
                OutputFormatter.WriteError(format, nameof(CommentsUnavailableException), commentsUnavailable.Message);
                return 4;
            case RateLimitedException rateLimited:
                OutputFormatter.WriteError(format, nameof(RateLimitedException), rateLimited.Message);
                return 5;
            case YouTubeRequestException request:
                OutputFormatter.WriteError(format, nameof(YouTubeRequestException), request.Message);
                return 5;
            case YouTubeProtocolException protocol:
                OutputFormatter.WriteError(format, nameof(YouTubeProtocolException), protocol.Message);
                return 5;
            case HttpRequestException http:
                OutputFormatter.WriteError(format, nameof(HttpRequestException), http.Message);
                return 5;
            case YouTubeException youtube:
                OutputFormatter.WriteError(format, youtube.GetType().Name, youtube.Message);
                return 5;
            case ArgumentException argument:
                OutputFormatter.WriteError(format, "UsageError", argument.Message);
                return 2;
            case FormatException formatException:
                OutputFormatter.WriteError(format, "UsageError", formatException.Message);
                return 2;
            case InvalidOperationException invalidOperation:
                OutputFormatter.WriteError(format, "UsageError", invalidOperation.Message);
                return 2;
            default:
                OutputFormatter.WriteError(format, ex.GetType().Name, ex.Message);
                return 1;
        }
    }

    private static string GetFormatEarly(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--format", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1].ToLowerInvariant();
            if (args[i].StartsWith("--format=", StringComparison.OrdinalIgnoreCase))
                return args[i][9..].ToLowerInvariant();
        }

        return "json";
    }

    private static async Task<int> ExecuteActionAsync(string format, Func<Task<int>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            return HandleException(format, ex);
        }
    }

    private static RootCommand BuildCommandTree(OptionDefinitions defs, CancellationToken rootCt)
    {
        var root = new RootCommand("YouTube API CLI - Native-AOT command-line frontend for YouTube data.");
        defs.AddToCommand(root);

        // 1. video
        root.Subcommands.Add(BuildVideoCommand(defs, rootCt));

        // 2. search
        root.Subcommands.Add(BuildSearchCommand(defs, rootCt));

        // 3. suggestions
        root.Subcommands.Add(BuildSuggestionsCommand(defs, rootCt));

        // 4. channel
        root.Subcommands.Add(BuildChannelCommand(defs, rootCt));

        // 5. playlist
        root.Subcommands.Add(BuildPlaylistCommand(defs, rootCt));

        // 6. comments
        root.Subcommands.Add(BuildCommentsCommand(defs, rootCt));

        // 7. feed
        root.Subcommands.Add(BuildFeedCommand(defs, rootCt));

        // 8. account
        root.Subcommands.Add(BuildAccountCommand(defs, rootCt));

        // 9. rating
        root.Subcommands.Add(BuildRatingCommand(defs, rootCt));

        // 10. history
        root.Subcommands.Add(BuildHistoryCommand(defs, rootCt));

        return root;
    }

    private static YouTubeClient CreateClient(GlobalOptions options)
    {
        YouTubeCookieAuthentication? auth = null;
        if (!string.IsNullOrWhiteSpace(options.CookiesPath))
        {
            if (!File.Exists(options.CookiesPath))
                throw new ArgumentException($"Cookie file not found: {options.CookiesPath}");

            using var stream = File.OpenRead(options.CookiesPath);
            auth = YouTubeCookieAuthentication.FromNetscape(stream);
        }

        var clientOptions = new YouTubeClientOptions
        {
            Language = options.Language,
            Region = options.Region,
            Authentication = auth,
            VisitorData = options.VisitorData,
            RolloutToken = options.RolloutToken,
            ProofOfOriginToken = options.ProofOfOriginToken,
            AuthUser = options.AuthUser,
            PageId = options.PageId
        };

        return new YouTubeClient(clientOptions);
    }

    private static void ValidateOptions(GlobalOptions options, bool isCollection)
    {
        if (options is { All: true, HasExplicitPages: true })
            throw new ArgumentException("--all and --pages are mutually exclusive.");

        if (options.Pages <= 0) throw new ArgumentException("--pages must be a positive integer.");

        if (!string.Equals(options.Format, "json", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(options.Format, "ndjson", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(options.Format, "table", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Invalid format '{options.Format}'. Valid formats are json, ndjson, table.");

        if (isCollection) return;
        if (options.HasExplicitContinuation)
            throw new ArgumentException("--continuation is only supported for collection commands.");

        if (options.All || options.HasExplicitPages)
            throw new ArgumentException(
                "Pagination options (--pages, --all) are only supported for collection commands.");
    }

    // =========================================================================
    // 1. video
    // =========================================================================
    private static Command BuildVideoCommand(OptionDefinitions defs, CancellationToken rootCt)
    {
        var videoCmd = new Command("video", "Video metadata and transcript operations.");

        // video info <video>
        var infoCmd = new Command("info", "Retrieve full video metadata.");
        var videoArg = new Argument<string>("video") { Description = "Video ID or YouTube URL." };
        infoCmd.Arguments.Add(videoArg);
        infoCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, false);

                var videoRaw = parseResult.GetValue(videoArg);
                if (string.IsNullOrWhiteSpace(videoRaw))
                    throw new ArgumentException("Video identifier is required.");

                var videoId = VideoId.Parse(videoRaw);
                using var client = CreateClient(options);
                var video = await client.Videos.GetAsync(videoId, linked.Token);

                OutputFormatter.ExecuteSingle(
                    options,
                    video,
                    CliJsonContext.Default.Video,
                    TableFormatter.RenderVideo);

                return 0;
            });
        });
        videoCmd.Subcommands.Add(infoCmd);

        // video transcripts <video>
        var transcriptsCmd = new Command("transcripts", "List available transcript tracks for a video.");
        var transcriptsVideoArg = new Argument<string>("video") { Description = "Video ID or YouTube URL." };
        transcriptsCmd.Arguments.Add(transcriptsVideoArg);
        transcriptsCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, false);

                var videoRaw = parseResult.GetValue(transcriptsVideoArg);
                if (string.IsNullOrWhiteSpace(videoRaw))
                    throw new ArgumentException("Video identifier is required.");

                var videoId = VideoId.Parse(videoRaw);
                using var client = CreateClient(options);
                var tracks = await client.Videos.GetTranscriptTracksAsync(videoId, linked.Token);

                OutputFormatter.ExecuteList(
                    options,
                    tracks,
                    CliJsonContext.Default.IReadOnlyListTranscriptTrack,
                    CliJsonContext.Default.TranscriptTrack,
                    TableFormatter.RenderTranscriptTracks);

                return 0;
            });
        });
        videoCmd.Subcommands.Add(transcriptsCmd);

        // video transcript <video> --track <track-id>
        var transcriptCmd = new Command("transcript", "Retrieve timed cues for a specific transcript track.");
        var transcriptVideoArg = new Argument<string>("video") { Description = "Video ID or YouTube URL." };
        var trackOption = new Option<string>("--track") { Description = "Transcript track ID.", Required = true };
        transcriptCmd.Arguments.Add(transcriptVideoArg);
        transcriptCmd.Options.Add(trackOption);
        transcriptCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, false);

                var videoRaw = parseResult.GetValue(transcriptVideoArg);
                if (string.IsNullOrWhiteSpace(videoRaw))
                    throw new ArgumentException("Video identifier is required.");

                var trackRaw = parseResult.GetValue(trackOption);
                if (string.IsNullOrWhiteSpace(trackRaw))
                    throw new ArgumentException("--track option is required.");

                var videoId = VideoId.Parse(videoRaw);
                var trackId = TranscriptTrackId.Parse(trackRaw);
                using var client = CreateClient(options);
                var transcript = await client.Videos.GetTranscriptAsync(videoId, trackId, linked.Token);

                OutputFormatter.ExecuteSingle(
                    options,
                    transcript,
                    CliJsonContext.Default.Transcript,
                    TableFormatter.RenderTranscript);

                return 0;
            });
        });
        videoCmd.Subcommands.Add(transcriptCmd);

        return videoCmd;
    }

    // =========================================================================
    // 2. search
    // =========================================================================
    private static Command BuildSearchCommand(OptionDefinitions defs, CancellationToken rootCt)
    {
        var searchCmd = new Command("search", "Search YouTube for videos, channels, and playlists.");
        var queryArg = new Argument<string?>("query")
            { Arity = ArgumentArity.ZeroOrOne, Description = "Search query string." };
        var kindOpt = new Option<string>("--kind")
            { DefaultValueFactory = _ => "all", Description = "Filter search results: all, video, channel, playlist." };
        searchCmd.Arguments.Add(queryArg);
        searchCmd.Options.Add(kindOpt);

        searchCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, true);

                var queryResult = parseResult.GetResult(queryArg);
                var hasExplicitQuery = queryResult is not null && !queryResult.Implicit &&
                                       !string.IsNullOrWhiteSpace(parseResult.GetValue(queryArg));

                var kindResult = parseResult.GetResult(kindOpt);
                var hasExplicitKind = kindResult is not null && !kindResult.Implicit;

                SearchContinuation? initialContinuation = null;
                SearchRequest? initialRequest = null;

                if (options.HasExplicitContinuation)
                {
                    if (hasExplicitQuery)
                        throw new ArgumentException(
                            "Search query cannot be specified when resuming with a continuation token.");
                    if (hasExplicitKind)
                        throw new ArgumentException(
                            "--kind cannot be specified when resuming with a continuation token.");

                    initialContinuation = SearchContinuation.Import(options.Continuation!);
                }
                else
                {
                    var query = parseResult.GetValue(queryArg);
                    if (string.IsNullOrWhiteSpace(query))
                        throw new ArgumentException(
                            "Search query is required when not resuming with a continuation token.");

                    var kindStr = parseResult.GetValue(kindOpt) ?? "all";
                    if (!Enum.TryParse<SearchKind>(kindStr, true, out var kind))
                        throw new ArgumentException(
                            $"Invalid kind '{kindStr}'. Valid values: all, video, channel, playlist.");

                    initialRequest = new SearchRequest(query, kind);
                }

                using var client = CreateClient(options);

                await OutputFormatter.ExecuteCollectionAsync(
                    options,
                    token => client.Search.GetPageAsync(initialRequest!, token),
                    (cont, token) => client.Search.GetPageAsync(cont, token),
                    initialContinuation,
                    CliJsonContext.Default.PageEnvelopeSearchResult,
                    CliJsonContext.Default.SearchResult,
                    TableFormatter.RenderSearchResults,
                    cont => cont.Export(),
                    linked.Token);

                return 0;
            });
        });

        return searchCmd;
    }

    // =========================================================================
    // 3. suggestions
    // =========================================================================
    private static Command BuildSuggestionsCommand(OptionDefinitions defs, CancellationToken rootCt)
    {
        var suggestionsCmd = new Command("suggestions", "Retrieve search query auto-completion suggestions.");
        var queryArg = new Argument<string>("query") { Description = "Query prefix string." };
        suggestionsCmd.Arguments.Add(queryArg);

        suggestionsCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, false);

                var query = parseResult.GetValue(queryArg);
                if (string.IsNullOrWhiteSpace(query))
                    throw new ArgumentException("Query prefix is required.");

                using var client = CreateClient(options);
                var suggestions = await client.Suggestions.GetAsync(query, linked.Token);

                OutputFormatter.ExecuteList(
                    options,
                    suggestions,
                    CliJsonContext.Default.IReadOnlyListString,
                    CliJsonContext.Default.String,
                    TableFormatter.RenderSuggestions);

                return 0;
            });
        });

        return suggestionsCmd;
    }

    // =========================================================================
    // 4. channel
    // =========================================================================
    private static Command BuildChannelCommand(OptionDefinitions defs, CancellationToken rootCt)
    {
        var channelCmd = new Command("channel", "Channel metadata, videos, and playlist operations.");

        // channel info <channel>
        var infoCmd = new Command("info", "Retrieve full channel metadata.");
        var channelArg = new Argument<string>("channel") { Description = "Channel ID, handle (@username), or URL." };
        infoCmd.Arguments.Add(channelArg);
        infoCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, false);

                var channelRaw = parseResult.GetValue(channelArg);
                if (string.IsNullOrWhiteSpace(channelRaw))
                    throw new ArgumentException("Channel reference is required.");

                var channelRef = ChannelReference.Parse(channelRaw);
                using var client = CreateClient(options);
                var channel = await client.Channels.GetAsync(channelRef, linked.Token);

                OutputFormatter.ExecuteSingle(
                    options,
                    channel,
                    CliJsonContext.Default.Channel,
                    TableFormatter.RenderChannel);

                return 0;
            });
        });
        channelCmd.Subcommands.Add(infoCmd);

        // channel videos [<channel>] --sort newest|popular|oldest
        var videosCmd = new Command("videos", "Retrieve videos uploaded by a channel.");
        var videosChannelArg = new Argument<string?>("channel")
            { Arity = ArgumentArity.ZeroOrOne, Description = "Channel reference." };
        var sortOpt = new Option<string>("--sort")
            { DefaultValueFactory = _ => "newest", Description = "Sort order: newest, popular, oldest." };
        videosCmd.Arguments.Add(videosChannelArg);
        videosCmd.Options.Add(sortOpt);
        videosCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, true);

                var channelResult = parseResult.GetResult(videosChannelArg);
                var hasExplicitChannel = channelResult is not null && !channelResult.Implicit &&
                                         !string.IsNullOrWhiteSpace(parseResult.GetValue(videosChannelArg));

                var sortResult = parseResult.GetResult(sortOpt);
                var hasExplicitSort = sortResult is not null && !sortResult.Implicit;

                ChannelVideosContinuation? initialContinuation = null;
                ChannelReference? channelRef = null;
                var sortOrder = ChannelVideoSort.Newest;

                if (options.HasExplicitContinuation)
                {
                    if (hasExplicitChannel)
                        throw new ArgumentException(
                            "Channel reference cannot be specified when resuming with a continuation token.");
                    if (hasExplicitSort)
                        throw new ArgumentException(
                            "--sort cannot be specified when resuming with a continuation token.");

                    initialContinuation = ChannelVideosContinuation.Import(options.Continuation!);
                }
                else
                {
                    var channelRaw = parseResult.GetValue(videosChannelArg);
                    if (string.IsNullOrWhiteSpace(channelRaw))
                        throw new ArgumentException(
                            "Channel reference is required when not resuming with a continuation token.");

                    channelRef = ChannelReference.Parse(channelRaw);

                    var sortStr = parseResult.GetValue(sortOpt) ?? "newest";
                    if (!Enum.TryParse(sortStr, true, out sortOrder))
                        throw new ArgumentException(
                            $"Invalid sort '{sortStr}'. Valid values: newest, popular, oldest.");
                }

                using var client = CreateClient(options);

                await OutputFormatter.ExecuteCollectionAsync(
                    options,
                    token => client.Channels.GetVideosPageAsync(channelRef!.Value, sortOrder, token),
                    (cont, token) => client.Channels.GetVideosPageAsync(cont, token),
                    initialContinuation,
                    CliJsonContext.Default.PageEnvelopeVideoSummary,
                    CliJsonContext.Default.VideoSummary,
                    TableFormatter.RenderVideos,
                    cont => cont.Export(),
                    linked.Token);

                return 0;
            });
        });
        channelCmd.Subcommands.Add(videosCmd);

        // channel playlists [<channel>]
        var playlistsCmd = new Command("playlists", "Retrieve public playlists created by a channel.");
        var playlistsChannelArg = new Argument<string?>("channel")
            { Arity = ArgumentArity.ZeroOrOne, Description = "Channel reference." };
        playlistsCmd.Arguments.Add(playlistsChannelArg);
        playlistsCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, true);

                var channelResult = parseResult.GetResult(playlistsChannelArg);
                var hasExplicitChannel = channelResult is not null && !channelResult.Implicit &&
                                         !string.IsNullOrWhiteSpace(parseResult.GetValue(playlistsChannelArg));

                ChannelPlaylistsContinuation? initialContinuation = null;
                ChannelReference? channelRef = null;

                if (options.HasExplicitContinuation)
                {
                    if (hasExplicitChannel)
                        throw new ArgumentException(
                            "Channel reference cannot be specified when resuming with a continuation token.");

                    initialContinuation = ChannelPlaylistsContinuation.Import(options.Continuation!);
                }
                else
                {
                    var channelRaw = parseResult.GetValue(playlistsChannelArg);
                    if (string.IsNullOrWhiteSpace(channelRaw))
                        throw new ArgumentException(
                            "Channel reference is required when not resuming with a continuation token.");

                    channelRef = ChannelReference.Parse(channelRaw);
                }

                using var client = CreateClient(options);

                await OutputFormatter.ExecuteCollectionAsync(
                    options,
                    token => client.Channels.GetPlaylistsPageAsync(channelRef!.Value, token),
                    (cont, token) => client.Channels.GetPlaylistsPageAsync(cont, token),
                    initialContinuation,
                    CliJsonContext.Default.PageEnvelopePlaylistSummary,
                    CliJsonContext.Default.PlaylistSummary,
                    TableFormatter.RenderPlaylists,
                    cont => cont.Export(),
                    linked.Token);

                return 0;
            });
        });
        channelCmd.Subcommands.Add(playlistsCmd);

        return channelCmd;
    }

    // =========================================================================
    // 5. playlist
    // =========================================================================
    private static Command BuildPlaylistCommand(OptionDefinitions defs, CancellationToken rootCt)
    {
        var playlistCmd = new Command("playlist", "Playlist querying, creation, and management operations.");

        // playlist info <playlist>
        var infoCmd = new Command("info", "Retrieve full playlist metadata.");
        var playlistArg = new Argument<string>("playlist") { Description = "Playlist ID or URL." };
        infoCmd.Arguments.Add(playlistArg);
        infoCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, false);

                var playlistRaw = parseResult.GetValue(playlistArg);
                if (string.IsNullOrWhiteSpace(playlistRaw))
                    throw new ArgumentException("Playlist identifier is required.");

                var playlistId = PlaylistId.Parse(playlistRaw);
                using var client = CreateClient(options);
                var playlist = await client.Playlists.GetAsync(playlistId, linked.Token);

                OutputFormatter.ExecuteSingle(
                    options,
                    playlist,
                    CliJsonContext.Default.Playlist,
                    TableFormatter.RenderPlaylist);

                return 0;
            });
        });
        playlistCmd.Subcommands.Add(infoCmd);

        // playlist items [<playlist>]
        var itemsCmd = new Command("items", "Retrieve items contained in a playlist.");
        var itemsPlaylistArg = new Argument<string?>("playlist")
            { Arity = ArgumentArity.ZeroOrOne, Description = "Playlist ID or URL." };
        itemsCmd.Arguments.Add(itemsPlaylistArg);
        itemsCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, true);

                var playlistResult = parseResult.GetResult(itemsPlaylistArg);
                var hasExplicitPlaylist = playlistResult is not null && !playlistResult.Implicit &&
                                          !string.IsNullOrWhiteSpace(parseResult.GetValue(itemsPlaylistArg));

                PlaylistItemsContinuation? initialContinuation = null;
                PlaylistId? playlistId = null;

                if (options.HasExplicitContinuation)
                {
                    if (hasExplicitPlaylist)
                        throw new ArgumentException(
                            "Playlist ID cannot be specified when resuming with a continuation token.");

                    initialContinuation = PlaylistItemsContinuation.Import(options.Continuation!);
                }
                else
                {
                    var playlistRaw = parseResult.GetValue(itemsPlaylistArg);
                    if (string.IsNullOrWhiteSpace(playlistRaw))
                        throw new ArgumentException(
                            "Playlist ID is required when not resuming with a continuation token.");

                    playlistId = PlaylistId.Parse(playlistRaw);
                }

                using var client = CreateClient(options);

                await OutputFormatter.ExecuteCollectionAsync(
                    options,
                    token => client.Playlists.GetItemsPageAsync(playlistId!.Value, token),
                    (cont, token) => client.Playlists.GetItemsPageAsync(cont, token),
                    initialContinuation,
                    CliJsonContext.Default.PageEnvelopePlaylistItem,
                    CliJsonContext.Default.PlaylistItem,
                    TableFormatter.RenderPlaylistItems,
                    cont => cont.Export(),
                    linked.Token);

                return 0;
            });
        });
        playlistCmd.Subcommands.Add(itemsCmd);

        // playlist mine
        var mineCmd = new Command("mine", "Retrieve playlists owned by the authenticated user.");
        mineCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, true);

                var initialContinuation = options.HasExplicitContinuation
                    ? OwnedPlaylistsContinuation.Import(options.Continuation!)
                    : null;

                using var client = CreateClient(options);

                await OutputFormatter.ExecuteCollectionAsync(
                    options,
                    token => client.Playlists.GetMinePageAsync(token),
                    (cont, token) => client.Playlists.GetMinePageAsync(cont, token),
                    initialContinuation,
                    CliJsonContext.Default.PageEnvelopePlaylistSummary,
                    CliJsonContext.Default.PlaylistSummary,
                    TableFormatter.RenderPlaylists,
                    cont => cont.Export(),
                    linked.Token);

                return 0;
            });
        });
        playlistCmd.Subcommands.Add(mineCmd);

        // playlist create --title <text> [--description <text>] [--privacy private|unlisted|public]
        var createCmd = new Command("create", "Create a new playlist.");
        var titleOpt = new Option<string>("--title") { Description = "Playlist title.", Required = true };
        var descOpt = new Option<string?>("--description") { Description = "Playlist description." };
        var privacyOpt = new Option<string>("--privacy")
            { DefaultValueFactory = _ => "private", Description = "Playlist privacy: private, unlisted, public." };
        createCmd.Options.Add(titleOpt);
        createCmd.Options.Add(descOpt);
        createCmd.Options.Add(privacyOpt);
        createCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, false);

                var title = parseResult.GetValue(titleOpt);
                if (string.IsNullOrWhiteSpace(title))
                    throw new ArgumentException("--title is required.");

                var desc = parseResult.GetValue(descOpt);
                var privacyStr = parseResult.GetValue(privacyOpt) ?? "private";
                if (!Enum.TryParse<PlaylistPrivacy>(privacyStr, true, out var privacy))
                    throw new ArgumentException(
                        $"Invalid privacy '{privacyStr}'. Valid values: private, unlisted, public.");

                var req = new CreatePlaylistRequest(title, desc, privacy);
                using var client = CreateClient(options);
                var newId = await client.Playlists.CreateAsync(req, linked.Token);

                var result = new PlaylistCreateResult(newId);
                OutputFormatter.ExecuteSingle(
                    options,
                    result,
                    CliJsonContext.Default.PlaylistCreateResult,
                    TableFormatter.RenderPlaylistCreate);

                return 0;
            });
        });
        playlistCmd.Subcommands.Add(createCmd);

        // playlist delete <playlist>
        var deleteCmd = new Command("delete", "Delete a playlist owned by the authenticated user.");
        var deletePlaylistArg = new Argument<string>("playlist") { Description = "Playlist ID to delete." };
        deleteCmd.Arguments.Add(deletePlaylistArg);
        deleteCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, false);

                var playlistRaw = parseResult.GetValue(deletePlaylistArg);
                if (string.IsNullOrWhiteSpace(playlistRaw))
                    throw new ArgumentException("Playlist ID is required.");

                var playlistId = PlaylistId.Parse(playlistRaw);
                using var client = CreateClient(options);
                await client.Playlists.DeleteAsync(playlistId, linked.Token);

                var result = new PlaylistActionResult(true, playlistId);
                OutputFormatter.ExecuteSingle(
                    options,
                    result,
                    CliJsonContext.Default.PlaylistActionResult,
                    TableFormatter.RenderPlaylistAction);

                return 0;
            });
        });
        playlistCmd.Subcommands.Add(deleteCmd);

        // playlist add <playlist> <video>
        var addCmd = new Command("add", "Append a video to an owned playlist.");
        var addPlaylistArg = new Argument<string>("playlist") { Description = "Playlist ID." };
        var addVideoArg = new Argument<string>("video") { Description = "Video ID or URL to add." };
        addCmd.Arguments.Add(addPlaylistArg);
        addCmd.Arguments.Add(addVideoArg);
        addCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, false);

                var playlistRaw = parseResult.GetValue(addPlaylistArg);
                var videoRaw = parseResult.GetValue(addVideoArg);

                if (string.IsNullOrWhiteSpace(playlistRaw))
                    throw new ArgumentException("Playlist ID is required.");
                if (string.IsNullOrWhiteSpace(videoRaw))
                    throw new ArgumentException("Video ID is required.");

                var playlistId = PlaylistId.Parse(playlistRaw);
                var videoId = VideoId.Parse(videoRaw);

                using var client = CreateClient(options);
                await client.Playlists.AddVideoAsync(playlistId, videoId, linked.Token);

                var result = new PlaylistActionResult(true, playlistId, videoId);
                OutputFormatter.ExecuteSingle(
                    options,
                    result,
                    CliJsonContext.Default.PlaylistActionResult,
                    TableFormatter.RenderPlaylistAction);

                return 0;
            });
        });
        playlistCmd.Subcommands.Add(addCmd);

        // playlist remove <playlist> <playlist-item-id>
        var removeCmd = new Command("remove", "Remove a video occurrence from an owned playlist.");
        var removePlaylistArg = new Argument<string>("playlist") { Description = "Playlist ID." };
        var removeItemArg = new Argument<string>("playlist-item-id") { Description = "Playlist item occurrence ID." };
        removeCmd.Arguments.Add(removePlaylistArg);
        removeCmd.Arguments.Add(removeItemArg);
        removeCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, false);

                var playlistRaw = parseResult.GetValue(removePlaylistArg);
                var itemRaw = parseResult.GetValue(removeItemArg);

                if (string.IsNullOrWhiteSpace(playlistRaw))
                    throw new ArgumentException("Playlist ID is required.");
                if (string.IsNullOrWhiteSpace(itemRaw))
                    throw new ArgumentException("Playlist item ID is required.");

                var playlistId = PlaylistId.Parse(playlistRaw);
                var itemId = PlaylistItemId.Parse(itemRaw);

                using var client = CreateClient(options);
                await client.Playlists.RemoveItemAsync(playlistId, itemId, linked.Token);

                var result = new PlaylistActionResult(true, playlistId, ItemId: itemId);
                OutputFormatter.ExecuteSingle(
                    options,
                    result,
                    CliJsonContext.Default.PlaylistActionResult,
                    TableFormatter.RenderPlaylistAction);

                return 0;
            });
        });
        playlistCmd.Subcommands.Add(removeCmd);

        return playlistCmd;
    }

    // =========================================================================
    // 6. comments
    // =========================================================================
    private static Command BuildCommentsCommand(OptionDefinitions defs, CancellationToken rootCt)
    {
        var commentsCmd = new Command("comments", "Comment threads and replies querying operations.");

        // comments threads [<video>] --sort top|newest
        var threadsCmd = new Command("threads", "Retrieve top-level comment threads for a video.");
        var videoArg = new Argument<string?>("video")
            { Arity = ArgumentArity.ZeroOrOne, Description = "Video ID or URL." };
        var sortOpt = new Option<string>("--sort")
            { DefaultValueFactory = _ => "top", Description = "Sort order: top, newest." };
        threadsCmd.Arguments.Add(videoArg);
        threadsCmd.Options.Add(sortOpt);
        threadsCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, true);

                var videoResult = parseResult.GetResult(videoArg);
                var hasExplicitVideo = videoResult is not null && !videoResult.Implicit &&
                                       !string.IsNullOrWhiteSpace(parseResult.GetValue(videoArg));

                var sortResult = parseResult.GetResult(sortOpt);
                var hasExplicitSort = sortResult is not null && !sortResult.Implicit;

                CommentThreadsContinuation? initialContinuation = null;
                VideoId? videoId = null;
                var sortOrder = CommentSort.Top;

                if (options.HasExplicitContinuation)
                {
                    if (hasExplicitVideo)
                        throw new ArgumentException(
                            "Video ID cannot be specified when resuming with a continuation token.");
                    if (hasExplicitSort)
                        throw new ArgumentException(
                            "--sort cannot be specified when resuming with a continuation token.");

                    initialContinuation = CommentThreadsContinuation.Import(options.Continuation!);
                }
                else
                {
                    var videoRaw = parseResult.GetValue(videoArg);
                    if (string.IsNullOrWhiteSpace(videoRaw))
                        throw new ArgumentException(
                            "Video ID is required when not resuming with a continuation token.");

                    videoId = VideoId.Parse(videoRaw);

                    var sortStr = parseResult.GetValue(sortOpt) ?? "top";
                    if (!Enum.TryParse(sortStr, true, out sortOrder))
                        throw new ArgumentException($"Invalid sort '{sortStr}'. Valid values: top, newest.");
                }

                using var client = CreateClient(options);

                await OutputFormatter.ExecuteCollectionAsync(
                    options,
                    token => client.Comments.GetThreadsPageAsync(videoId!.Value, sortOrder, token),
                    (cont, token) => client.Comments.GetThreadsPageAsync(cont, token),
                    initialContinuation,
                    CliJsonContext.Default.PageEnvelopeCommentThread,
                    CliJsonContext.Default.CommentThread,
                    TableFormatter.RenderCommentThreads,
                    cont => cont.Export(),
                    linked.Token);

                return 0;
            });
        });
        commentsCmd.Subcommands.Add(threadsCmd);

        // comments replies --continuation <opaque-replies-continuation>
        var repliesCmd = new Command("replies", "Retrieve nested reply comments using a continuation token.");
        repliesCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, true);

                if (!options.HasExplicitContinuation)
                    throw new ArgumentException("--continuation is required for comments replies.");

                var initialContinuation = CommentRepliesContinuation.Import(options.Continuation!);

                using var client = CreateClient(options);

                await OutputFormatter.ExecuteCollectionAsync(
                    options,
                    _ => client.Comments.GetRepliesPageAsync(initialContinuation, linked.Token),
                    (cont, token) => client.Comments.GetRepliesPageAsync(cont, token),
                    initialContinuation,
                    CliJsonContext.Default.PageEnvelopeComment,
                    CliJsonContext.Default.Comment,
                    TableFormatter.RenderComments,
                    cont => cont.Export(),
                    linked.Token);

                return 0;
            });
        });
        commentsCmd.Subcommands.Add(repliesCmd);

        return commentsCmd;
    }

    // =========================================================================
    // 7. feed
    // =========================================================================
    private static Command BuildFeedCommand(OptionDefinitions defs, CancellationToken rootCt)
    {
        var feedCmd = new Command("feed", "User feeds operations: Home, Subscriptions, Channels, and History.");

        // feed home
        var homeCmd = new Command("home", "Retrieve items from the YouTube home feed.");
        homeCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, true);

                var initialContinuation = options.HasExplicitContinuation
                    ? HomeContinuation.Import(options.Continuation!)
                    : null;

                using var client = CreateClient(options);

                await OutputFormatter.ExecuteCollectionAsync(
                    options,
                    token => client.Feeds.GetHomePageAsync(token),
                    (cont, token) => client.Feeds.GetHomePageAsync(cont, token),
                    initialContinuation,
                    CliJsonContext.Default.PageEnvelopeFeedItem,
                    CliJsonContext.Default.FeedItem,
                    TableFormatter.RenderFeedItems,
                    cont => cont.Export(),
                    linked.Token);

                return 0;
            });
        });
        feedCmd.Subcommands.Add(homeCmd);

        // feed subscriptions
        var subsCmd = new Command("subscriptions", "Retrieve items from the authenticated user's subscriptions feed.");
        subsCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, true);

                var initialContinuation = options.HasExplicitContinuation
                    ? SubscriptionsContinuation.Import(options.Continuation!)
                    : null;

                using var client = CreateClient(options);

                await OutputFormatter.ExecuteCollectionAsync(
                    options,
                    token => client.Feeds.GetSubscriptionsPageAsync(token),
                    (cont, token) => client.Feeds.GetSubscriptionsPageAsync(cont, token),
                    initialContinuation,
                    CliJsonContext.Default.PageEnvelopeFeedItem,
                    CliJsonContext.Default.FeedItem,
                    TableFormatter.RenderFeedItems,
                    cont => cont.Export(),
                    linked.Token);

                return 0;
            });
        });
        feedCmd.Subcommands.Add(subsCmd);

        // feed channels
        var channelsCmd = new Command("channels", "Retrieve channels to which the authenticated user is subscribed.");
        channelsCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, true);

                var initialContinuation = options.HasExplicitContinuation
                    ? SubscribedChannelsContinuation.Import(options.Continuation!)
                    : null;

                using var client = CreateClient(options);

                await OutputFormatter.ExecuteCollectionAsync(
                    options,
                    token => client.Feeds.GetSubscribedChannelsPageAsync(token),
                    (cont, token) => client.Feeds.GetSubscribedChannelsPageAsync(cont, token),
                    initialContinuation,
                    CliJsonContext.Default.PageEnvelopeChannelSummary,
                    CliJsonContext.Default.ChannelSummary,
                    TableFormatter.RenderChannels,
                    cont => cont.Export(),
                    linked.Token);

                return 0;
            });
        });
        feedCmd.Subcommands.Add(channelsCmd);

        // feed history
        var historyCmd = new Command("history", "Retrieve the authenticated user's watch history feed.");
        historyCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, true);

                var initialContinuation = options.HasExplicitContinuation
                    ? HistoryContinuation.Import(options.Continuation!)
                    : null;

                using var client = CreateClient(options);

                await OutputFormatter.ExecuteCollectionAsync(
                    options,
                    token => client.Feeds.GetHistoryPageAsync(token),
                    (cont, token) => client.Feeds.GetHistoryPageAsync(cont, token),
                    initialContinuation,
                    CliJsonContext.Default.PageEnvelopeHistoryEntry,
                    CliJsonContext.Default.HistoryEntry,
                    TableFormatter.RenderHistoryEntries,
                    cont => cont.Export(),
                    linked.Token);

                return 0;
            });
        });
        feedCmd.Subcommands.Add(historyCmd);

        return feedCmd;
    }

    // =========================================================================
    // 8. account
    // =========================================================================
    private static Command BuildAccountCommand(OptionDefinitions defs, CancellationToken rootCt)
    {
        var accountCmd = new Command("account", "Account profile and subscription operations.");

        // account profile
        var profileCmd = new Command("profile", "Retrieve profile info for the authenticated user.");
        profileCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, false);

                using var client = CreateClient(options);
                var profile = await client.Account.GetProfileAsync(linked.Token);

                OutputFormatter.ExecuteSingle(
                    options,
                    profile,
                    CliJsonContext.Default.Profile,
                    TableFormatter.RenderProfile);

                return 0;
            });
        });
        accountCmd.Subcommands.Add(profileCmd);

        // account subscribe <channel-id>
        var subscribeCmd = new Command("subscribe", "Subscribe to a YouTube channel.");
        var subChannelArg = new Argument<string>("channel-id") { Description = "Channel ID to subscribe to." };
        subscribeCmd.Arguments.Add(subChannelArg);
        subscribeCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, false);

                var channelRaw = parseResult.GetValue(subChannelArg);
                if (string.IsNullOrWhiteSpace(channelRaw))
                    throw new ArgumentException("Channel ID is required.");

                var channelId = ChannelId.Parse(channelRaw);
                using var client = CreateClient(options);
                await client.Account.SubscribeAsync(channelId, linked.Token);

                var result = new AccountActionResult(true, channelId);
                OutputFormatter.ExecuteSingle(
                    options,
                    result,
                    CliJsonContext.Default.AccountActionResult,
                    TableFormatter.RenderAccountAction);

                return 0;
            });
        });
        accountCmd.Subcommands.Add(subscribeCmd);

        // account unsubscribe <channel-id>
        var unsubscribeCmd = new Command("unsubscribe", "Unsubscribe from a YouTube channel.");
        var unsubChannelArg = new Argument<string>("channel-id") { Description = "Channel ID to unsubscribe from." };
        unsubscribeCmd.Arguments.Add(unsubChannelArg);
        unsubscribeCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, false);

                var channelRaw = parseResult.GetValue(unsubChannelArg);
                if (string.IsNullOrWhiteSpace(channelRaw))
                    throw new ArgumentException("Channel ID is required.");

                var channelId = ChannelId.Parse(channelRaw);
                using var client = CreateClient(options);
                await client.Account.UnsubscribeAsync(channelId, linked.Token);

                var result = new AccountActionResult(true, channelId);
                OutputFormatter.ExecuteSingle(
                    options,
                    result,
                    CliJsonContext.Default.AccountActionResult,
                    TableFormatter.RenderAccountAction);

                return 0;
            });
        });
        accountCmd.Subcommands.Add(unsubscribeCmd);

        return accountCmd;
    }

    // =========================================================================
    // 9. rating
    // =========================================================================
    private static Command BuildRatingCommand(OptionDefinitions defs, CancellationToken rootCt)
    {
        var ratingCmd = new Command("rating", "Video rating querying and modification operations.");

        // rating get <video>
        var getCmd = new Command("get", "Get the current rating given to a video.");
        var getVideoArg = new Argument<string>("video") { Description = "Video ID or URL." };
        getCmd.Arguments.Add(getVideoArg);
        getCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, false);

                var videoRaw = parseResult.GetValue(getVideoArg);
                if (string.IsNullOrWhiteSpace(videoRaw))
                    throw new ArgumentException("Video ID is required.");

                var videoId = VideoId.Parse(videoRaw);
                using var client = CreateClient(options);
                var rating = await client.Ratings.GetAsync(videoId, linked.Token);

                var result = new RatingGetResult(videoId, rating);
                OutputFormatter.ExecuteSingle(
                    options,
                    result,
                    CliJsonContext.Default.RatingGetResult,
                    TableFormatter.RenderRatingGet);

                return 0;
            });
        });
        ratingCmd.Subcommands.Add(getCmd);

        // rating set <video> <rating>
        var setCmd = new Command("set", "Set the rating (none, like, dislike) for a video.");
        var setVideoArg = new Argument<string>("video") { Description = "Video ID or URL." };
        var ratingArg = new Argument<string>("rating") { Description = "Rating: none, like, dislike." };
        setCmd.Arguments.Add(setVideoArg);
        setCmd.Arguments.Add(ratingArg);
        setCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, false);

                var videoRaw = parseResult.GetValue(setVideoArg);
                var ratingRaw = parseResult.GetValue(ratingArg);

                if (string.IsNullOrWhiteSpace(videoRaw))
                    throw new ArgumentException("Video ID is required.");
                if (string.IsNullOrWhiteSpace(ratingRaw))
                    throw new ArgumentException("Rating is required.");

                var videoId = VideoId.Parse(videoRaw);
                if (!Enum.TryParse<VideoRating>(ratingRaw, true, out var rating))
                    throw new ArgumentException($"Invalid rating '{ratingRaw}'. Valid values: none, like, dislike.");

                using var client = CreateClient(options);
                await client.Ratings.SetAsync(videoId, rating, linked.Token);

                var result = new RatingActionResult(true, videoId, rating);
                OutputFormatter.ExecuteSingle(
                    options,
                    result,
                    CliJsonContext.Default.RatingActionResult,
                    TableFormatter.RenderRatingAction);

                return 0;
            });
        });
        ratingCmd.Subcommands.Add(setCmd);

        return ratingCmd;
    }

    // =========================================================================
    // 10. history
    // =========================================================================
    private static Command BuildHistoryCommand(OptionDefinitions defs, CancellationToken rootCt)
    {
        var historyCmd = new Command("history", "Watch history removal and clear operations.");

        // history remove <history-entry-id>
        var removeCmd = new Command("remove", "Remove a specific entry from watch history.");
        var entryArg = new Argument<string>("history-entry-id") { Description = "History entry ID to remove." };
        removeCmd.Arguments.Add(entryArg);
        removeCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, false);

                var entryRaw = parseResult.GetValue(entryArg);
                if (string.IsNullOrWhiteSpace(entryRaw))
                    throw new ArgumentException("History entry ID is required.");

                var entryId = HistoryEntryId.Parse(entryRaw);
                using var client = CreateClient(options);
                await client.Account.RemoveHistoryEntryAsync(entryId, linked.Token);

                var result = new HistoryActionResult(true, entryId);
                OutputFormatter.ExecuteSingle(
                    options,
                    result,
                    CliJsonContext.Default.HistoryActionResult,
                    TableFormatter.RenderHistoryAction);

                return 0;
            });
        });
        historyCmd.Subcommands.Add(removeCmd);

        // history clear --yes
        var clearCmd = new Command("clear", "Clear the authenticated user's entire watch history.");
        var yesOpt = new Option<bool>("--yes") { Description = "Confirm clearing entire watch history." };
        clearCmd.Options.Add(yesOpt);
        clearCmd.SetAction(async (parseResult, ct) =>
        {
            var options = defs.Parse(parseResult);
            return await ExecuteActionAsync(options.Format, async () =>
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, rootCt);
                ValidateOptions(options, false);

                var yes = parseResult.GetValue(yesOpt);
                if (!yes)
                    throw new ArgumentException("The history clear command requires the --yes flag to confirm.");

                using var client = CreateClient(options);
                await client.Account.ClearHistoryAsync(linked.Token);

                var result = new HistoryActionResult(true, Cleared: true);
                OutputFormatter.ExecuteSingle(
                    options,
                    result,
                    CliJsonContext.Default.HistoryActionResult,
                    TableFormatter.RenderHistoryAction);

                return 0;
            });
        });
        historyCmd.Subcommands.Add(clearCmd);

        return historyCmd;
    }
}