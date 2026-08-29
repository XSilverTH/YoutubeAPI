using System.Text.Json;
using YoutubeAPI.Exceptions;
using YoutubeAPI.Models.Comments;
using YoutubeAPI.Models.Common;
using YoutubeAPI.Models.Continuations;
using YoutubeAPI.Models.Enums;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Infrastructure;

internal sealed class CommentsHandler(InnerTubeSession session) : IYouTubeCommentsHandler
{
    public async Task<Page<CommentThread, CommentThreadsContinuation>> GetThreadsPageAsync(
        VideoId videoId,
        CommentSort sort,
        CancellationToken cancellationToken)
    {
        // Fetch /next to find comments section continuation token
        using var nextDoc = await session.PostInnerTubeAsync(
            "next",
            writer => { writer.WriteString("videoId", videoId.Value); },
            cancellationToken).ConfigureAwait(false);

        var nextRoot = nextDoc.RootElement;
        CheckIfCommentsDisabled(nextRoot);

        var initialCommentsToken = FindInitialCommentsToken(nextRoot);
        if (string.IsNullOrEmpty(initialCommentsToken))
            return new Page<CommentThread, CommentThreadsContinuation>([], null);

        // Fetch comments section
        using var commentsDoc = await session.PostInnerTubeAsync(
            "next",
            writer => { writer.WriteString("continuation", initialCommentsToken); },
            cancellationToken).ConfigureAwait(false);

        var commentsRoot = commentsDoc.RootElement;

        // If Newest sort requested, check if sort menu has a specific continuation token for Newest
        if (sort != CommentSort.Newest) return ParseCommentThreadsResponse(commentsRoot, videoId.Value, sort);

        // {
        var newestToken = FindSortContinuationToken(commentsRoot, "Newest");
        if (string.IsNullOrEmpty(newestToken))
            return ParseCommentThreadsResponse(commentsRoot, videoId.Value, sort);
        using var newestDoc = await session.PostInnerTubeAsync(
            "next",
            writer => { writer.WriteString("continuation", newestToken); },
            cancellationToken).ConfigureAwait(false);

        return ParseCommentThreadsResponse(newestDoc.RootElement, videoId.Value, sort);
        // }
    }

    public async Task<Page<CommentThread, CommentThreadsContinuation>> GetThreadsPageAsync(
        CommentThreadsContinuation continuation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        using var doc = await session.PostInnerTubeAsync(
            "next",
            writer => { writer.WriteString("continuation", continuation.Token); },
            cancellationToken).ConfigureAwait(false);

        return ParseCommentThreadsResponse(doc.RootElement, continuation.VideoId, continuation.Sort);
    }

    public async Task<Page<Comment, CommentRepliesContinuation>> GetRepliesPageAsync(
        CommentRepliesContinuation continuation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        using var doc = await session.PostInnerTubeAsync(
            "next",
            writer => { writer.WriteString("continuation", continuation.Token); },
            cancellationToken).ConfigureAwait(false);

        return ParseCommentRepliesResponse(doc.RootElement, continuation.Target);
    }

    private static void CheckIfCommentsDisabled(JsonElement root)
    {
        if (!root.TryGetProperty("contents", out var contents) ||
            !contents.TryGetProperty("twoColumnWatchNextResults", out var watchNext) ||
            !watchNext.TryGetProperty("results", out var results) ||
            !results.TryGetProperty("results", out var innerResults) ||
            !innerResults.TryGetProperty("contents", out var resultItems) ||
            resultItems.ValueKind != JsonValueKind.Array) return;
        foreach (var item in resultItems.EnumerateArray())
            if (item.TryGetProperty("itemSectionRenderer", out var isr))
            {
                var targetId = isr.TryGetProperty("targetId", out var tid) ? tid.GetString() ?? "" : "";
                var secId = isr.TryGetProperty("sectionIdentifier", out var sid) ? sid.GetString() ?? "" : "";

                if (!targetId.Contains("comment", StringComparison.OrdinalIgnoreCase) &&
                    !secId.Contains("comment", StringComparison.OrdinalIgnoreCase)) continue;
                if (!isr.TryGetProperty("contents", out var isrContents) ||
                    isrContents.ValueKind != JsonValueKind.Array) continue;
                foreach (var c in isrContents.EnumerateArray())
                    if (c.TryGetProperty("messageRenderer", out var mr))
                    {
                        var text = mr.GetText();
                        if (text.Contains("turned off", StringComparison.OrdinalIgnoreCase) ||
                            text.Contains("disabled", StringComparison.OrdinalIgnoreCase))
                            throw new CommentsUnavailableException(text);
                    }
                    else if (c.TryGetProperty("commentsEntryPointRenderer", out var cepr))
                    {
                        var text = cepr.GetText("headerText");
                        if (text.Contains("turned off", StringComparison.OrdinalIgnoreCase) ||
                            text.Contains("disabled", StringComparison.OrdinalIgnoreCase))
                            throw new CommentsUnavailableException(text);
                    }
            }
    }

