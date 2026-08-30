using System.Text.Json.Serialization;
using YoutubeAPI.Models.Account;
using YoutubeAPI.Models.Channels;
using YoutubeAPI.Models.Comments;
using YoutubeAPI.Models.Continuations;
using YoutubeAPI.Models.Feeds;
using YoutubeAPI.Models.Playlists;
using YoutubeAPI.Models.Search;
using YoutubeAPI.Models.Videos;

namespace YoutubeAPI.Infrastructure;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ContinuationEnvelope))]
[JsonSerializable(typeof(SearchResult))]
[JsonSerializable(typeof(VideoSearchResult))]
[JsonSerializable(typeof(ChannelSearchResult))]
[JsonSerializable(typeof(PlaylistSearchResult))]
[JsonSerializable(typeof(FeedItem))]
[JsonSerializable(typeof(VideoFeedItem))]
[JsonSerializable(typeof(ChannelFeedItem))]
[JsonSerializable(typeof(PlaylistFeedItem))]
[JsonSerializable(typeof(Profile))]
[JsonSerializable(typeof(VideoSummary))]
[JsonSerializable(typeof(VideoPlaybackProgress))]
[JsonSerializable(typeof(Video))]
[JsonSerializable(typeof(ChannelSummary))]
[JsonSerializable(typeof(Channel))]
[JsonSerializable(typeof(PlaylistSummary))]
[JsonSerializable(typeof(Playlist))]
[JsonSerializable(typeof(PlaylistItem))]
[JsonSerializable(typeof(Comment))]
[JsonSerializable(typeof(CommentThread))]
[JsonSerializable(typeof(TranscriptTrack))]
[JsonSerializable(typeof(Transcript))]
[JsonSerializable(typeof(TranscriptCue))]
[JsonSerializable(typeof(HistoryEntry))]
[JsonSerializable(typeof(SearchRequest))]
[JsonSerializable(typeof(CreatePlaylistRequest))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSerializable(typeof(IReadOnlyList<SearchResult>))]
[JsonSerializable(typeof(IReadOnlyList<FeedItem>))]
[JsonSerializable(typeof(IReadOnlyList<VideoSummary>))]
[JsonSerializable(typeof(IReadOnlyList<PlaylistSummary>))]
[JsonSerializable(typeof(IReadOnlyList<PlaylistItem>))]
[JsonSerializable(typeof(IReadOnlyList<CommentThread>))]
[JsonSerializable(typeof(IReadOnlyList<Comment>))]
[JsonSerializable(typeof(IReadOnlyList<TranscriptTrack>))]
[JsonSerializable(typeof(IReadOnlyList<ChannelSummary>))]
[JsonSerializable(typeof(IReadOnlyList<HistoryEntry>))]
internal sealed partial class YouTubeJsonContext : JsonSerializerContext;