using System.Text.Json;
using YoutubeAPI.Exceptions;
using YoutubeAPI.Models.Enums;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Infrastructure;

internal sealed class RatingsHandler(InnerTubeSession session) : IYouTubeRatingsHandler
{
    public async Task<VideoRating> GetAsync(VideoId videoId, CancellationToken cancellationToken)
    {
        session.EnsureAuthenticated();

        using var doc = await session.PostInnerTubeAsync(
            "next",
            writer => { writer.WriteString("videoId", videoId.Value); },
            cancellationToken).ConfigureAwait(false);

        return ParseVideoRating(doc.RootElement);
    }

    public async Task SetAsync(VideoId videoId, VideoRating rating, CancellationToken cancellationToken)
    {
        session.EnsureAuthenticated();

        // 1. Fetch the authenticated watch response to discover current rating actions and parameters
        using var watchDoc = await session.PostInnerTubeAsync(
            "next",
            writer => { writer.WriteString("videoId", videoId.Value); },
            cancellationToken).ConfigureAwait(false);

        var actions = DiscoverRatingActions(watchDoc.RootElement, videoId);

        if (!actions.TryGetValue(rating, out var actionInfo))
            throw new YouTubeProtocolException(
                $"Could not discover rating action for rating '{rating}' on video '{videoId}'.");

        // 2. Post mutation once with discovered endpoint, target, params, and tracking
        using var mutateDoc = await session.PostInnerTubeAsync(
            actionInfo.Endpoint,
            writer =>
            {
                writer.WriteStartObject("target");
                writer.WriteString("videoId", actionInfo.TargetVideoId ?? videoId.Value);
                writer.WriteEndObject();

                if (!string.IsNullOrEmpty(actionInfo.Params)) writer.WriteString("params", actionInfo.Params);

                if (!string.IsNullOrEmpty(actionInfo.TrackingParams))
                    writer.WriteString("trackingParams", actionInfo.TrackingParams);
            },
            cancellationToken).ConfigureAwait(false);

        // 3. Require explicit commandProcessed / success acknowledgement
        ValidateRatingAcknowledgement(mutateDoc.RootElement, actionInfo.Endpoint, videoId);
    }

    private static void ValidateRatingAcknowledgement(JsonElement root, string endpoint, VideoId videoId)
    {
        var status = root.TryGetProperty("status", out var st) ? st.GetString() : null;
        if (status != null)
        {
            if (!status.Equals("STATUS_SUCCEEDED", StringComparison.OrdinalIgnoreCase))
                throw new YouTubeRequestException($"Failed to set rating for video '{videoId}': status '{status}'.",
                    "rating.set");
            return;
        }

        if (root.TryGetProperty("commandProcessed", out var cp) && cp.ValueKind == JsonValueKind.True) return;

        if (root.TryGetProperty("success", out var succ) && succ.ValueKind == JsonValueKind.True) return;

        if (root.TryGetProperty("actions", out var actions) && actions.ValueKind == JsonValueKind.Array &&
            actions.GetArrayLength() > 0) return;

        if (root.TryGetProperty("feedbackResponses", out var fr) && fr.ValueKind == JsonValueKind.Array &&
            fr.GetArrayLength() > 0) return;

        throw new YouTubeProtocolException(
            $"YouTube returned an ambiguous or unacknowledged response for rating mutation on video '{videoId}' at endpoint '{endpoint}'.");
    }

    private static Dictionary<VideoRating, DiscoveredRatingAction> DiscoverRatingActions(JsonElement root,
        VideoId videoId)
    {
        var actions = new Dictionary<VideoRating, DiscoveredRatingAction>();
        CollectRatingActions(root, actions, videoId.Value);
        return actions;
    }

