using System.Diagnostics.CodeAnalysis;
using YoutubeAPI.Infrastructure;

namespace YoutubeAPI.Models.ValueTypes;

/// <summary>
///     Represents an immutable YouTube playlist identifier.
/// </summary>
public readonly record struct PlaylistId
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PlaylistId" /> struct.
    /// </summary>
    /// <param name="value">The playlist identifier or supported URL.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not a valid playlist ID.</exception>
    public PlaylistId(string value)
    {
        if (!TryParse(value, out var parsed))
            throw new ArgumentException($"'{value}' is not a valid YouTube playlist identifier or supported URL.",
                nameof(value));

        Value = parsed.Value;
    }

    private PlaylistId(string value, bool validated)
    {
        _ = validated;
        Value = value;
    }

    /// <summary>
    ///     Gets the canonical playlist identifier string.
    /// </summary>
    public string Value => field ?? string.Empty;

    /// <summary>
    ///     Parses a playlist ID string or supported YouTube URL into a <see cref="PlaylistId" />.
    /// </summary>
    /// <param name="value">The playlist ID string or URL to parse.</param>
    /// <returns>A new <see cref="PlaylistId" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> cannot be parsed as a playlist ID.</exception>
    public static PlaylistId Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return !TryParse(value, out var result)
            ? throw new FormatException($"'{value}' is not a valid YouTube playlist identifier or supported URL.")
            : result;
    }

    /// <summary>
    ///     Tries to parse a playlist ID string or supported YouTube URL into a <see cref="PlaylistId" />.
    /// </summary>
    /// <param name="value">The playlist ID string or URL to parse.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="PlaylistId" /> if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? value, out PlaylistId result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();

        // 1. Try parsing as URL
        if (YouTubeUrlParser.TryParseUri(trimmed, out var uri))
        {
            var queryParams = YouTubeUrlParser.ParseQueryString(uri.Query);
            if (!queryParams.TryGetValue("list", out var listId) || !IsValidRawId(listId)) return false;
            result = new PlaylistId(listId, true);
            return true;
        }

        // 2. Try parsing as raw playlist ID
        if (!IsValidRawId(trimmed)) return false;
        result = new PlaylistId(trimmed, true);
        return true;
    }

    /// <summary>Parses a playlist identifier from a character span.</summary>
    public static PlaylistId Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return Parse(s.ToString());
    }

    /// <summary>Tries to parse a playlist identifier from a character span.</summary>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out PlaylistId result)
    {
        return TryParse(s.ToString(), out result);
    }

    private static bool IsValidRawId(string s)
    {
        return s.Length >= 2 && s.All(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_');
    }

    /// <summary>Returns the canonical playlist identifier.</summary>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>Compares this playlist identifier with another identifier.</summary>
    public int CompareTo(PlaylistId other)
    {
        return string.Compare(Value, other.Value, StringComparison.Ordinal);
    }

    /// <summary>Compares this playlist identifier with another object.</summary>
    public int CompareTo(object? obj)
    {
        return obj switch
        {
            null => 1,
            PlaylistId other => CompareTo(other),
            _ => throw new ArgumentException($"Object must be of type {nameof(PlaylistId)}", nameof(obj))
        };
    }

    /// <summary>
    ///     Implicitly converts a <see cref="PlaylistId" /> to its string representation.
    /// </summary>
    /// <param name="id">The <see cref="PlaylistId" /> instance.</param>
    public static implicit operator string(PlaylistId id)
    {
        return id.Value;
    }

    /// <summary>
    ///     Explicitly converts a string to a <see cref="PlaylistId" />.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    public static explicit operator PlaylistId(string value)
    {
        return Parse(value);
    }
}