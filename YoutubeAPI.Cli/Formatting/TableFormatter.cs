using System.Globalization;
using System.Text;
using YoutubeAPI.Cli.Models;
using YoutubeAPI.Models.Account;
using YoutubeAPI.Models.Channels;
using YoutubeAPI.Models.Comments;
using YoutubeAPI.Models.Feeds;
using YoutubeAPI.Models.Playlists;
using YoutubeAPI.Models.Search;
using YoutubeAPI.Models.Videos;

namespace YoutubeAPI.Cli.Formatting;

public static class TableFormatter
{
    private static void RenderTable(string[] headers, IReadOnlyList<IReadOnlyList<string>> rows,
        TextWriter writer)
    {
        if (headers.Length == 0)
            return;

        var columnCount = headers.Length;
        var widths = new int[columnCount];

        for (var i = 0; i < columnCount; i++) widths[i] = headers[i].Length;

        foreach (var row in rows)
            for (var i = 0; i < Math.Min(columnCount, row.Count); i++)
                if (row[i].Length > widths[i])
                    widths[i] = row[i].Length;

        // Write header
        var sb = new StringBuilder();
        for (var i = 0; i < columnCount; i++)
        {
            if (i > 0) sb.Append("  ");
            sb.Append(headers[i].PadRight(widths[i]));
        }

        writer.WriteLine(sb.ToString());

        // Write separator
        sb.Clear();
        for (var i = 0; i < columnCount; i++)
        {
            if (i > 0) sb.Append("  ");
            sb.Append('-', widths[i]);
        }

        writer.WriteLine(sb.ToString());

        // Write rows
        foreach (var row in rows)
        {
            sb.Clear();
            for (var i = 0; i < columnCount; i++)
            {
                if (i > 0) sb.Append("  ");
                var cell = i < row.Count ? row[i] : string.Empty;
                sb.Append(cell.PadRight(widths[i]));
            }

            writer.WriteLine(sb.ToString());
        }
    }

    public static void RenderSearchResults(IReadOnlyList<SearchResult> items, string? next, TextWriter writer)
    {
        var headers = new[] { "Type", "ID", "Title / Name", "Author / Handle", "Details" };
        var rows = new List<IReadOnlyList<string>>();

        foreach (var item in items)
            switch (item)
            {
                case VideoSearchResult videoResult:
                    var v = videoResult.Video;
                    rows.Add([
                        "video",
                        v.Id.Value,
                        v.Title,
                        v.Channel.Title,
                        $"Duration: {FormatDuration(v.Duration)}, Views: {v.Statistics.ViewCount?.ToString(CultureInfo.InvariantCulture) ?? "-"}"
                    ]);
                    break;
                case ChannelSearchResult channelResult:
                    var c = channelResult.Channel;
                    rows.Add([
                        "channel",
                        c.Id.Value,
                        c.Title,
                        c.Handle ?? "-",
                        $"Subscribers: {c.SubscriberCount?.ToString(CultureInfo.InvariantCulture) ?? "-"}"
                    ]);
                    break;
                case PlaylistSearchResult playlistResult:
                    var p = playlistResult.Playlist;
                    rows.Add([
                        "playlist",
                        p.Id.Value,
                        p.Title,
                        p.Author?.Title ?? "-",
                        $"Items: {p.ItemCount?.ToString(CultureInfo.InvariantCulture) ?? "-"}"
                    ]);
                    break;
            }

        RenderTable(headers, rows, writer);
        if (string.IsNullOrEmpty(next)) return;
        writer.WriteLine();
        writer.WriteLine($"Next continuation: {next}");
    }

    public static void RenderVideos(IReadOnlyList<VideoSummary> items, string? next, TextWriter writer)
    {
        var headers = new[] { "ID", "Title", "Channel", "Duration", "Views", "Published" };
        var rows = items.Select(v => (IReadOnlyList<string>)
        [
            v.Id.Value, v.Title, v.Channel.Title, FormatDuration(v.Duration),
            v.Statistics.ViewCount?.ToString(CultureInfo.InvariantCulture) ?? "-", v.PublishedText ?? "-"
        ]).ToList();

        RenderTable(headers, rows, writer);
        if (string.IsNullOrEmpty(next)) return;
        writer.WriteLine();
        writer.WriteLine($"Next continuation: {next}");
    }

