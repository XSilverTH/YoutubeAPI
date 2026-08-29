using System.Diagnostics.CodeAnalysis;
using YoutubeAPI.Infrastructure;

namespace YoutubeAPI.Models.ValueTypes;

/// <summary>
///     Represents an immutable 24-character YouTube channel identifier starting with "UC".
/// </summary>
public readonly record struct ChannelId
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ChannelId" /> struct with a canonical channel identifier string.
    /// </summary>
    /// <param name="value">The 24-character channel identifier starting with "UC".</param>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid channel ID.</exception>
    public ChannelId(string value)
    {
        if (!TryParse(value, out var parsed))
            throw new ArgumentException($"'{value}' is not a valid YouTube channel identifier or supported URL.",
                nameof(value));

        Value = parsed.Value;
    }

    private ChannelId(string value, bool validated)
    {
        _ = validated;
        Value = value;
    }

    /// <summary>
    ///     Gets the canonical 24-character channel identifier string.
    /// </summary>
    public string Value => field ?? string.Empty;

    /// <summary>
    ///     Parses a raw channel ID string or supported YouTube channel URL into a <see cref="ChannelId" />.
    /// </summary>
    /// <param name="value">The channel ID string or URL to parse.</param>
    /// <returns>A new <see cref="ChannelId" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> is not a valid channel ID or supported URL.</exception>
    public static ChannelId Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return !TryParse(value, out var result)
            ? throw new FormatException($"'{value}' is not a valid YouTube channel identifier or supported URL.")
            : result;
    }

    /// <summary>
    ///     Tries to parse a raw channel ID string or supported YouTube channel URL into a <see cref="ChannelId" />.
    /// </summary>
    /// <param name="value">The channel ID string or URL to parse.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="ChannelId" /> if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? value, out ChannelId result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();

        // 1. Try parsing as URL
        if (YouTubeUrlParser.TryParseUri(trimmed, out var uri))
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2 || !segments[0].Equals("channel", StringComparison.OrdinalIgnoreCase)) return false;
            if (!IsValidRawId(segments[1])) return false;
            result = new ChannelId(segments[1], true);
            return true;
        }

        // 2. Try parsing as raw 24-char ID starting with UC
        if (!IsValidRawId(trimmed)) return false;
        result = new ChannelId(trimmed, true);
        return true;
    }

    /// <summary>Parses a channel identifier from a character span.</summary>
    public static ChannelId Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return Parse(s.ToString());
    }

    /// <summary>Tries to parse a channel identifier from a character span.</summary>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ChannelId result)
    {
        return TryParse(s.ToString(), out result);
    }

    private static bool IsValidRawId(string s)
    {
        if (s.Length != 24 || !s.StartsWith("UC", StringComparison.Ordinal))
            return false;

        return s.All(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_');
    }

    /// <summary>Returns the canonical channel identifier.</summary>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>Compares this channel identifier with another identifier.</summary>
    public int CompareTo(ChannelId other)
    {
        return string.Compare(Value, other.Value, StringComparison.Ordinal);
    }

    /// <summary>Compares this channel identifier with another object.</summary>
    public int CompareTo(object? obj)
    {
        return obj switch
        {
            null => 1,
            ChannelId other => CompareTo(other),
            _ => throw new ArgumentException($"Object must be of type {nameof(ChannelId)}", nameof(obj))
        };
    }

    /// <summary>
    ///     Implicitly converts a <see cref="ChannelId" /> to its string representation.
    /// </summary>
    /// <param name="id">The <see cref="ChannelId" /> instance.</param>
    public static implicit operator string(ChannelId id)
    {
        return id.Value;
    }

    /// <summary>
    ///     Explicitly converts a string to a <see cref="ChannelId" />.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    public static explicit operator ChannelId(string value)
    {
        return Parse(value);
    }
}