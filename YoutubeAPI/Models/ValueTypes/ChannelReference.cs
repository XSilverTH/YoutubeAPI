using System.Diagnostics.CodeAnalysis;
using YoutubeAPI.Infrastructure;

namespace YoutubeAPI.Models.ValueTypes;

/// <summary>
///     Represents an immutable reference to a YouTube channel by ID, handle (@username), or supported channel URL.
/// </summary>
public readonly record struct ChannelReference
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ChannelReference" /> struct.
    /// </summary>
    /// <param name="value">The channel ID, handle, or supported URL.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not a valid channel reference.</exception>
    public ChannelReference(string value)
    {
        if (!TryParse(value, out var parsed))
            throw new ArgumentException($"'{value}' is not a valid YouTube channel reference or supported URL.",
                nameof(value));

        Value = parsed.Value;
    }

    private ChannelReference(string value, bool validated)
    {
        _ = validated;
        Value = value;
    }

    /// <summary>
    ///     Gets the normalized channel reference string (e.g. "UC...", "@handle", or custom name).
    /// </summary>
    public string Value => field ?? string.Empty;

    /// <summary>
    ///     Creates a channel reference from a strongly typed <see cref="ChannelId" />.
    /// </summary>
    /// <param name="channelId">The channel identifier.</param>
    /// <returns>A new <see cref="ChannelReference" /> instance.</returns>
    public static ChannelReference FromId(ChannelId channelId)
    {
        return new ChannelReference(channelId.Value, true);
    }

    /// <summary>
    ///     Creates a channel reference from a channel handle.
    /// </summary>
    /// <param name="handle">The channel handle (with or without '@').</param>
    /// <returns>A new <see cref="ChannelReference" /> instance.</returns>
    public static ChannelReference FromHandle(string handle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handle);
        var normalized = handle.StartsWith('@') ? handle : "@" + handle;
        return new ChannelReference(normalized, true);
    }

    /// <summary>
    ///     Parses a channel reference string or supported URL into a <see cref="ChannelReference" />.
    /// </summary>
    /// <param name="value">The channel reference string or URL.</param>
    /// <returns>A new <see cref="ChannelReference" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> cannot be parsed as a channel reference.</exception>
    public static ChannelReference Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return !TryParse(value, out var result)
            ? throw new FormatException($"'{value}' is not a valid YouTube channel reference or supported URL.")
            : result;
    }

    /// <summary>
    ///     Tries to parse a channel reference string or supported URL into a <see cref="ChannelReference" />.
    /// </summary>
    /// <param name="value">The channel reference string or URL to parse.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="ChannelReference" /> if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? value, out ChannelReference result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (YouTubeUrlParser.TryParseUri(trimmed, out var uri))
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                return false;

            if (segments[0].Equals("channel", StringComparison.OrdinalIgnoreCase))
            {
                if (segments.Length < 2 || !ChannelId.TryParse(segments[1], out var channelId)) return false;
                result = new ChannelReference(channelId.Value, true);
                return true;
            }

            if (segments[0].StartsWith('@') && segments[0].Length >= 2 &&
                segments[0][1..].All(IsHandleCharacter))
            {
                result = new ChannelReference(segments[0], true);
                return true;
            }

            if (segments[0] is "c" or "user" or "u" &&
                segments.Length >= 2 &&
                !string.IsNullOrWhiteSpace(segments[1]))
            {
                result = new ChannelReference(segments[1], true);
                return true;
            }

            if (segments.Length != 1 || string.IsNullOrWhiteSpace(segments[0]) ||
                segments[0].Equals("c", StringComparison.OrdinalIgnoreCase) ||
                segments[0].Equals("user", StringComparison.OrdinalIgnoreCase) ||
                segments[0].Equals("u", StringComparison.OrdinalIgnoreCase)) return false;
            result = new ChannelReference(segments[0], true);
            return true;
        }

        if (trimmed.StartsWith('@') && trimmed.Length >= 2 &&
            trimmed[1..].All(IsHandleCharacter))
        {
            result = new ChannelReference(trimmed, true);
            return true;
        }

        if (!ChannelId.TryParse(trimmed, out var chId)) return false;
        result = new ChannelReference(chId.Value, true);
        return true;
    }

    private static bool IsHandleCharacter(char c)
    {
        return char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '-';
    }

    /// <summary>Parses a channel reference from a character span.</summary>
    public static ChannelReference Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return Parse(s.ToString());
    }

    /// <summary>Tries to parse a channel reference from a character span.</summary>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ChannelReference result)
    {
        return TryParse(s.ToString(), out result);
    }

    /// <summary>Returns the canonical channel reference.</summary>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>Compares this channel reference with another reference.</summary>
    public int CompareTo(ChannelReference other)
    {
        return string.Compare(Value, other.Value, StringComparison.Ordinal);
    }

    /// <summary>Compares this channel reference with another object.</summary>
    public int CompareTo(object? obj)
    {
        return obj switch
        {
            null => 1,
            ChannelReference other => CompareTo(other),
            _ => throw new ArgumentException($"Object must be of type {nameof(ChannelReference)}", nameof(obj))
        };
    }

    /// <summary>
    ///     Implicitly converts a <see cref="ChannelId" /> to a <see cref="ChannelReference" />.
    /// </summary>
    /// <param name="channelId">The channel ID instance.</param>
    public static implicit operator ChannelReference(ChannelId channelId)
    {
        return FromId(channelId);
    }

    /// <summary>
    ///     Implicitly converts a <see cref="ChannelReference" /> to its string representation.
    /// </summary>
    /// <param name="reference">The <see cref="ChannelReference" /> instance.</param>
    public static implicit operator string(ChannelReference reference)
    {
        return reference.Value;
    }

    /// <summary>
    ///     Explicitly converts a string to a <see cref="ChannelReference" />.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    public static explicit operator ChannelReference(string value)
    {
        return Parse(value);
    }
}