    public static void RenderPlaylists(IReadOnlyList<PlaylistSummary> items, string? next, TextWriter writer)
    {
        var headers = new[] { "ID", "Title", "Author", "Items" };
        var rows = items.Select(p => (IReadOnlyList<string>)
            [
                p.Id.Value, p.Title, p.Author?.Title ?? "-", p.ItemCount?.ToString(CultureInfo.InvariantCulture) ?? "-"
            ])
            .ToList();

        RenderTable(headers, rows, writer);
        if (string.IsNullOrEmpty(next)) return;
        writer.WriteLine();
        writer.WriteLine($"Next continuation: {next}");
    }

    public static void RenderPlaylistItems(IReadOnlyList<PlaylistItem> items, string? next, TextWriter writer)
    {
        var headers = new[] { "Pos", "Item ID", "Video ID", "Title", "Available" };
        var rows = items.Select(item => (IReadOnlyList<string>)
        [
            item.Position?.ToString(CultureInfo.InvariantCulture) ?? "-", item.Id?.Value ?? "-",
            item.Video?.Id.Value ?? "-", item.DisplayTitle, item.IsAvailable ? "Yes" : "No"
        ]).ToList();

        RenderTable(headers, rows, writer);
        if (string.IsNullOrEmpty(next)) return;
        writer.WriteLine();
        writer.WriteLine($"Next continuation: {next}");
    }

    public static void RenderCommentThreads(IReadOnlyList<CommentThread> items, string? next, TextWriter writer)
    {
        var headers = new[] { "ID", "Author", "Published", "Likes", "Replies", "Text" };
        var rows = items.Select(thread => (IReadOnlyList<string>)
        [
            thread.TopLevel.Id.Value, thread.TopLevel.Author.Name, thread.TopLevel.PublishedText,
            thread.TopLevel.LikeCount?.ToString(CultureInfo.InvariantCulture) ?? "0",
            thread.ReplyCount?.ToString(CultureInfo.InvariantCulture) ??
            thread.Replies.Count.ToString(CultureInfo.InvariantCulture),
            SanitizeText(thread.TopLevel.Text)
        ]).ToList();

        RenderTable(headers, rows, writer);
        if (string.IsNullOrEmpty(next)) return;
        writer.WriteLine();
        writer.WriteLine($"Next continuation: {next}");
    }

    public static void RenderComments(IReadOnlyList<Comment> items, string? next, TextWriter writer)
    {
        var headers = new[] { "ID", "Author", "Published", "Likes", "Text" };
        var rows = items.Select(comment => (IReadOnlyList<string>)
        [
            comment.Id.Value, comment.Author.Name, comment.PublishedText,
            comment.LikeCount?.ToString(CultureInfo.InvariantCulture) ?? "0", SanitizeText(comment.Text)
        ]).ToList();

        RenderTable(headers, rows, writer);
        if (string.IsNullOrEmpty(next)) return;
        writer.WriteLine();
        writer.WriteLine($"Next continuation: {next}");
    }

    public static void RenderFeedItems(IReadOnlyList<FeedItem> items, string? next, TextWriter writer)
    {
        var headers = new[] { "Type", "ID", "Title", "Channel / Author", "Details" };
        var rows = new List<IReadOnlyList<string>>();

        foreach (var item in items)
            switch (item)
            {
                case VideoFeedItem videoItem:
                    var v = videoItem.Video;
                    rows.Add([
                        "video",
                        v.Id.Value,
                        v.Title,
                        v.Channel.Title,
                        $"Duration: {FormatDuration(v.Duration)}, Views: {v.Statistics.ViewCount?.ToString(CultureInfo.InvariantCulture) ?? "-"}"
                    ]);
                    break;
                case ChannelFeedItem channelItem:
                    var c = channelItem.Channel;
                    rows.Add([
                        "channel",
                        c.Id.Value,
                        c.Title,
                        c.Handle ?? "-",
                        $"Subscribers: {c.SubscriberCount?.ToString(CultureInfo.InvariantCulture) ?? "-"}"
                    ]);
                    break;
                case PlaylistFeedItem playlistItem:
                    var p = playlistItem.Playlist;
                    rows.Add([
                        "playlist",
                        p.Id.Value,
                        p.Title,
                        p.Author?.Title ?? "-",
                        $"Items: {p.ItemCount?.ToString(CultureInfo.InvariantCulture) ?? "-"}"
                    ]);
                    break;
            }

        RenderTable(headers, rows, writer);
        if (string.IsNullOrEmpty(next)) return;
        writer.WriteLine();
        writer.WriteLine($"Next continuation: {next}");
    }

