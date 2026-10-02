using System.Text.Json;
using YoutubeAPI.Exceptions;
using YoutubeAPI.Models.Account;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Infrastructure;

internal sealed class AccountHandler(InnerTubeSession session) : IYouTubeAccountHandler
{
    private readonly Lock _profileSync = new();
    private Profile? _cachedProfile;
    private Task<Profile>? _profileTask;

    public async Task<Profile> GetProfileAsync(CancellationToken cancellationToken)
    {
        session.EnsureAuthenticated();
        Task<Profile> profileTask;
        lock (_profileSync)
        {
            if (_cachedProfile != null)
                return _cachedProfile;

            _profileTask ??= LoadProfileAsync();
            profileTask = _profileTask;
        }

        try
        {
            var profile = await profileTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            _cachedProfile = profile;
            return profile;
        }
        catch
        {
            lock (_profileSync)
            {
                if (ReferenceEquals(_profileTask, profileTask) &&
                    (profileTask.IsFaulted || profileTask.IsCanceled))
                    _profileTask = null;
            }

            throw;
        }
        }

    public async Task SubscribeAsync(ChannelId channelId, CancellationToken cancellationToken)
    {
        session.EnsureAuthenticated();

        // 1. Resolve active profile to confirm account and validate identity snapshot
        _ = await GetProfileAsync(cancellationToken).ConfigureAwait(false);

        // 2. Fetch channel browse response to discover the current action endpoint and request parameters
        using var channelDoc = await session.PostInnerTubeAsync(
            "browse",
            writer => { writer.WriteString("browseId", channelId.Value); },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var command = FindSubscriptionCommand(channelDoc.RootElement, true);
        if (command == null)
            throw new YouTubeProtocolException(
                $"Could not discover subscribe action endpoint for channel '{channelId.Value}'.");

        // 3. Post discovered subscription command
        using var doc = await session.PostInnerTubeAsync(
            command.Endpoint,
            writer =>
            {
                writer.WriteStartArray("channelIds");
                if (command.ChannelIds is { Count: > 0 })
                    foreach (var id in command.ChannelIds)
                        writer.WriteStringValue(id);
                else
                    writer.WriteStringValue(channelId.Value);

                writer.WriteEndArray();

                if (!string.IsNullOrEmpty(command.Params)) writer.WriteString("params", command.Params);

                if (string.IsNullOrEmpty(command.TrackingParams)) return;
                writer.WriteStartObject("clickTracking");
                writer.WriteString("clickTrackingParams", command.TrackingParams);
                writer.WriteEndObject();
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        // 4. Require acknowledged action
        if (!HasAcknowledgedAction(doc.RootElement))
            throw new YouTubeProtocolException(
                $"YouTube did not acknowledge the subscribe action for channel '{channelId.Value}'.");
    }

    public async Task UnsubscribeAsync(ChannelId channelId, CancellationToken cancellationToken)
    {
        session.EnsureAuthenticated();

        // 1. Resolve active profile to confirm account and validate identity snapshot
        _ = await GetProfileAsync(cancellationToken).ConfigureAwait(false);

        // 2. Fetch channel browse response to discover the current action endpoint and request parameters
        using var channelDoc = await session.PostInnerTubeAsync(
            "browse",
            writer => { writer.WriteString("browseId", channelId.Value); },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var command = FindSubscriptionCommand(channelDoc.RootElement, false);
        if (command == null)
            throw new YouTubeProtocolException(
                $"Could not discover unsubscribe action endpoint for channel '{channelId.Value}'.");

        // 3. Post discovered unsubscription command
        using var doc = await session.PostInnerTubeAsync(
            command.Endpoint,
            writer =>
            {
                writer.WriteStartArray("channelIds");
                if (command.ChannelIds is { Count: > 0 })
                    foreach (var id in command.ChannelIds)
                        writer.WriteStringValue(id);
                else
                    writer.WriteStringValue(channelId.Value);

                writer.WriteEndArray();

                if (!string.IsNullOrEmpty(command.Params)) writer.WriteString("params", command.Params);

                if (string.IsNullOrEmpty(command.TrackingParams)) return;
                writer.WriteStartObject("clickTracking");
                writer.WriteString("clickTrackingParams", command.TrackingParams);
                writer.WriteEndObject();
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        // 4. Require acknowledged action
        if (!HasAcknowledgedAction(doc.RootElement))
            throw new YouTubeProtocolException(
                $"YouTube did not acknowledge the unsubscribe action for channel '{channelId.Value}'.");
    }

    public async Task RemoveHistoryEntryAsync(HistoryEntryId entryId, CancellationToken cancellationToken)
    {
        session.EnsureAuthenticated();

        using var doc = await session.PostInnerTubeAsync(
            "feedback",
            writer =>
            {
                writer.WriteStartArray("feedbackTokens");
                writer.WriteStringValue(entryId.Value);
                writer.WriteEndArray();
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var root = doc.RootElement;
        var processed = false;
        if (root.TryGetProperty("feedbackResponses", out var responses) && responses.ValueKind == JsonValueKind.Array)
            foreach (var resp in responses.EnumerateArray())
                if (resp.TryGetProperty("isProcessed", out var ip) && ip.ValueKind == JsonValueKind.True &&
                    ip.GetBoolean())
                    processed = true;
                else
                    throw new YouTubeRequestException("YouTube reported history removal feedback was not processed.",
                        "feedback");

        if (!processed)
            throw new YouTubeRequestException("YouTube did not confirm history removal feedback processing.",
                "feedback");
    }

    public async Task ClearHistoryAsync(CancellationToken cancellationToken)
    {
        session.EnsureAuthenticated();

        var feedsHandler = new FeedsHandler(session);
        string? lastFirstId = null;

        while (true)
        {
            var page = await feedsHandler.GetHistoryPageAsync(cancellationToken).ConfigureAwait(false);
            if (page.Items.Count == 0)
                return;

            var currentFirstId = page.Items[0].Id.Value;
            if (string.Equals(currentFirstId, lastFirstId, StringComparison.Ordinal))
                throw new YouTubeProtocolException(
                    "ClearHistoryAsync made no progress removing watch history entries.");

            lastFirstId = currentFirstId;
            var tokens = page.Items.Select(i => i.Id.Value).ToList();
            using var doc = await session.PostInnerTubeAsync(
                "feedback",
                writer =>
                {
                    writer.WriteStartArray("feedbackTokens");
                    foreach (var token in tokens)
                        writer.WriteStringValue(token);
                    writer.WriteEndArray();
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var root = doc.RootElement;
            if (!root.TryGetProperty("feedbackResponses", out var responses) ||
                responses.ValueKind != JsonValueKind.Array ||
                responses.GetArrayLength() != tokens.Count)
                throw new YouTubeProtocolException("ClearHistoryAsync received an ambiguous feedback response.");

            foreach (var response in responses.EnumerateArray())
                if (!response.TryGetProperty("isProcessed", out var processed) ||
                    processed.ValueKind != JsonValueKind.True ||
                    !processed.GetBoolean())
                    throw new YouTubeProtocolException(
                        "ClearHistoryAsync encountered an unacknowledged feedback removal.");
        }
    }

    private async Task<Profile> LoadProfileAsync()
    {
        string? requestDiagnostics = null;
        using var doc = await session.PostInnerTubeAsync(
            "account/account_menu",
            _ => { },
            diagnostics => requestDiagnostics = diagnostics,
            cancellationToken: CancellationToken.None).ConfigureAwait(false);

        if (InnerTubeSession.HasExplicitLoggedOut(doc.RootElement))
            throw new AuthenticationExpiredException(
                $"Failed to load user profile: YouTube explicitly reported the account as logged out ({DescribeResponseShape(doc.RootElement)}; {requestDiagnostics ?? "requestDiagnostics=unavailable"}).");

        var profile = ParseAccountMenuProfile(doc.RootElement);
        if (profile is not null)
            return profile;

        throw new YouTubeProtocolException(
            $"Failed to load user profile: unexpected account_menu response format ({DescribeResponseShape(doc.RootElement)}; {requestDiagnostics ?? "requestDiagnostics=unavailable"}).");
    }

    private static SubscriptionCommand? FindSubscriptionCommand(JsonElement element, bool isSubscribe)
    {
        var targetProp = isSubscribe ? "subscribeEndpoint" : "unsubscribeEndpoint";
        var defaultEndpoint = isSubscribe ? "subscription/subscribe" : "subscription/unsubscribe";

        switch (element.ValueKind)
        {
            // 1. Direct target property
            case JsonValueKind.Object when element.TryGetProperty(targetProp, out var ep):
                return ExtractCommandFromEndpoint(ep, defaultEndpoint, element);
            // 2. Check subscribeButtonRenderer
            case JsonValueKind.Object:
            {
                if (element.TryGetProperty("subscribeButtonRenderer", out var sbr))
                {
                    if (sbr.TryGetProperty(targetProp, out var sbrEp))
                        return ExtractCommandFromEndpoint(sbrEp, defaultEndpoint, sbr);

                    if (sbr.TryGetProperty("serviceEndpoint", out var sbrSe))
                        if (sbrSe.TryGetProperty(targetProp, out var seEp))
                            return ExtractCommandFromEndpoint(seEp, defaultEndpoint, sbrSe);

                    if (sbr.TryGetProperty("onTap", out var sbrOnTap))
                    {
                        var cmd = FindSubscriptionCommand(sbrOnTap, isSubscribe);
                        if (cmd != null)
                            return cmd;
                    }

                    if (isSubscribe && sbr.TryGetProperty("params", out var sbrParams) &&
                        sbrParams.ValueKind == JsonValueKind.String)
                    {
                        var pVal = sbrParams.GetString()!;
                        var tp = sbr.TryGetProperty("trackingParams", out var sbrTp) ? sbrTp.GetString() : null;
                        List<string>? chIds = null;
                        if (sbr.TryGetProperty("channelId", out var chIdEl) && chIdEl.ValueKind == JsonValueKind.String)
                            chIds = [chIdEl.GetString()!];
                        return new SubscriptionCommand(defaultEndpoint, pVal, tp, chIds);
                    }
                }

                // 3. Check buttonViewModel or generic innertubeCommand/serviceEndpoint
                if (element.TryGetProperty("serviceEndpoint", out var se))
                    if (se.TryGetProperty(targetProp, out var seEp))
                        return ExtractCommandFromEndpoint(seEp, defaultEndpoint, se);

                if (!element.TryGetProperty("innertubeCommand", out var itc))
                    return element.EnumerateObject().Select(prop => FindSubscriptionCommand(prop.Value, isSubscribe))
                        .OfType<SubscriptionCommand>().FirstOrDefault();
                return itc.TryGetProperty(targetProp, out var itcEp)
                    ? ExtractCommandFromEndpoint(itcEp, defaultEndpoint, itc)
                    :
                    // 4. Recurse properties
                    element.EnumerateObject().Select(prop => FindSubscriptionCommand(prop.Value, isSubscribe))
                        .OfType<SubscriptionCommand>().FirstOrDefault();
            }
            case JsonValueKind.Array:
                return element.EnumerateArray().Select(item => FindSubscriptionCommand(item, isSubscribe))
                    .OfType<SubscriptionCommand>().FirstOrDefault();
            default:
                return null;
        }
    }

    private static SubscriptionCommand ExtractCommandFromEndpoint(
        JsonElement epElement,
        string defaultEndpoint,
        JsonElement parentElement)
    {
        var endpoint = defaultEndpoint;
        string? paramsVal = null;
        string? trackingParams = null;
        List<string>? channelIds = null;

        if (epElement.ValueKind == JsonValueKind.Object)
        {
            if (epElement.TryGetProperty("params", out var p) && p.ValueKind == JsonValueKind.String)
                paramsVal = p.GetString();

            if (epElement.TryGetProperty("clickTrackingParams", out var ctp) && ctp.ValueKind == JsonValueKind.String)
                trackingParams = ctp.GetString();

            if (epElement.TryGetProperty("channelIds", out var chs) && chs.ValueKind == JsonValueKind.Array)
            {
                channelIds = [];
                foreach (var ch in chs.EnumerateArray())
                    if (ch.ValueKind == JsonValueKind.String && ch.GetString() is { } s)
                        channelIds.Add(s);
            }

            if (epElement.TryGetProperty("commandMetadata", out var cmdMeta) &&
                cmdMeta.TryGetProperty("webCommandMetadata", out var wcm) &&
                wcm.TryGetProperty("url", out var urlEl) &&
                urlEl.GetString() is { } urlStr)
            {
                if (urlStr.StartsWith("/youtubei/v1/", StringComparison.OrdinalIgnoreCase))
                    endpoint = urlStr["/youtubei/v1/".Length..];
                else if (urlStr.StartsWith('/')) endpoint = urlStr.TrimStart('/');
            }

            if (epElement.TryGetProperty("subscribeEndpoint", out var nestedSub) &&
                nestedSub.ValueKind == JsonValueKind.Object)
            {
                if (string.IsNullOrEmpty(paramsVal) && nestedSub.TryGetProperty("params", out var np) &&
                    np.ValueKind == JsonValueKind.String) paramsVal = np.GetString();

                if (channelIds == null && nestedSub.TryGetProperty("channelIds", out var nchs) &&
                    nchs.ValueKind == JsonValueKind.Array)
                {
                    channelIds = [];
                    foreach (var ch in nchs.EnumerateArray())
                        if (ch.ValueKind == JsonValueKind.String && ch.GetString() is { } s)
                            channelIds.Add(s);
                }
            }
            else if (epElement.TryGetProperty("unsubscribeEndpoint", out var nestedUnsub) &&
                     nestedUnsub.ValueKind == JsonValueKind.Object)
            {
                if (string.IsNullOrEmpty(paramsVal) && nestedUnsub.TryGetProperty("params", out var np) &&
                    np.ValueKind == JsonValueKind.String) paramsVal = np.GetString();

                if (channelIds == null && nestedUnsub.TryGetProperty("channelIds", out var nchs) &&
                    nchs.ValueKind == JsonValueKind.Array)
                {
                    channelIds = [];
                    foreach (var ch in nchs.EnumerateArray())
                        if (ch.ValueKind == JsonValueKind.String && ch.GetString() is { } s)
                            channelIds.Add(s);
                }
            }
        }

        if (!string.IsNullOrEmpty(paramsVal) || parentElement.ValueKind != JsonValueKind.Object)
            return new SubscriptionCommand(endpoint, paramsVal, trackingParams, channelIds);
        if (parentElement.TryGetProperty("params", out var pp) && pp.ValueKind == JsonValueKind.String)
            paramsVal = pp.GetString();

        if (string.IsNullOrEmpty(trackingParams) && parentElement.TryGetProperty("trackingParams", out var ptp) &&
            ptp.ValueKind == JsonValueKind.String) trackingParams = ptp.GetString();

        return new SubscriptionCommand(endpoint, paramsVal, trackingParams, channelIds);
    }

    private static bool HasAcknowledgedAction(JsonElement root)
    {
        return (root.TryGetProperty("actions", out var actions) && actions.ValueKind == JsonValueKind.Array &&
                actions.GetArrayLength() > 0) ||
               (root.TryGetProperty("mutationResults", out var mutations) &&
                mutations.ValueKind == JsonValueKind.Array && mutations.GetArrayLength() > 0) ||
               (root.TryGetProperty("commands", out var commands) && commands.ValueKind == JsonValueKind.Array &&
                commands.GetArrayLength() > 0) ||
               root.TryGetProperty("frameworkUpdates", out _) ||
               (root.TryGetProperty("status", out var st) &&
                st.GetString()?.Equals("STATUS_SUCCEEDED", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static Profile? ParseAccountMenuProfile(JsonElement root)
    {
        if (!TryFindActiveAccountHeaderRenderer(root, out var aahr)) return null;
        var displayName = aahr.GetText("accountName");
        if (string.IsNullOrEmpty(displayName)) displayName = aahr.GetText("displayName");
        if (string.IsNullOrEmpty(displayName)) displayName = aahr.GetText("channelName");
        if (string.IsNullOrEmpty(displayName)) displayName = aahr.GetText("title");

        var handle = aahr.GetText("channelHandle");
        if (string.IsNullOrEmpty(handle)) handle = aahr.GetText("handleText");
        if (string.IsNullOrEmpty(handle)) handle = aahr.GetText("handle");
        if (string.IsNullOrEmpty(handle)) handle = null;

        var avatars = aahr.GetThumbnails("accountPhoto");
        if (avatars.Count == 0) avatars = aahr.GetThumbnails("avatar");
        var avatar = avatars.Count > 0 ? avatars[0] : null;

        ChannelId? channelId = null;
        if (aahr.TryGetProperty("serviceEndpoint", out var se) &&
            se.TryGetProperty("browseEndpoint", out var be) &&
            be.TryGetProperty("browseId", out var bid) &&
            ChannelId.TryParse(bid.GetString(), out var cid))
            channelId = cid;
        else if (aahr.TryGetProperty("channelEndpoint", out var ce) &&
                 ce.TryGetProperty("browseEndpoint", out var cbe) &&
                 cbe.TryGetProperty("browseId", out var cbid) &&
                 ChannelId.TryParse(cbid.GetString(), out var ccid))
            channelId = ccid;
        else if (aahr.TryGetProperty("navigationEndpoint", out var ne) &&
                 ne.TryGetProperty("browseEndpoint", out var nbe) &&
                 nbe.TryGetProperty("browseId", out var nbid) &&
                 ChannelId.TryParse(nbid.GetString(), out var ncid))
            channelId = ncid;

        return new Profile(channelId, displayName, handle, avatar);
    }

    private static bool TryFindActiveAccountHeaderRenderer(JsonElement element, out JsonElement renderer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (element.TryGetProperty("activeAccountHeaderRenderer", out renderer) ||
                    element.TryGetProperty("accountHeaderRenderer", out renderer) ||
                    element.TryGetProperty("accountInfoRenderer", out renderer))
                    return true;

                // YouTube occasionally moves the account header fields without
                // preserving the renderer name. Accept the recognizable header shape
                // while continuing to recurse through the response.
                if (element.TryGetProperty("accountName", out _) ||
                    element.TryGetProperty("displayName", out _) ||
                    element.TryGetProperty("channelName", out _) ||
                    element.TryGetProperty("accountPhoto", out _) ||
                    element.TryGetProperty("avatar", out _))
                {
                    renderer = element;
                    return true;
                }

                foreach (var prop in element.EnumerateObject())
                    if (TryFindActiveAccountHeaderRenderer(prop.Value, out renderer))
                        return true;
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    if (TryFindActiveAccountHeaderRenderer(item, out renderer))
                        return true;
                break;
        }

        renderer = default;
        return false;
    }

    private static string DescribeResponseShape(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return $"root={root.ValueKind}";

        var actionShapes = new List<string>();
        var actionCount = 0;
        if (root.TryGetProperty("actions", out var actions) && actions.ValueKind == JsonValueKind.Array)
        {
            actionCount = actions.GetArrayLength();
            foreach (var action in actions.EnumerateArray().Take(256))
                actionShapes.Add(action.ValueKind == JsonValueKind.Object
                    ? $"[{DescribePropertyNames(action, 12)}]"
                    : $"[{action.ValueKind}]");
        }

        var structures = new List<string>();
        var visited = 0;
        var hasSignInEndpoint = false;
        var traversalTruncated = false;
        CollectDiagnosticStructure(root, "$", 0, ref visited, structures, ref hasSignInEndpoint,
            ref traversalTruncated);
        var loggedOutPath = "responseContext.loggedOut";
        var loggedOut = "absent";
        if (root.TryGetProperty("responseContext", out var context) && context.ValueKind == JsonValueKind.Object)
        {
            if (!TryGetBoolean(context, "loggedOut", out loggedOut))
            {
                loggedOutPath = "responseContext.mainAppWebResponseContext.loggedOut";
                if (context.TryGetProperty("mainAppWebResponseContext", out var mainContext) &&
                    mainContext.ValueKind == JsonValueKind.Object)
                    _ = TryGetBoolean(mainContext, "loggedOut", out loggedOut);
            }
        }

        var signInPresence = hasSignInEndpoint ? "true" : traversalTruncated ? "unknown" : "false";
        var output = $"topLevel=[{DescribePropertyNames(root, 12)}],actions=[{string.Join(",", actionShapes)}]," +
                     $"actionsTruncated={(actionCount > 256).ToString().ToLowerInvariant()}," +
                     $"signInEndpointPresent={signInPresence},{loggedOutPath}={loggedOut}," +
                     $"traversalTruncated={traversalTruncated.ToString().ToLowerInvariant()}";
        if (structures.Count > 0)
            output += $",structure=[{string.Join(",", structures)}]";
        return output.Length <= 1800 ? output : output[..1800];
    }

    private static bool TryGetBoolean(JsonElement element, string propertyName, out string value)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            value = property.GetBoolean().ToString().ToLowerInvariant();
            return true;
        }

        value = "absent";
        return false;
    }

    private static string DescribePropertyNames(JsonElement element, int limit)
    {
        var names = new List<string>();
        foreach (var property in element.EnumerateObject().Take(limit))
            names.Add(SafePropertyName(property.Name));
        return string.Join(",", names);
    }

    private static void CollectDiagnosticStructure(
        JsonElement element,
        string path,
        int depth,
        ref int visited,
        List<string> structures,
        ref bool hasSignInEndpoint,
        ref bool traversalTruncated)
    {
        if (element.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            return;
        if (depth >= 20 || visited >= 512 || structures.Count >= 48)
        {
            traversalTruncated = true;
            return;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (++visited > 512)
                {
                    traversalTruncated = true;
                    return;
                }

                var safeName = SafePropertyName(property.Name);
                var childPath = $"{path}.{safeName}";
                if (property.Name.Equals("signInEndpoint", StringComparison.Ordinal))
                    hasSignInEndpoint = true;
                var inRelevantBranch = path.Contains("popup", StringComparison.OrdinalIgnoreCase) ||
                                       path.Contains("header", StringComparison.OrdinalIgnoreCase) ||
                                       safeName.Contains("popup", StringComparison.OrdinalIgnoreCase) ||
                                       safeName.Contains("header", StringComparison.OrdinalIgnoreCase);
                if (inRelevantBranch && (safeName.EndsWith("Renderer", StringComparison.Ordinal) ||
                                         safeName is "popup" or "header" or "signInEndpoint"))
                    structures.Add(childPath);
                CollectDiagnosticStructure(property.Value, childPath, depth + 1, ref visited, structures,
                    ref hasSignInEndpoint, ref traversalTruncated);
                if (visited >= 512 || structures.Count >= 48)
                {
                    traversalTruncated = true;
                    return;
                }
            }
        }
        else
        {
            var index = 0;
            var arrayLength = element.GetArrayLength();
            foreach (var item in element.EnumerateArray().Take(32))
            {
                CollectDiagnosticStructure(item, $"{path}[{index++}]", depth + 1, ref visited, structures,
                    ref hasSignInEndpoint, ref traversalTruncated);
                if (visited >= 512 || structures.Count >= 48)
                {
                    traversalTruncated = true;
                    return;
                }
            }
            if (arrayLength > 32)
                traversalTruncated = true;
        }
    }

    private static string SafePropertyName(string name)
    {
        if (name.Length is 0 or > 64)
            return "[unsafe]";
        foreach (var character in name)
            if (!(character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-'))
                return "[unsafe]";
        return name;
    }

    private sealed record SubscriptionCommand(
        string Endpoint,
        string? Params,
        string? TrackingParams,
        IReadOnlyList<string>? ChannelIds);
}