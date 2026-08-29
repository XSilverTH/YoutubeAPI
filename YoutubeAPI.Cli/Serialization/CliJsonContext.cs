using System.Text.Json.Serialization;
using YoutubeAPI.Cli.Converters;
using YoutubeAPI.Cli.Models;
using YoutubeAPI.Models.Account;
using YoutubeAPI.Models.Channels;
using YoutubeAPI.Models.Comments;
using YoutubeAPI.Models.Common;
using YoutubeAPI.Models.Continuations;
using YoutubeAPI.Models.Enums;
using YoutubeAPI.Models.Feeds;
using YoutubeAPI.Models.Playlists;
using YoutubeAPI.Models.Search;
using YoutubeAPI.Models.Videos;

namespace YoutubeAPI.Cli.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters =
    [
        typeof(VideoIdJsonConverter),
        typeof(ChannelIdJsonConverter),
        typeof(ChannelReferenceJsonConverter),
        typeof(PlaylistIdJsonConverter),
        typeof(PlaylistItemIdJsonConverter),
        typeof(HistoryEntryIdJsonConverter),
        typeof(CommentIdJsonConverter),
        typeof(TranscriptTrackIdJsonConverter),
        typeof(CommentRepliesContinuationJsonConverter),
        typeof(JsonStringEnumConverter<VideoRating>),
        typeof(JsonStringEnumConverter<SearchKind>),
        typeof(JsonStringEnumConverter<PlaylistPrivacy>),
        typeof(JsonStringEnumConverter<LiveBroadcastState>),
        typeof(JsonStringEnumConverter<CommentSort>),
        typeof(JsonStringEnumConverter<ChannelVideoSort>)
    ])]
[JsonSerializable(typeof(Video))]
[JsonSerializable(typeof(VideoSummary))]
[JsonSerializable(typeof(Channel))]
[JsonSerializable(typeof(ChannelSummary))]
[JsonSerializable(typeof(Playlist))]
[JsonSerializable(typeof(PlaylistSummary))]
[JsonSerializable(typeof(PlaylistItem))]
[JsonSerializable(typeof(TranscriptTrack))]
[JsonSerializable(typeof(TranscriptCue))]
[JsonSerializable(typeof(Transcript))]
[JsonSerializable(typeof(SearchResult))]
[JsonSerializable(typeof(VideoSearchResult))]
[JsonSerializable(typeof(ChannelSearchResult))]
[JsonSerializable(typeof(PlaylistSearchResult))]
[JsonSerializable(typeof(FeedItem))]
[JsonSerializable(typeof(VideoFeedItem))]
[JsonSerializable(typeof(ChannelFeedItem))]
[JsonSerializable(typeof(PlaylistFeedItem))]
[JsonSerializable(typeof(CommentThread))]
[JsonSerializable(typeof(Comment))]
[JsonSerializable(typeof(CommentRepliesContinuation))]
[JsonSerializable(typeof(CommentAuthor))]
[JsonSerializable(typeof(HistoryEntry))]
[JsonSerializable(typeof(Profile))]
[JsonSerializable(typeof(Thumbnail))]
[JsonSerializable(typeof(VideoStatistics))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSerializable(typeof(List<TranscriptTrack>))]
[JsonSerializable(typeof(IReadOnlyList<TranscriptTrack>))]
[JsonSerializable(typeof(PageEnvelope<SearchResult>))]
[JsonSerializable(typeof(PageEnvelope<VideoSummary>))]
[JsonSerializable(typeof(PageEnvelope<PlaylistSummary>))]
[JsonSerializable(typeof(PageEnvelope<PlaylistItem>))]
[JsonSerializable(typeof(PageEnvelope<CommentThread>))]
[JsonSerializable(typeof(PageEnvelope<Comment>))]
[JsonSerializable(typeof(PageEnvelope<FeedItem>))]
[JsonSerializable(typeof(PageEnvelope<ChannelSummary>))]
[JsonSerializable(typeof(PageEnvelope<HistoryEntry>))]
[JsonSerializable(typeof(NdjsonContinuationControl))]
[JsonSerializable(typeof(CliError))]
[JsonSerializable(typeof(CliErrorEnvelope))]
[JsonSerializable(typeof(PlaylistCreateResult))]
[JsonSerializable(typeof(PlaylistActionResult))]
[JsonSerializable(typeof(AccountActionResult))]
[JsonSerializable(typeof(HistoryActionResult))]
[JsonSerializable(typeof(RatingActionResult))]
[JsonSerializable(typeof(RatingGetResult))]
public sealed partial class CliJsonContext : JsonSerializerContext;