    public static void RenderChannels(IReadOnlyList<ChannelSummary> items, string? next, TextWriter writer)
    {
        var headers = new[] { "ID", "Title", "Handle", "Subscribers", "Verified" };
        var rows = items.Select(c => (IReadOnlyList<string>)
        [
            c.Id.Value, c.Title, c.Handle ?? "-", c.SubscriberCount?.ToString(CultureInfo.InvariantCulture) ?? "-",
            c.IsVerified ? "Yes" : "No"
        ]).ToList();

        RenderTable(headers, rows, writer);
        if (string.IsNullOrEmpty(next)) return;
        writer.WriteLine();
        writer.WriteLine($"Next continuation: {next}");
    }

    public static void RenderHistoryEntries(IReadOnlyList<HistoryEntry> items, string? next, TextWriter writer)
    {
        var headers = new[] { "Entry ID", "Type", "Title", "Channel / Author" };
        var rows = new List<IReadOnlyList<string>>();

        foreach (var entry in items)
        {
            var (type, title, author) = entry.Item switch
            {
                VideoFeedItem vf => ("video", vf.Video.Title, vf.Video.Channel.Title),
                ChannelFeedItem cf => ("channel", cf.Channel.Title, cf.Channel.Handle ?? cf.Channel.Title),
                PlaylistFeedItem pf => ("playlist", pf.Playlist.Title, pf.Playlist.Author?.Title ?? "-"),
                _ => ("unknown", "-", "-")
            };

            rows.Add([entry.Id.Value, type, title, author]);
        }

        RenderTable(headers, rows, writer);
        if (string.IsNullOrEmpty(next)) return;
        writer.WriteLine();
        writer.WriteLine($"Next continuation: {next}");
    }

    public static void RenderVideo(Video video, TextWriter writer)
    {
        writer.WriteLine($"ID:           {video.Summary.Id.Value}");
        writer.WriteLine($"Title:        {video.Summary.Title}");
        writer.WriteLine($"Channel:      {video.Summary.Channel.Title} ({video.Summary.Channel.Id.Value})");
        writer.WriteLine($"Duration:     {FormatDuration(video.Summary.Duration)}");
        writer.WriteLine(
            $"Upload Date:  {video.UploadDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-"}");
        writer.WriteLine($"Live State:   {video.LiveState}");
        writer.WriteLine(
            $"Views:        {video.Summary.Statistics.ViewCount?.ToString(CultureInfo.InvariantCulture) ?? "-"}");
        writer.WriteLine(
            $"Likes:        {video.Summary.Statistics.LikeCount?.ToString(CultureInfo.InvariantCulture) ?? "-"}");
        writer.WriteLine(
            $"Comments:     {video.Summary.Statistics.CommentCount?.ToString(CultureInfo.InvariantCulture) ?? "-"}");
        writer.WriteLine($"Keywords:     {(video.Keywords.Count > 0 ? string.Join(", ", video.Keywords) : "-")}");
        writer.WriteLine();
        writer.WriteLine("Description:");
        writer.WriteLine(video.Description);
    }

    public static void RenderChannel(Channel channel, TextWriter writer)
    {
        writer.WriteLine($"ID:           {channel.Summary.Id.Value}");
        writer.WriteLine($"Title:        {channel.Summary.Title}");
        writer.WriteLine($"Handle:       {channel.Summary.Handle ?? "-"}");
        writer.WriteLine(
            $"Subscribers:  {channel.Summary.SubscriberCount?.ToString(CultureInfo.InvariantCulture) ?? "-"}");
        writer.WriteLine($"Verified:     {(channel.Summary.IsVerified ? "Yes" : "No")}");
        writer.WriteLine();
        writer.WriteLine("Description:");
        writer.WriteLine(channel.Description);
    }

    public static void RenderPlaylist(Playlist playlist, TextWriter writer)
    {
        writer.WriteLine($"ID:           {playlist.Summary.Id.Value}");
        writer.WriteLine($"Title:        {playlist.Summary.Title}");
        writer.WriteLine($"Author:       {playlist.Summary.Author?.Title ?? "-"}");
        writer.WriteLine($"Item Count:   {playlist.Summary.ItemCount?.ToString(CultureInfo.InvariantCulture) ?? "-"}");
        writer.WriteLine($"Privacy:      {playlist.Privacy?.ToString() ?? "-"}");
        if (string.IsNullOrEmpty(playlist.Description)) return;
        writer.WriteLine();
        writer.WriteLine("Description:");
        writer.WriteLine(playlist.Description);
    }