    private static string? FindInitialCommentsToken(JsonElement root)
    {
        if (!root.TryGetProperty("contents", out var contents) ||
            !contents.TryGetProperty("twoColumnWatchNextResults", out var watchNext) ||
            !watchNext.TryGetProperty("results", out var results) ||
            !results.TryGetProperty("results", out var innerResults) ||
            !innerResults.TryGetProperty("contents", out var resultItems) ||
            resultItems.ValueKind != JsonValueKind.Array) return null;
        foreach (var item in resultItems.EnumerateArray())
            if (item.TryGetProperty("itemSectionRenderer", out var isr))
            {
                var targetId = isr.TryGetProperty("targetId", out var tid) ? tid.GetString() ?? "" : "";
                var secId = isr.TryGetProperty("sectionIdentifier", out var sid) ? sid.GetString() ?? "" : "";

                if (!targetId.Contains("comment", StringComparison.OrdinalIgnoreCase) &&
                    !secId.Contains("comment", StringComparison.OrdinalIgnoreCase)) continue;
                if (!isr.TryGetProperty("contents", out var isrContents) ||
                    isrContents.ValueKind != JsonValueKind.Array) continue;
                foreach (var c in isrContents.EnumerateArray())
                {
                    var (tok, _) = c.ExtractContinuation();
                    if (!string.IsNullOrEmpty(tok))
                        return tok;
                }
            }

        return null;
    }

    private static string? FindSortContinuationToken(JsonElement root, string sortName)
    {
        if (!root.TryGetProperty("onResponseReceivedEndpoints", out var endpoints) ||
            endpoints.ValueKind != JsonValueKind.Array) return null;
        foreach (var ep in endpoints.EnumerateArray())
        {
            var cmd = ep.GetPropertyOrDefault("reloadContinuationItemsCommand");
            if (cmd.ValueKind != JsonValueKind.Object)
                cmd = ep.GetPropertyOrDefault("appendContinuationItemsAction");

            if (!cmd.TryGetProperty("continuationItems", out var items) ||
                items.ValueKind != JsonValueKind.Array) continue;
            foreach (var item in items.EnumerateArray())
                if (item.TryGetProperty("commentsHeaderRenderer", out var chr) &&
                    chr.TryGetProperty("sortMenu", out var sortMenu) &&
                    sortMenu.TryGetProperty("sortFilterSubMenuRenderer", out var sfsr) &&
                    sfsr.TryGetProperty("subMenuItems", out var subItems) &&
                    subItems.ValueKind == JsonValueKind.Array)
                    foreach (var subItem in from subItem in subItems.EnumerateArray()
                             let title = subItem.GetText("title")
                             where title.Contains(sortName, StringComparison.OrdinalIgnoreCase)
                             select subItem)
                    {
                        var (tok, _) = subItem.ExtractContinuation();
                        if (!string.IsNullOrEmpty(tok))
                            return tok;

                        if (!subItem.TryGetProperty("serviceEndpoint", out var se)) continue;
                        var (tok2, _) = se.ExtractContinuation();
                        if (!string.IsNullOrEmpty(tok2))
                            return tok2;
                    }
        }

        return null;
    }

