using System.Buffers.Text;
using System.Text.Json;

namespace YoutubeAPI.Models.Continuations;

internal sealed record ContinuationEnvelope(
    int Version,
    string Route,
    string Token,
    string? Target = null,
    string? ProfileId = null,
    string? TrackingParams = null,
    string? Extra = null)
{
    public const int CurrentVersion = 1;

    public string Encode()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("v", Version);
            writer.WriteString("r", Route);
            writer.WriteString("t", Token);

            if (Target != null)
                writer.WriteString("tg", Target);

            if (ProfileId != null)
                writer.WriteString("p", ProfileId);

            if (TrackingParams != null)
                writer.WriteString("tp", TrackingParams);

            if (Extra != null)
                writer.WriteString("e", Extra);

            writer.WriteEndObject();
        }

        var bytes = stream.ToArray();
        return Base64Url.EncodeToString(bytes);
    }

    public static ContinuationEnvelope Decode(string base64Url)
    {
        if (string.IsNullOrWhiteSpace(base64Url))
            throw new FormatException("Continuation token cannot be empty or whitespace.");

        byte[] bytes;
        try
        {
            bytes = Base64Url.DecodeFromChars(base64Url.AsSpan());
        }
        catch (Exception ex)
        {
            throw new FormatException("Continuation token is not a valid base64url string.", ex);
        }

        try
        {
            var reader = new Utf8JsonReader(bytes);
            var version = 0;
            string? route = null;
            string? token = null;
            string? target = null;
            string? profileId = null;
            string? trackingParams = null;
            string? extra = null;

            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                throw new FormatException("Invalid continuation envelope payload.");

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var propName = reader.GetString();
                reader.Read();

                switch (propName)
                {
                    case "v":
                        version = reader.GetInt32();
                        break;
                    case "r":
                        route = reader.GetString();
                        break;
                    case "t":
                        token = reader.GetString();
                        break;
                    case "tg":
                        target = reader.GetString();
                        break;
                    case "p":
                        profileId = reader.GetString();
                        break;
                    case "tp":
                        trackingParams = reader.GetString();
                        break;
                    case "e":
                        extra = reader.GetString();
                        break;
                }
            }

            return
                version is <= 0 or > CurrentVersion
                    ? throw new FormatException($"Unsupported continuation envelope version: {version}.")
                    : string.IsNullOrEmpty(route)
                        ? throw new FormatException("Continuation envelope is missing route discriminator.")
                        : string.IsNullOrEmpty(token)
                            ? throw new FormatException("Continuation envelope is missing server token.")
                            : new ContinuationEnvelope(version, route, token, target, profileId, trackingParams, extra);
        }
        catch (FormatException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new FormatException($"Malformed continuation envelope: {ex.Message}", ex);
        }
    }
}