    private static void CollectRatingActions(JsonElement element,
        Dictionary<VideoRating, DiscoveredRatingAction> actions, string defaultVideoId)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                if (element.TryGetProperty("segmentedLikeDislikeButtonViewModel", out var sldvm))
                {
                    CollectFromSegmentedViewModel(sldvm, actions, defaultVideoId);
                }
                else if (element.TryGetProperty("segmentedLikeDislikeButtonRenderer", out var sldbr))
                {
                    CollectFromSegmentedRenderer(sldbr, actions, defaultVideoId);
                }
                else if (element.TryGetProperty("likeEndpoint", out _))
                {
                    var action = TryParseLikeEndpoint(element, defaultVideoId);
                    if (action != null) actions.TryAdd(action.Rating, action);
                }
                else if (element.TryGetProperty("toggleButtonRenderer", out var tbr))
                {
                    CollectFromToggleButtonRenderer(tbr, actions, defaultVideoId);
                }

                foreach (var property in element.EnumerateObject())
                    CollectRatingActions(property.Value, actions, defaultVideoId);
                break;
            }
            case JsonValueKind.Array:
            {
                foreach (var item in element.EnumerateArray()) CollectRatingActions(item, actions, defaultVideoId);
                break;
            }
        }
    }

    private static void CollectFromSegmentedViewModel(JsonElement sldvm,
        Dictionary<VideoRating, DiscoveredRatingAction> actions, string defaultVideoId)
    {
        if (sldvm.TryGetProperty("likeButtonViewModel", out var lbvmProp))
        {
            var lbvm = lbvmProp.TryGetProperty("likeButtonViewModel", out var inner) ? inner : lbvmProp;
            CollectFromButtonViewModel(lbvm, actions, defaultVideoId, VideoRating.Like);
        }

        if (!sldvm.TryGetProperty("dislikeButtonViewModel", out var dlbvmProp)) return;

        var dlbvm = dlbvmProp.TryGetProperty("dislikeButtonViewModel", out var iinner) ? iinner : dlbvmProp;
        CollectFromButtonViewModel(dlbvm, actions, defaultVideoId, VideoRating.Dislike);
    }

    private static void CollectFromButtonViewModel(JsonElement bvm,
        Dictionary<VideoRating, DiscoveredRatingAction> actions, string defaultVideoId,
        VideoRating expectedDefaultRating)
    {
        if (bvm.TryGetProperty("toggleButtonViewModel", out var tbvmProp))
        {
            var tbvm = tbvmProp.TryGetProperty("toggleButtonViewModel", out var inner) ? inner : tbvmProp;

            if (tbvm.TryGetProperty("defaultButtonViewModel", out var dbvmProp))
            {
                var dbvm = dbvmProp.TryGetProperty("buttonViewModel", out var inner2) ? inner2 : dbvmProp;
                var action = ExtractActionFromButtonViewModel(dbvm, defaultVideoId, expectedDefaultRating);
                if (action != null)
                    actions.TryAdd(action.Rating, action);
            }

            if (!tbvm.TryGetProperty("toggledButtonViewModel", out var tbvmProp2)) return;
            {
                var tbvm2 = tbvmProp2.TryGetProperty("buttonViewModel", out var inner3) ? inner3 : tbvmProp2;
                var action = ExtractActionFromButtonViewModel(tbvm2, defaultVideoId, VideoRating.None);
                if (action != null)
                    actions.TryAdd(action.Rating, action);
            }
        }
        else if (bvm.TryGetProperty("buttonViewModel", out var directBvm))
        {
            var action = ExtractActionFromButtonViewModel(directBvm, defaultVideoId, expectedDefaultRating);
            if (action != null)
                actions.TryAdd(action.Rating, action);
        }
    }

    private static DiscoveredRatingAction? ExtractActionFromButtonViewModel(JsonElement bvm, string defaultVideoId,
        VideoRating fallbackRating)
    {
        return bvm.TryGetProperty("onTap", out var onTap)
            ? ExtractActionFromOnTap(onTap, defaultVideoId, fallbackRating)
            : null;
    }

    private static DiscoveredRatingAction? ExtractActionFromOnTap(JsonElement onTap, string defaultVideoId,
        VideoRating fallbackRating)
    {
        if (onTap.TryGetProperty("innertubeCommand", out var itc))
            return TryParseLikeEndpoint(itc, defaultVideoId) ??
                   ExtractActionFromCommand(itc, defaultVideoId, fallbackRating);

        if (onTap.TryGetProperty("serialCommand", out var sc) && sc.TryGetProperty("commands", out var cmds) &&
            cmds.ValueKind == JsonValueKind.Array)
            foreach (var act in cmds.EnumerateArray()
                         .Select(cmd => ExtractActionFromOnTap(cmd, defaultVideoId, fallbackRating))
                         .OfType<DiscoveredRatingAction>())
                return act;

        if (!onTap.TryGetProperty("commandExecutorCommand", out var cec) ||
            !cec.TryGetProperty("commands", out var cmds2) ||
            cmds2.ValueKind != JsonValueKind.Array) return TryParseLikeEndpoint(onTap, defaultVideoId);
        {
            foreach (var act in cmds2.EnumerateArray()
                         .Select(cmd => ExtractActionFromOnTap(cmd, defaultVideoId, fallbackRating))
                         .OfType<DiscoveredRatingAction>()) return act;
        }

        return TryParseLikeEndpoint(onTap, defaultVideoId);
    }

    private static DiscoveredRatingAction? ExtractActionFromCommand(JsonElement cmd, string defaultVideoId,
        VideoRating fallbackRating)
    {
        if (cmd.TryGetProperty("likeEndpoint", out var le))
        {
            var tracking = cmd.TryGetProperty("clickTrackingParams", out var ctp) ? ctp.GetString() : null;
            return ParseLikeEndpointObject(le, tracking, defaultVideoId, fallbackRating);
        }

        if (!cmd.TryGetProperty("dislikeEndpoint", out var de)) return null;
        {
            var tracking = cmd.TryGetProperty("clickTrackingParams", out var ctp) ? ctp.GetString() : null;
            return ParseLikeEndpointObject(de, tracking, defaultVideoId, VideoRating.Dislike);
        }
    }

    private static void CollectFromSegmentedRenderer(JsonElement sldbr,
        Dictionary<VideoRating, DiscoveredRatingAction> actions, string defaultVideoId)
    {
        if (sldbr.TryGetProperty("likeButton", out var lb) && lb.TryGetProperty("toggleButtonRenderer", out var tbr))
            CollectFromToggleButtonRenderer(tbr, actions, defaultVideoId, VideoRating.Like);

        if (sldbr.TryGetProperty("dislikeButton", out var dlb) &&
            dlb.TryGetProperty("toggleButtonRenderer", out var dtbr))
            CollectFromToggleButtonRenderer(dtbr, actions, defaultVideoId, VideoRating.Dislike);
    }

    private static void CollectFromToggleButtonRenderer(JsonElement tbr,
        Dictionary<VideoRating, DiscoveredRatingAction> actions, string defaultVideoId,
        VideoRating expectedDefaultRating = VideoRating.None)
    {
        if (tbr.TryGetProperty("defaultServiceEndpoint", out var dse))
        {
            var action = TryParseLikeEndpoint(dse, defaultVideoId) ??
                         ExtractActionFromCommand(dse, defaultVideoId, expectedDefaultRating);
            if (action != null)
                actions.TryAdd(action.Rating, action);
        }

        if (tbr.TryGetProperty("toggledServiceEndpoint", out var tse))
        {
            var action = TryParseLikeEndpoint(tse, defaultVideoId) ??
                         ExtractActionFromCommand(tse, defaultVideoId, VideoRating.None);
            if (action != null)
                actions.TryAdd(action.Rating, action);
        }

        if (!tbr.TryGetProperty("serviceEndpoint", out var se)) return;
        {
            var action = TryParseLikeEndpoint(se, defaultVideoId) ??
                         ExtractActionFromCommand(se, defaultVideoId, expectedDefaultRating);
            if (action != null)
                actions.TryAdd(action.Rating, action);
        }
    }

    private static DiscoveredRatingAction? TryParseLikeEndpoint(JsonElement element, string defaultVideoId)
    {
        string? tracking = null;

        if (element.TryGetProperty("clickTrackingParams", out var ctp))
            tracking = ctp.GetString();
        else if (element.TryGetProperty("trackingParams", out var tp))
            tracking = tp.GetString();

        return
            element.TryGetProperty("likeEndpoint", out var le) ? ParseLikeEndpointObject(le, tracking, defaultVideoId) :
            element.TryGetProperty("status", out _) ? ParseLikeEndpointObject(element, tracking, defaultVideoId) :
            null;
    }

    private static DiscoveredRatingAction? ParseLikeEndpointObject(JsonElement le, string? tracking,
        string defaultVideoId, VideoRating? fallbackRating = null)
    {
        var status = le.TryGetProperty("status", out var s) ? s.GetString() : null;
        var paramsStr = le.TryGetProperty("params", out var p) ? p.GetString() : null;
        string? targetVideoId = null;
        if (le.TryGetProperty("target", out var target) && target.TryGetProperty("videoId", out var vid))
            targetVideoId = vid.GetString();
        targetVideoId ??= defaultVideoId;

        if (string.IsNullOrEmpty(tracking))
        {
            if (le.TryGetProperty("trackingParams", out var tp))
                tracking = tp.GetString();
            else if (le.TryGetProperty("clickTrackingParams", out var ctp))
                tracking = ctp.GetString();
        }

        VideoRating rating;
        string endpoint;

        if (status != null)
        {
            if (status.Equals("LIKE", StringComparison.OrdinalIgnoreCase))
            {
                rating = VideoRating.Like;
                endpoint = "like/like";
            }
            else if (status.Equals("DISLIKE", StringComparison.OrdinalIgnoreCase))
            {
                rating = VideoRating.Dislike;
                endpoint = "like/dislike";
            }
            else if (status.Equals("INDIFFERENT", StringComparison.OrdinalIgnoreCase) ||
                     status.Equals("REMOVE_LIKE", StringComparison.OrdinalIgnoreCase))
            {
                rating = VideoRating.None;
                endpoint = "like/removelike";
            }
            else if (fallbackRating.HasValue)
            {
                rating = fallbackRating.Value;
                endpoint = rating switch
                {
                    VideoRating.Like => "like/like",
                    VideoRating.Dislike => "like/dislike",
                    _ => "like/removelike"
                };
            }
            else
            {
                return null;
            }
        }
        else if (fallbackRating.HasValue)
        {
            rating = fallbackRating.Value;
            endpoint = rating switch
            {
                VideoRating.Like => "like/like",
                VideoRating.Dislike => "like/dislike",
                _ => "like/removelike"
            };
        }
        else
        {
            return null;
        }

        return new DiscoveredRatingAction(rating, endpoint, targetVideoId, paramsStr, tracking);
    }

    private static VideoRating ParseVideoRating(JsonElement root)
    {
        if (!root.TryGetProperty("contents", out var contents) ||
            !contents.TryGetProperty("twoColumnWatchNextResults", out var watchNext) ||
            !watchNext.TryGetProperty("results", out var results) ||
            !results.TryGetProperty("results", out var innerResults) ||
            !innerResults.TryGetProperty("contents", out var resultItems) ||
            resultItems.ValueKind != JsonValueKind.Array) return FindVideoRating(root);
        foreach (var item in resultItems.EnumerateArray())
            if (item.TryGetProperty("videoPrimaryInfoRenderer", out var primary) &&
                primary.TryGetProperty("videoActions", out var actions) &&
                actions.TryGetProperty("menuRenderer", out var menu) &&
                menu.TryGetProperty("topLevelButtons", out var buttons) &&
                buttons.ValueKind == JsonValueKind.Array)
                foreach (var rating in buttons.EnumerateArray().Select(ParseButtonRating)
                             .Where(rating => rating != VideoRating.None))
                    return rating;

        return FindVideoRating(root);
    }

    private static VideoRating ParseButtonRating(JsonElement button)
    {
        if (button.TryGetProperty("segmentedLikeDislikeButtonViewModel", out var sldvm))
        {
            if (sldvm.TryGetProperty("likeButtonViewModel", out var lbvm) &&
                lbvm.TryGetProperty("likeButtonViewModel", out var innerLbvm) &&
                innerLbvm.TryGetProperty("likeStatusEntity", out var lse) &&
                lse.TryGetProperty("likeStatus", out var ls))
            {
                var status = ls.GetString() ?? "";
                if (status.Equals("LIKE", StringComparison.OrdinalIgnoreCase))
                    return VideoRating.Like;
                if (status.Equals("DISLIKE", StringComparison.OrdinalIgnoreCase))
                    return VideoRating.Dislike;
            }

            if (sldvm.TryGetProperty("dislikeButtonViewModel", out var dlbvm) &&
                dlbvm.TryGetProperty("dislikeButtonViewModel", out var innerDlbvm) &&
                innerDlbvm.TryGetProperty("dislikeStatusEntity", out var dlse) &&
                dlse.TryGetProperty("likeStatus", out var dls))
            {
                var status = dls.GetString() ?? "";
                if (status.Equals("DISLIKE", StringComparison.OrdinalIgnoreCase))
                    return VideoRating.Dislike;
                if (status.Equals("LIKE", StringComparison.OrdinalIgnoreCase))
                    return VideoRating.Like;
            }
        }

        if (button.TryGetProperty("segmentedLikeDislikeButtonRenderer", out var sldbr))
        {
            if (sldbr.TryGetProperty("likeButton", out var lb) &&
                lb.TryGetProperty("toggleButtonRenderer", out var tbr) &&
                tbr.TryGetProperty("isToggled", out var isToggled) &&
                isToggled.ValueKind == JsonValueKind.True)
                return VideoRating.Like;

            if (sldbr.TryGetProperty("dislikeButton", out var dlb) &&
                dlb.TryGetProperty("toggleButtonRenderer", out var dtbr) &&
                dtbr.TryGetProperty("isToggled", out var isDisliked) &&
                isDisliked.ValueKind == JsonValueKind.True)
                return VideoRating.Dislike;
        }

        if (!button.TryGetProperty("toggleButtonRenderer", out var directTbr) ||
            !directTbr.TryGetProperty("isToggled", out var directToggled) ||
            directToggled.ValueKind != JsonValueKind.True ||
            !directTbr.TryGetProperty("defaultServiceEndpoint", out var dse) ||
            !dse.TryGetProperty("likeEndpoint", out var le) ||
            !le.TryGetProperty("status", out var st)) return VideoRating.None;

        var s = st.GetString() ?? "";
        if (s.Equals("LIKE", StringComparison.OrdinalIgnoreCase))
            return VideoRating.Like;

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (s.Equals("DISLIKE", StringComparison.OrdinalIgnoreCase))
            return VideoRating.Dislike;

        return VideoRating.None;
    }

    private static VideoRating FindVideoRating(JsonElement root)
    {
        switch (root.ValueKind)
        {
            case JsonValueKind.Object:
            {
                if (root.TryGetProperty("likeStatusEntity", out var lse) &&
                    lse.TryGetProperty("likeStatus", out var ls))
                {
                    var status = ls.GetString() ?? "";
                    if (status.Equals("LIKE", StringComparison.OrdinalIgnoreCase))
                        return VideoRating.Like;
                    if (status.Equals("DISLIKE", StringComparison.OrdinalIgnoreCase))
                        return VideoRating.Dislike;
                }

                foreach (var rating in root.EnumerateObject().Select(prop => FindVideoRating(prop.Value))
                             .Where(rating => rating != VideoRating.None)) return rating;

                break;
            }
            case JsonValueKind.Array:
            {
                foreach (var rating in root.EnumerateArray().Select(FindVideoRating)
                             .Where(rating => rating != VideoRating.None)) return rating;

                break;
            }
        }

        return VideoRating.None;
    }

    private sealed record DiscoveredRatingAction(
        VideoRating Rating,
        string Endpoint,
        string? TargetVideoId,
        string? Params,
        string? TrackingParams);
}