    private static Page<CommentThread, CommentThreadsContinuation> ParseCommentThreadsResponse(
        JsonElement root,
        string? videoId,
        CommentSort sort)
    {
        var entityMap = BuildEntityMap(root);
        var threads = new List<CommentThread>();
        string? continuationToken = null;
        string? trackingParams = null;

        if (root.TryGetProperty("onResponseReceivedEndpoints", out var endpoints) &&
            endpoints.ValueKind == JsonValueKind.Array)
            foreach (var ep in endpoints.EnumerateArray())
            {
                var cmd = ep.GetPropertyOrDefault("reloadContinuationItemsCommand");
                if (cmd.ValueKind != JsonValueKind.Object)
                    cmd = ep.GetPropertyOrDefault("appendContinuationItemsAction");

                if (!cmd.TryGetProperty("continuationItems", out var items) ||
                    items.ValueKind != JsonValueKind.Array) continue;
                foreach (var item in items.EnumerateArray())
                {
                    var (tok, trk) = item.ExtractContinuation();
                    if (!string.IsNullOrEmpty(tok))
                    {
                        continuationToken = tok;
                        trackingParams = trk;
                        continue;
                    }

                    if (!item.TryGetProperty("commentThreadRenderer", out var ctr)) continue;
                    var thread = ParseCommentThread(ctr, entityMap);
                    if (thread != null) threads.Add(thread);
                }
            }

        CommentThreadsContinuation? next = null;
        if (!string.IsNullOrEmpty(continuationToken))
            next = new CommentThreadsContinuation(continuationToken, videoId, sort, trackingParams);

        return new Page<CommentThread, CommentThreadsContinuation>(threads, next);
    }

    private static Page<Comment, CommentRepliesContinuation> ParseCommentRepliesResponse(
        JsonElement root,
        string? target)
    {
        var entityMap = BuildEntityMap(root);
        var replies = new List<Comment>();
        string? continuationToken = null;
        string? trackingParams = null;

        if (root.TryGetProperty("onResponseReceivedEndpoints", out var endpoints) &&
            endpoints.ValueKind == JsonValueKind.Array)
            foreach (var ep in endpoints.EnumerateArray())
            {
                var cmd = ep.GetPropertyOrDefault("appendContinuationItemsAction");
                if (cmd.ValueKind != JsonValueKind.Object)
                    cmd = ep.GetPropertyOrDefault("reloadContinuationItemsCommand");

                if (!cmd.TryGetProperty("continuationItems", out var items) ||
                    items.ValueKind != JsonValueKind.Array) continue;
                foreach (var item in items.EnumerateArray())
                {
                    var (tok, trk) = item.ExtractContinuation();
                    if (!string.IsNullOrEmpty(tok))
                    {
                        continuationToken = tok;
                        trackingParams = trk;
                        continue;
                    }

                    if (item.TryGetProperty("commentRenderer", out var cr))
                    {
                        var comment = ParseCommentFromRenderer(cr);
                        if (comment != null)
                            replies.Add(comment);
                    }
                    else if (item.TryGetProperty("commentViewModel", out var cvm))
                    {
                        var comment = ParseCommentFromViewModel(cvm, entityMap);
                        if (comment != null)
                            replies.Add(comment);
                    }
                }
            }

        CommentRepliesContinuation? next = null;
        if (!string.IsNullOrEmpty(continuationToken))
            next = new CommentRepliesContinuation(continuationToken, target, trackingParams);

        return new Page<Comment, CommentRepliesContinuation>(replies, next);
    }

