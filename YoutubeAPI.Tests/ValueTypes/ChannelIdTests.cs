using Xunit;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Tests.ValueTypes;

public sealed class ChannelIdTests
{
    private const string ValidChannelId = "UCuAXFkgsw1L7xaCfnd5JJOw";
    private const string ValidChannelId2 = "UC_x5XG1OV2P6uZZ5FSM9Ttw";

    [Theory]
    [InlineData(ValidChannelId)]
    [InlineData(ValidChannelId2)]
    [InlineData("UC1234567890123456789012")]
    [InlineData("UC----------------------")]
    [InlineData("UC______________________")]
    public void ConstructorWithValidRawIdSetsValue(string rawId)
    {
        var id = new ChannelId(rawId);
        Assert.Equal(rawId, id.Value);
        Assert.Equal(rawId, id.ToString());
    }

    [Theory]
    [InlineData("https://www.youtube.com/channel/UCuAXFkgsw1L7xaCfnd5JJOw", ValidChannelId)]
    [InlineData("http://www.youtube.com/channel/UCuAXFkgsw1L7xaCfnd5JJOw", ValidChannelId)]
    [InlineData("https://youtube.com/channel/UCuAXFkgsw1L7xaCfnd5JJOw", ValidChannelId)]
    [InlineData("https://m.youtube.com/channel/UCuAXFkgsw1L7xaCfnd5JJOw", ValidChannelId)]
    [InlineData("https://www.youtube.com/channel/UCuAXFkgsw1L7xaCfnd5JJOw/videos", ValidChannelId)]
    [InlineData("www.youtube.com/channel/UCuAXFkgsw1L7xaCfnd5JJOw", ValidChannelId)]
    public void ParseWithValidUrlExtractsChannelId(string input, string expectedId)
    {
        var id = ChannelId.Parse(input);
        Assert.Equal(expectedId, id.Value);
    }

    [Theory]
    [InlineData(ValidChannelId, ValidChannelId)]
    [InlineData("https://www.youtube.com/channel/UCuAXFkgsw1L7xaCfnd5JJOw", ValidChannelId)]
    public void TryParseWithValidInputReturnsTrueAndSetsResult(string input, string expectedId)
    {
        var success = ChannelId.TryParse(input, out var id);
        Assert.True(success);
        Assert.Equal(expectedId, id.Value);
    }

    [Theory]
    [InlineData(ValidChannelId, ValidChannelId)]
    [InlineData("https://www.youtube.com/channel/UCuAXFkgsw1L7xaCfnd5JJOw", ValidChannelId)]
    public void SpanParseAndTryParseWithValidInputSucceeds(string input, string expectedId)
    {
        var idFromParse = ChannelId.Parse(input.AsSpan(), null);
        Assert.Equal(expectedId, idFromParse.Value);

        var success = ChannelId.TryParse(input.AsSpan(), null, out var idFromTryParse);
        Assert.True(success);
        Assert.Equal(expectedId, idFromTryParse.Value);
    }

    [Fact]
    public void ParseWithNullThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ChannelId.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("UCuAXFkgsw1L7xaCfnd5JJO")] // 23 chars
    [InlineData("UCuAXFkgsw1L7xaCfnd5JJOww")] // 25 chars
    [InlineData("ABuAXFkgsw1L7xaCfnd5JJOw")] // doesn't start with UC
    [InlineData("PLuAXFkgsw1L7xaCfnd5JJOw")] // starts with PL
    [InlineData("UCuAXFkgsw1L7xaCfnd5J!@#")] // invalid chars
    [InlineData("https://www.youtube.com/channel/")] // missing id
    [InlineData("https://www.youtube.com/c/CustomName")] // c/ is reference, not raw ChannelId
    [InlineData("https://www.youtube.com/@handle")] // @handle is reference, not raw ChannelId
    public void ParseWithInvalidInputThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => ChannelId.Parse(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not_a_channel_id")]
    [InlineData("UCshort")]
    public void TryParseWithInvalidInputReturnsFalse(string? input)
    {
        var success = ChannelId.TryParse(input, out var id);
        Assert.False(success);
        Assert.Equal(string.Empty, id.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not_valid")]
    [InlineData("AB1234567890123456789012")]
    public void ConstructorWithInvalidRawIdThrowsArgumentException(string rawId)
    {
        Assert.Throws<ArgumentException>(() => new ChannelId(rawId));
    }

    [Fact]
    public void ConversionsImplicitAndExplicitWorkAsExpected()
    {
        var id = new ChannelId(ValidChannelId);
        string stringVal = id;
        Assert.Equal(ValidChannelId, stringVal);

        var fromExplicit = (ChannelId)ValidChannelId;
        Assert.Equal(ValidChannelId, fromExplicit.Value);
    }

    [Fact]
    public void DefaultValueHasEmptyStringValue()
    {
        var defaultId = default(ChannelId);
        Assert.Equal(string.Empty, defaultId.Value);
        Assert.Equal(string.Empty, defaultId.ToString());
    }

    [Fact]
    public void EqualityAndComparisonBehaveCorrectly()
    {
        var id1 = new ChannelId(ValidChannelId);
        var id2 = new ChannelId(ValidChannelId);
        var id3 = new ChannelId(ValidChannelId2);

        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
        Assert.NotEqual(id1, id3);
        Assert.Equal(0, id1.CompareTo(id2));
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }
}