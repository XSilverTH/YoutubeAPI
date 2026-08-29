using System.Text.Json;
using System.Text.Json.Serialization;
using YoutubeAPI.Models.Continuations;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Cli.Converters;

public sealed class VideoIdJsonConverter : JsonConverter<VideoId>
{
    public override VideoId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        return s is not null ? VideoId.Parse(s) : default;
    }

    public override void Write(Utf8JsonWriter writer, VideoId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

public sealed class ChannelIdJsonConverter : JsonConverter<ChannelId>
{
    public override ChannelId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        return s is not null ? ChannelId.Parse(s) : default;
    }

    public override void Write(Utf8JsonWriter writer, ChannelId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

public sealed class ChannelReferenceJsonConverter : JsonConverter<ChannelReference>
{
    public override ChannelReference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        return s is not null ? ChannelReference.Parse(s) : default;
    }

    public override void Write(Utf8JsonWriter writer, ChannelReference value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

public sealed class PlaylistIdJsonConverter : JsonConverter<PlaylistId>
{
    public override PlaylistId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        return s is not null ? PlaylistId.Parse(s) : default;
    }

    public override void Write(Utf8JsonWriter writer, PlaylistId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

public sealed class PlaylistItemIdJsonConverter : JsonConverter<PlaylistItemId>
{
    public override PlaylistItemId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        return s is not null ? PlaylistItemId.Parse(s) : default;
    }

    public override void Write(Utf8JsonWriter writer, PlaylistItemId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

public sealed class HistoryEntryIdJsonConverter : JsonConverter<HistoryEntryId>
{
    public override HistoryEntryId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        return s is not null ? HistoryEntryId.Parse(s) : default;
    }

    public override void Write(Utf8JsonWriter writer, HistoryEntryId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

public sealed class CommentIdJsonConverter : JsonConverter<CommentId>
{
    public override CommentId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        return s is not null ? CommentId.Parse(s) : default;
    }

    public override void Write(Utf8JsonWriter writer, CommentId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

public sealed class TranscriptTrackIdJsonConverter : JsonConverter<TranscriptTrackId>
{
    public override TranscriptTrackId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        return s is not null ? TranscriptTrackId.Parse(s) : default;
    }

    public override void Write(Utf8JsonWriter writer, TranscriptTrackId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

public sealed class CommentRepliesContinuationJsonConverter : JsonConverter<CommentRepliesContinuation>
{
    public override CommentRepliesContinuation? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var s = reader.GetString();
        return s is not null ? CommentRepliesContinuation.Import(s) : null;
    }

    public override void Write(Utf8JsonWriter writer, CommentRepliesContinuation value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Export());
    }
}