    public static void RenderProfile(Profile profile, TextWriter writer)
    {
        writer.WriteLine($"Channel ID:   {profile.ChannelId?.Value ?? "-"}");
        writer.WriteLine($"Display Name: {profile.DisplayName}");
        writer.WriteLine($"Handle:       {profile.Handle ?? "-"}");
    }

    public static void RenderTranscriptTracks(IReadOnlyList<TranscriptTrack> tracks, TextWriter writer)
    {
        var headers = new[] { "ID", "Language", "Name", "Auto-Generated" };
        var rows = tracks.Select(track => (IReadOnlyList<string>)
            [track.Id.Value, track.LanguageCode, track.DisplayName, track.IsAutoGenerated ? "Yes" : "No"]).ToList();

        RenderTable(headers, rows, writer);
    }

    public static void RenderTranscript(Transcript transcript, TextWriter writer)
    {
        writer.WriteLine($"Video ID: {transcript.VideoId.Value}");
        writer.WriteLine($"Track:    {transcript.Track.DisplayName} ({transcript.Track.LanguageCode})");
        writer.WriteLine();

        var headers = new[] { "Start", "Duration", "Text" };
        var rows = transcript.Cues.Select(cue => (IReadOnlyList<string>)
            [FormatDuration(cue.Start), FormatDuration(cue.Duration), cue.Text]).ToList();

        RenderTable(headers, rows, writer);
    }

    public static void RenderSuggestions(IReadOnlyList<string> suggestions, TextWriter writer)
    {
        var headers = new[] { "Suggestion" };
        var rows = suggestions.Select(s => (IReadOnlyList<string>)[s]).ToList();
        RenderTable(headers, rows, writer);
    }

    public static void RenderPlaylistCreate(PlaylistCreateResult result, TextWriter writer)
    {
        writer.WriteLine($"Created Playlist ID: {result.Id.Value}");
    }

    public static void RenderPlaylistAction(PlaylistActionResult result, TextWriter writer)
    {
        writer.WriteLine($"Success:     {result.Success}");
        writer.WriteLine($"Playlist ID: {result.PlaylistId.Value}");
        if (result.VideoId is not null)
            writer.WriteLine($"Video ID:    {result.VideoId.Value.Value}");
        if (result.ItemId is not null)
            writer.WriteLine($"Item ID:     {result.ItemId.Value.Value}");
    }

    public static void RenderAccountAction(AccountActionResult result, TextWriter writer)
    {
        writer.WriteLine($"Success:    {result.Success}");
        writer.WriteLine($"Channel ID: {result.ChannelId.Value}");
    }

    public static void RenderHistoryAction(HistoryActionResult result, TextWriter writer)
    {
        writer.WriteLine($"Success:  {result.Success}");
        if (result.EntryId is not null)
            writer.WriteLine($"Entry ID: {result.EntryId.Value.Value}");
        if (result.Cleared is not null)
            writer.WriteLine($"Cleared:  {result.Cleared.Value}");
    }

    public static void RenderRatingAction(RatingActionResult result, TextWriter writer)
    {
        writer.WriteLine($"Success:  {result.Success}");
        writer.WriteLine($"Video ID: {result.VideoId.Value}");
        writer.WriteLine($"Rating:   {result.Rating}");
    }

    public static void RenderRatingGet(RatingGetResult result, TextWriter writer)
    {
        writer.WriteLine($"Video ID: {result.VideoId.Value}");
        writer.WriteLine($"Rating:   {result.Rating}");
    }

    private static string FormatDuration(TimeSpan? duration)
    {
        if (duration is null) return "-";
        var d = duration.Value;
        return d.TotalHours >= 1
            ? $"{(int)d.TotalHours}:{d.Minutes:D2}:{d.Seconds:D2}"
            : $"{d.Minutes}:{d.Seconds:D2}";
    }

    private static string SanitizeText(string text)
    {
        var sanitized = text.Replace("\r\n", " ", StringComparison.Ordinal).Replace('\n', ' ').Replace('\r', ' ');
        return sanitized.Length > 80 ? sanitized[..77] + "..." : sanitized;
    }
}