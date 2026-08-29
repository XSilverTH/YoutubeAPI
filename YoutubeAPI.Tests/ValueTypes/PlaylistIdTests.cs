using Xunit;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Tests.ValueTypes;

public sealed class PlaylistIdTests
{
    private const string ValidPlaylist = "PLrAXtmErZgOdP_8GztsuKi9nrraNbKKp4";
    private const string ValidPlaylist2 = "RDCLAK5uy_kfdijrP83aKKrmr37bvDzpYnMRclmeeak";

    [Theory]
    [InlineData(ValidPlaylist)]
    [InlineData(ValidPlaylist2)]
    [InlineData("LL")]
    [InlineData("WL")]
    [InlineData("FL")]
    [InlineData("UUuAXFkgsw1L7xaCfnd5JJOw")]
    public void ConstructorWithValidRawIdSetsValue(string rawId)
    {
        var id = new PlaylistId(rawId);
        Assert.Equal(rawId, id.Value);
        Assert.Equal(rawId, id.ToString());
    }

    [Theory]
    [InlineData("https://www.youtube.com/playlist?list=PLrAXtmErZgOdP_8GztsuKi9nrraNbKKp4", ValidPlaylist)]
    [InlineData("http://www.youtube.com/playlist?list=PLrAXtmErZgOdP_8GztsuKi9nrraNbKKp4", ValidPlaylist)]
    [InlineData("https://youtube.com/playlist?list=PLrAXtmErZgOdP_8GztsuKi9nrraNbKKp4", ValidPlaylist)]
    [InlineData("https://music.youtube.com/playlist?list=PLrAXtmErZgOdP_8GztsuKi9nrraNbKKp4", ValidPlaylist)]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PLrAXtmErZgOdP_8GztsuKi9nrraNbKKp4", ValidPlaylist)]
    [InlineData("www.youtube.com/playlist?list=PLrAXtmErZgOdP_8GztsuKi9nrraNbKKp4", ValidPlaylist)]
    public void ParseWithValidUrlExtractsPlaylistId(string input, string expectedId)
    {
        var id = PlaylistId.Parse(input);
        Assert.Equal(expectedId, id.Value);
    }

    [Theory]
    [InlineData(ValidPlaylist, ValidPlaylist)]
    [InlineData("https://www.youtube.com/playlist?list=PLrAXtmErZgOdP_8GztsuKi9nrraNbKKp4", ValidPlaylist)]
    public void TryParseWithValidInputReturnsTrueAndSetsResult(string input, string expectedId)
    {
        var success = PlaylistId.TryParse(input, out var id);
        Assert.True(success);
        Assert.Equal(expectedId, id.Value);
    }

    [Theory]
    [InlineData(ValidPlaylist, ValidPlaylist)]
    [InlineData("https://www.youtube.com/playlist?list=PLrAXtmErZgOdP_8GztsuKi9nrraNbKKp4", ValidPlaylist)]
    public void SpanParseAndTryParseWithValidInputSucceeds(string input, string expectedId)
    {
        var idFromParse = PlaylistId.Parse(input.AsSpan(), null);
        Assert.Equal(expectedId, idFromParse.Value);

        var success = PlaylistId.TryParse(input.AsSpan(), null, out var idFromTryParse);
        Assert.True(success);
        Assert.Equal(expectedId, idFromTryParse.Value);
    }

    [Fact]
    public void ParseWithNullThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => PlaylistId.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")] // single char (len < 2)
    [InlineData("PL!@#$")] // invalid chars
    [InlineData("https://www.youtube.com/playlist")] // missing list parameter
    [InlineData("https://www.youtube.com/playlist?list=")] // empty list parameter
    public void ParseWithInvalidInputThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => PlaylistId.Parse(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("P")]
    public void TryParseWithInvalidInputReturnsFalse(string? input)
    {
        var success = PlaylistId.TryParse(input, out var id);
        Assert.False(success);
        Assert.Equal(string.Empty, id.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("X")]
    public void ConstructorWithInvalidRawIdThrowsArgumentException(string rawId)
    {
        Assert.Throws<ArgumentException>(() => new PlaylistId(rawId));
    }

    [Fact]
    public void ConversionsImplicitAndExplicitWorkAsExpected()
    {
        var id = new PlaylistId(ValidPlaylist);
        string stringVal = id;
        Assert.Equal(ValidPlaylist, stringVal);

        var fromExplicit = (PlaylistId)ValidPlaylist;
        Assert.Equal(ValidPlaylist, fromExplicit.Value);
    }

    [Fact]
    public void DefaultValueHasEmptyStringValue()
    {
        var defaultId = default(PlaylistId);
        Assert.Equal(string.Empty, defaultId.Value);
        Assert.Equal(string.Empty, defaultId.ToString());
    }

    [Fact]
    public void EqualityAndComparisonBehaveCorrectly()
    {
        var id1 = new PlaylistId(ValidPlaylist);
        var id2 = new PlaylistId(ValidPlaylist);
        var id3 = new PlaylistId(ValidPlaylist2);

        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
        Assert.NotEqual(id1, id3);
        Assert.Equal(0, id1.CompareTo(id2));
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }
}