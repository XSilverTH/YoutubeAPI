using System.Diagnostics.CodeAnalysis;

namespace YoutubeAPI.Models.ValueTypes;

/// <summary>
///     Represents an immutable identifier for a video transcript or closed-caption track.
/// </summary>
public readonly record struct TranscriptTrackId
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TranscriptTrackId" /> struct.
    /// </summary>
    /// <param name="value">The transcript track identifier string.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is null or whitespace.</exception>
    public TranscriptTrackId(string value)
    {
        if (!TryParse(value, out var parsed))
            throw new ArgumentException("Transcript track identifier cannot be empty or whitespace.", nameof(value));

        Value = parsed.Value;
    }

    private TranscriptTrackId(string value, bool validated)
    {
        _ = validated;
        Value = value;
    }

    /// <summary>
    ///     Gets the canonical transcript track identifier string.
    /// </summary>
    public string Value => field ?? string.Empty;

    /// <summary>
    ///     Parses a transcript track identifier string into a <see cref="TranscriptTrackId" />.
    /// </summary>
    /// <param name="value">The transcript track identifier string.</param>
    /// <returns>A new <see cref="TranscriptTrackId" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> is empty or whitespace.</exception>
    public static TranscriptTrackId Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return !TryParse(value, out var result)
            ? throw new FormatException("Transcript track identifier cannot be empty or whitespace.")
            : result;
    }

    /// <summary>
    ///     Tries to parse a transcript track identifier string into a <see cref="TranscriptTrackId" />.
    /// </summary>
    /// <param name="value">The transcript track identifier string.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="TranscriptTrackId" /> if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? value, out TranscriptTrackId result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        result = new TranscriptTrackId(trimmed, true);
        return true;
    }

    /// <summary>Parses a transcript track identifier from a character span.</summary>
    public static TranscriptTrackId Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return Parse(s.ToString());
    }

    /// <summary>Tries to parse a transcript track identifier from a character span.</summary>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TranscriptTrackId result)
    {
        return TryParse(s.ToString(), out result);
    }

    /// <summary>Returns the canonical transcript track identifier.</summary>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>Compares this transcript track identifier with another identifier.</summary>
    public int CompareTo(TranscriptTrackId other)
    {
        return string.Compare(Value, other.Value, StringComparison.Ordinal);
    }

    /// <summary>Compares this transcript track identifier with another object.</summary>
    public int CompareTo(object? obj)
    {
        return obj switch
        {
            null => 1,
            TranscriptTrackId other => CompareTo(other),
            _ => throw new ArgumentException($"Object must be of type {nameof(TranscriptTrackId)}", nameof(obj))
        };
    }

    /// <summary>
    ///     Implicitly converts a <see cref="TranscriptTrackId" /> to its string representation.
    /// </summary>
    /// <param name="id">The <see cref="TranscriptTrackId" /> instance.</param>
    public static implicit operator string(TranscriptTrackId id)
    {
        return id.Value;
    }

    /// <summary>
    ///     Explicitly converts a string to a <see cref="TranscriptTrackId" />.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    public static explicit operator TranscriptTrackId(string value)
    {
        return Parse(value);
    }
}