    private static CommentThread? ParseCommentThread(JsonElement ctr, Dictionary<string, JsonElement> entityMap)
    {
        Comment? topLevel = null;

        if (ctr.TryGetProperty("comment", out var commentWrapper) &&
            commentWrapper.TryGetProperty("commentRenderer", out var cr))
            topLevel = ParseCommentFromRenderer(cr);
        else if (ctr.TryGetProperty("commentViewModel", out var cvmWrapper) &&
                 cvmWrapper.TryGetProperty("commentViewModel", out var cvm))
            topLevel = ParseCommentFromViewModel(cvm, entityMap);

        if (topLevel == null)
            return null;

        int? replyCount = null;
        if (ctr.TryGetProperty("replyCount", out var rc) && rc.TryGetInt32(out var rcVal)) replyCount = rcVal;

        var initialReplies = new List<Comment>();
        CommentRepliesContinuation? nextReplies = null;

        if (!ctr.TryGetProperty("replies", out var repliesWrapper) ||
            !repliesWrapper.TryGetProperty("commentRepliesRenderer", out var crr) ||
            !crr.TryGetProperty("contents", out var replyContents) ||
            replyContents.ValueKind != JsonValueKind.Array)
            return new CommentThread(topLevel, replyCount, initialReplies, nextReplies);
        foreach (var rcItem in replyContents.EnumerateArray())
        {
            var (tok, trk) = rcItem.ExtractContinuation();
            if (!string.IsNullOrEmpty(tok))
            {
                nextReplies = new CommentRepliesContinuation(tok, topLevel.Id.Value, trk);
            }
            else if (rcItem.TryGetProperty("commentRenderer", out var replyCr))
            {
                var rep = ParseCommentFromRenderer(replyCr);
                if (rep != null)
                    initialReplies.Add(rep);
            }
            else if (rcItem.TryGetProperty("commentViewModel", out var replyCvm))
            {
                var rep = ParseCommentFromViewModel(replyCvm, entityMap);
                if (rep != null)
                    initialReplies.Add(rep);
            }
        }

        return new CommentThread(topLevel, replyCount, initialReplies, nextReplies);
    }

    private static Comment? ParseCommentFromRenderer(JsonElement cr)
    {
        var commentIdStr = cr.TryGetProperty("commentId", out var cid) ? cid.GetString() : null;
        if (string.IsNullOrEmpty(commentIdStr) || !CommentId.TryParse(commentIdStr, out var commentId))
            return null;

        var authorName = cr.GetText("authorText");
        string? channelIdStr = null;
        if (cr.TryGetProperty("authorEndpoint", out var ep) &&
            ep.TryGetProperty("browseEndpoint", out var be) &&
            be.TryGetProperty("browseId", out var bid))
            channelIdStr = bid.GetString();

        var channelId = ChannelId.TryParse(channelIdStr, out var cidVal) ? cidVal : (ChannelId?)null;
        var avatars = cr.GetThumbnails("authorThumbnail");
        var avatar = avatars.Count > 0 ? avatars[0] : null;
        var author = new CommentAuthor(channelId, authorName,
            channelId != null ? new Uri($"https://www.youtube.com/channel/{channelId}") : null, avatar);

        var text = cr.GetText("contentText");
        var publishedText = cr.GetText("publishedTimeText");
        var publishedAt = InnerTubeElement.ParseRelativeDate(publishedText);

        var likeCount = cr.TryGetProperty("likeCount", out var lc) && lc.TryGetInt64(out var lcVal)
            ? lcVal
            : InnerTubeElement.ParseCount(cr.GetText("voteCount"));

        var isPinned = cr.TryGetProperty("pinnedCommentBadge", out _);
        var isHearted = cr.TryGetProperty("actionButtons", out var ab) &&
                        ab.TryGetProperty("commentActionButtonsRenderer", out var cabr) &&
                        cabr.TryGetProperty("creatorHeart", out _);
        var isEdited = text.Contains("(edited)", StringComparison.OrdinalIgnoreCase) || cr.GetText("publishedTimeText")
            .Contains("(edited)", StringComparison.OrdinalIgnoreCase);

        return new Comment(
            commentId,
            author,
            text,
            publishedText,
            publishedAt,
            likeCount,
            isPinned,
            isHearted,
            isEdited);
    }

