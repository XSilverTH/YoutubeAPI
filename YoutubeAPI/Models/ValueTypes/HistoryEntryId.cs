using System.Diagnostics.CodeAnalysis;

namespace YoutubeAPI.Models.ValueTypes;

/// <summary>
///     Represents an immutable unique identifier for a user history entry.
/// </summary>
public readonly record struct HistoryEntryId
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="HistoryEntryId" /> struct.
    /// </summary>
    /// <param name="value">The history entry identifier string.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is null or whitespace.</exception>
    public HistoryEntryId(string value)
    {
        if (!TryParse(value, out var parsed))
            throw new ArgumentException("History entry identifier cannot be empty or whitespace.", nameof(value));

        Value = parsed.Value;
    }

    private HistoryEntryId(string value, bool validated)
    {
        _ = validated;
        Value = value;
    }

    /// <summary>
    ///     Gets the canonical history entry identifier string.
    /// </summary>
    public string Value => field ?? string.Empty;

    /// <summary>
    ///     Parses a history entry identifier string into a <see cref="HistoryEntryId" />.
    /// </summary>
    /// <param name="value">The history entry identifier string.</param>
    /// <returns>A new <see cref="HistoryEntryId" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> is empty or whitespace.</exception>
    public static HistoryEntryId Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return !TryParse(value, out var result)
            ? throw new FormatException("History entry identifier cannot be empty or whitespace.")
            : result;
    }

    /// <summary>
    ///     Tries to parse a history entry identifier string into a <see cref="HistoryEntryId" />.
    /// </summary>
    /// <param name="value">The history entry identifier string.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="HistoryEntryId" /> if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? value, out HistoryEntryId result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        result = new HistoryEntryId(trimmed, true);
        return true;
    }

    /// <summary>Parses a history entry identifier from a character span.</summary>
    public static HistoryEntryId Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return Parse(s.ToString());
    }

    /// <summary>Tries to parse a history entry identifier from a character span.</summary>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out HistoryEntryId result)
    {
        return TryParse(s.ToString(), out result);
    }

    /// <summary>Returns the canonical history entry identifier.</summary>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>Compares this history entry identifier with another identifier.</summary>
    public int CompareTo(HistoryEntryId other)
    {
        return string.Compare(Value, other.Value, StringComparison.Ordinal);
    }

    /// <summary>Compares this history entry identifier with another object.</summary>
    public int CompareTo(object? obj)
    {
        return obj switch
        {
            null => 1,
            HistoryEntryId other => CompareTo(other),
            _ => throw new ArgumentException($"Object must be of type {nameof(HistoryEntryId)}", nameof(obj))
        };
    }

    /// <summary>
    ///     Implicitly converts a <see cref="HistoryEntryId" /> to its string representation.
    /// </summary>
    /// <param name="id">The <see cref="HistoryEntryId" /> instance.</param>
    public static implicit operator string(HistoryEntryId id)
    {
        return id.Value;
    }

    /// <summary>
    ///     Explicitly converts a string to a <see cref="HistoryEntryId" />.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    public static explicit operator HistoryEntryId(string value)
    {
        return Parse(value);
    }
}