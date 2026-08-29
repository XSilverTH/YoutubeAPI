using System.Diagnostics.CodeAnalysis;

namespace YoutubeAPI.Models.ValueTypes;

/// <summary>
///     Represents an immutable unique identifier for an item occurrence within a YouTube playlist.
/// </summary>
public readonly record struct PlaylistItemId
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PlaylistItemId" /> struct.
    /// </summary>
    /// <param name="value">The playlist item identifier string.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is null or whitespace.</exception>
    public PlaylistItemId(string value)
    {
        if (!TryParse(value, out var parsed))
            throw new ArgumentException("Playlist item identifier cannot be empty or whitespace.", nameof(value));

        Value = parsed.Value;
    }

    private PlaylistItemId(string value, bool validated)
    {
        _ = validated;
        Value = value;
    }

    /// <summary>
    ///     Gets the canonical playlist item identifier string.
    /// </summary>
    public string Value => field ?? string.Empty;

    /// <summary>
    ///     Parses a playlist item identifier string into a <see cref="PlaylistItemId" />.
    /// </summary>
    /// <param name="value">The playlist item identifier string.</param>
    /// <returns>A new <see cref="PlaylistItemId" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> is empty or whitespace.</exception>
    public static PlaylistItemId Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return !TryParse(value, out var result)
            ? throw new FormatException("Playlist item identifier cannot be empty or whitespace.")
            : result;
    }

    /// <summary>
    ///     Tries to parse a playlist item identifier string into a <see cref="PlaylistItemId" />.
    /// </summary>
    /// <param name="value">The playlist item identifier string.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="PlaylistItemId" /> if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? value, out PlaylistItemId result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        result = new PlaylistItemId(trimmed, true);
        return true;
    }

    /// <summary>Parses a playlist item identifier from a character span.</summary>
    public static PlaylistItemId Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return Parse(s.ToString());
    }

    /// <summary>Tries to parse a playlist item identifier from a character span.</summary>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out PlaylistItemId result)
    {
        return TryParse(s.ToString(), out result);
    }

    /// <summary>Returns the canonical playlist item identifier.</summary>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>Compares this playlist item identifier with another identifier.</summary>
    public int CompareTo(PlaylistItemId other)
    {
        return string.Compare(Value, other.Value, StringComparison.Ordinal);
    }

    /// <summary>Compares this playlist item identifier with another object.</summary>
    public int CompareTo(object? obj)
    {
        return obj switch
        {
            null => 1,
            PlaylistItemId other => CompareTo(other),
            _ => throw new ArgumentException($"Object must be of type {nameof(PlaylistItemId)}", nameof(obj))
        };
    }

    /// <summary>
    ///     Implicitly converts a <see cref="PlaylistItemId" /> to its string representation.
    /// </summary>
    /// <param name="id">The <see cref="PlaylistItemId" /> instance.</param>
    public static implicit operator string(PlaylistItemId id)
    {
        return id.Value;
    }

    /// <summary>
    ///     Explicitly converts a string to a <see cref="PlaylistItemId" />.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    public static explicit operator PlaylistItemId(string value)
    {
        return Parse(value);
    }
}