    private static Comment? ParseCommentFromViewModel(JsonElement cvm, Dictionary<string, JsonElement> entityMap)
    {
        var commentIdStr = cvm.TryGetProperty("commentId", out var cid) ? cid.GetString() : null;
        var commentKey = cvm.TryGetProperty("commentKey", out var ck) ? ck.GetString() : null;

        JsonElement entity = default;
        if (!string.IsNullOrEmpty(commentKey) && entityMap.TryGetValue(commentKey, out var foundEntity))
            entity = foundEntity;

        if (entity.ValueKind != JsonValueKind.Object && !string.IsNullOrEmpty(commentIdStr))
            // Search entityMap for matching commentId
            foreach (var kvp in entityMap)
                if (kvp.Value.TryGetProperty("properties", out var props) &&
                    props.TryGetProperty("commentId", out var cIdProp) &&
                    commentIdStr.Equals(cIdProp.GetString(), StringComparison.Ordinal))
                {
                    entity = kvp.Value;
                    break;
                }

        if (entity.ValueKind == JsonValueKind.Object)
        {
            var props = entity.GetPropertyOrDefault("properties");
            if (string.IsNullOrEmpty(commentIdStr))
                commentIdStr = props.TryGetProperty("commentId", out var pCid) ? pCid.GetString() : null;

            if (string.IsNullOrEmpty(commentIdStr) || !CommentId.TryParse(commentIdStr, out var commentId))
                return null;

            var text = props.GetPropertyOrDefault("content").GetText("content");
            var publishedText = props.GetText("publishedTime");
            var publishedAt = InnerTubeElement.ParseRelativeDate(publishedText);

            var authorEl = entity.GetPropertyOrDefault("author");
            var authorName = authorEl.GetText("displayName");
            var channelIdStr = authorEl.TryGetProperty("channelId", out var chIdEl) ? chIdEl.GetString() : null;
            var channelId = ChannelId.TryParse(channelIdStr, out var cidVal) ? cidVal : (ChannelId?)null;

            Thumbnail? avatar = null;
            var avatarUrl = authorEl.TryGetProperty("avatarThumbnailUrl", out var avUrlEl) ? avUrlEl.GetString() : null;
            if (!string.IsNullOrEmpty(avatarUrl) && Uri.TryCreate(avatarUrl, UriKind.Absolute, out var avUri))
                avatar = new Thumbnail(avUri, 88, 88);

            var author = new CommentAuthor(channelId, authorName,
                channelId != null ? new Uri($"https://www.youtube.com/channel/{channelId}") : null, avatar);

            var toolbar = entity.GetPropertyOrDefault("toolbar");
            var likeText = toolbar.GetText("likeCountNotliked");
            var likeCount = InnerTubeElement.ParseCount(likeText);

            var isPinned = cvm.TryGetProperty("pinnedText", out var pt) && !string.IsNullOrEmpty(pt.GetString());
            var isHearted = toolbar.TryGetProperty("heartActiveTooltip", out _);
            var isEdited = text.Contains("(edited)", StringComparison.OrdinalIgnoreCase);

            return new Comment(
                commentId,
                author,
                text,
                publishedText,
                publishedAt,
                likeCount,
                isPinned,
                isHearted,
                isEdited);
        }

        if (string.IsNullOrEmpty(commentIdStr) || !CommentId.TryParse(commentIdStr, out var fallbackId)) return null;
        {
            var author = new CommentAuthor(null, "User", null, null);
            return new Comment(
                fallbackId,
                author,
                string.Empty,
                string.Empty,
                null,
                null,
                false,
                false,
                false);
        }
    }

    private static Dictionary<string, JsonElement> BuildEntityMap(JsonElement root)
    {
        var map = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        if (!root.TryGetProperty("frameworkUpdates", out var fu) ||
            !fu.TryGetProperty("entityBatchUpdate", out var ebu) ||
            !ebu.TryGetProperty("mutations", out var mutations) ||
            mutations.ValueKind != JsonValueKind.Array) return map;
        foreach (var mutation in mutations.EnumerateArray())
        {
            var key = mutation.TryGetProperty("entityKey", out var ek) ? ek.GetString() : null;
            if (!mutation.TryGetProperty("payload", out var payload)) continue;
            if (payload.TryGetProperty("commentEntityPayload", out var cep))
            {
                var entityKey = cep.TryGetProperty("key", out var k) ? k.GetString() : key;
                if (!string.IsNullOrEmpty(entityKey)) map[entityKey] = cep;
            }
            else if (payload.TryGetProperty("commentSurfaceEntityPayload", out var csep))
            {
                var entityKey = csep.TryGetProperty("key", out var k) ? k.GetString() : key;
                if (!string.IsNullOrEmpty(entityKey)) map[entityKey] = csep;
            }
        }

        return map;
    }
}