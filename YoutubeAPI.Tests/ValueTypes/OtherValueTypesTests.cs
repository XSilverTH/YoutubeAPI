using Xunit;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Tests.ValueTypes;

public sealed class OtherValueTypesTests
{
    [Fact]
    public void PlaylistItemIdConstructorAndParseWithValidValueSucceeds()
    {
        var id = new PlaylistItemId("PLI_123456789");
        Assert.Equal("PLI_123456789", id.Value);
        Assert.Equal("PLI_123456789", id.ToString());

        var parsed = PlaylistItemId.Parse("PLI_123456789");
        Assert.Equal("PLI_123456789", parsed.Value);

        var success = PlaylistItemId.TryParse("PLI_123456789", out var tryParsed);
        Assert.True(success);
        Assert.Equal("PLI_123456789", tryParsed.Value);

        var spanParsed = PlaylistItemId.Parse("PLI_123456789".AsSpan(), null);
        Assert.Equal("PLI_123456789", spanParsed.Value);

        var spanSuccess = PlaylistItemId.TryParse("PLI_123456789".AsSpan(), null, out var spanTryParsed);
        Assert.True(spanSuccess);
        Assert.Equal("PLI_123456789", spanTryParsed.Value);
    }

    [Theory]
    [InlineData(null)]
    public void PlaylistItemIdParseWithNullThrowsArgumentNullException(string? input)
    {
        Assert.Throws<ArgumentNullException>(() => PlaylistItemId.Parse(input!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PlaylistItemIdParseWithEmptyOrWhitespaceThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => PlaylistItemId.Parse(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PlaylistItemIdConstructorWithInvalidThrowsArgumentException(string? input)
    {
        Assert.ThrowsAny<ArgumentException>(() => new PlaylistItemId(input!));
    }

    [Fact]
    public void PlaylistItemIdConversionsAndEquality()
    {
        var id1 = new PlaylistItemId("item_1");
        var id2 = new PlaylistItemId("item_1");
        var id3 = new PlaylistItemId("item_2");

        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
        Assert.NotEqual(id1, id3);
        Assert.Equal(0, id1.CompareTo(id2));

        string str = id1;
        Assert.Equal("item_1", str);
        var explicitId = (PlaylistItemId)"item_1";
        Assert.Equal("item_1", explicitId.Value);

        var defaultVal = default(PlaylistItemId);
        Assert.Equal(string.Empty, defaultVal.Value);
    }

    [Fact]
    public void HistoryEntryIdConstructorAndParseWithValidValueSucceeds()
    {
        var id = new HistoryEntryId("HE_123456789");
        Assert.Equal("HE_123456789", id.Value);
        Assert.Equal("HE_123456789", id.ToString());

        var parsed = HistoryEntryId.Parse("HE_123456789");
        Assert.Equal("HE_123456789", parsed.Value);

        var success = HistoryEntryId.TryParse("HE_123456789", out var tryParsed);
        Assert.True(success);
        Assert.Equal("HE_123456789", tryParsed.Value);

        var spanParsed = HistoryEntryId.Parse("HE_123456789".AsSpan(), null);
        Assert.Equal("HE_123456789", spanParsed.Value);

        var spanSuccess = HistoryEntryId.TryParse("HE_123456789".AsSpan(), null, out var spanTryParsed);
        Assert.True(spanSuccess);
        Assert.Equal("HE_123456789", spanTryParsed.Value);
    }

    [Theory]
    [InlineData(null)]
    public void HistoryEntryIdParseWithNullThrowsArgumentNullException(string? input)
    {
        Assert.Throws<ArgumentNullException>(() => HistoryEntryId.Parse(input!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void HistoryEntryIdParseWithEmptyOrWhitespaceThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => HistoryEntryId.Parse(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HistoryEntryIdConstructorWithInvalidThrowsArgumentException(string? input)
    {
        Assert.ThrowsAny<ArgumentException>(() => new HistoryEntryId(input!));
    }

    [Fact]
    public void HistoryEntryIdConversionsAndEquality()
    {
        var id1 = new HistoryEntryId("entry_1");
        var id2 = new HistoryEntryId("entry_1");
        var id3 = new HistoryEntryId("entry_2");

        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
        Assert.NotEqual(id1, id3);
        Assert.Equal(0, id1.CompareTo(id2));

        string str = id1;
        Assert.Equal("entry_1", str);
        var explicitId = (HistoryEntryId)"entry_1";
        Assert.Equal("entry_1", explicitId.Value);

        var defaultVal = default(HistoryEntryId);
        Assert.Equal(string.Empty, defaultVal.Value);
    }

    [Fact]
    public void TranscriptTrackIdConstructorAndParseWithValidValueSucceeds()
    {
        var id = new TranscriptTrackId(".en");
        Assert.Equal(".en", id.Value);
        Assert.Equal(".en", id.ToString());

        var parsed = TranscriptTrackId.Parse(".en");
        Assert.Equal(".en", parsed.Value);

        var success = TranscriptTrackId.TryParse(".en", out var tryParsed);
        Assert.True(success);
        Assert.Equal(".en", tryParsed.Value);

        var spanParsed = TranscriptTrackId.Parse(".en".AsSpan(), null);
        Assert.Equal(".en", spanParsed.Value);

        var spanSuccess = TranscriptTrackId.TryParse(".en".AsSpan(), null, out var spanTryParsed);
        Assert.True(spanSuccess);
        Assert.Equal(".en", spanTryParsed.Value);
    }

    [Theory]
    [InlineData(null)]
    public void TranscriptTrackIdParseWithNullThrowsArgumentNullException(string? input)
    {
        Assert.Throws<ArgumentNullException>(() => TranscriptTrackId.Parse(input!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TranscriptTrackIdParseWithEmptyOrWhitespaceThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => TranscriptTrackId.Parse(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TranscriptTrackIdConstructorWithInvalidThrowsArgumentException(string? input)
    {
        Assert.ThrowsAny<ArgumentException>(() => new TranscriptTrackId(input!));
    }

    [Fact]
    public void TranscriptTrackIdConversionsAndEquality()
    {
        var id1 = new TranscriptTrackId("track_1");
        var id2 = new TranscriptTrackId("track_1");
        var id3 = new TranscriptTrackId("track_2");

        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
        Assert.NotEqual(id1, id3);
        Assert.Equal(0, id1.CompareTo(id2));

        string str = id1;
        Assert.Equal("track_1", str);
        var explicitId = (TranscriptTrackId)"track_1";
        Assert.Equal("track_1", explicitId.Value);

        var defaultVal = default(TranscriptTrackId);
        Assert.Equal(string.Empty, defaultVal.Value);
    }
}