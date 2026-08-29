using Xunit;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Tests.ValueTypes;

public sealed class ChannelReferenceTests
{
    private const string ValidChannelId = "UCuAXFkgsw1L7xaCfnd5JJOw";

    [Fact]
    public void FromIdWithChannelIdCreatesChannelReference()
    {
        var channelId = new ChannelId(ValidChannelId);
        var reference = ChannelReference.FromId(channelId);
        Assert.Equal(ValidChannelId, reference.Value);
    }

    [Theory]
    [InlineData("mkbhd", "@mkbhd")]
    [InlineData("@mkbhd", "@mkbhd")]
    [InlineData("LinusTechTips", "@LinusTechTips")]
    [InlineData("@LinusTechTips", "@LinusTechTips")]
    public void FromHandleWithValidHandleNormalizesAndCreatesReference(string input, string expected)
    {
        var reference = ChannelReference.FromHandle(input);
        Assert.Equal(expected, reference.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromHandleWithNullOrWhitespaceThrowsArgumentException(string? handle)
    {
        Assert.ThrowsAny<ArgumentException>(() => ChannelReference.FromHandle(handle!));
    }

    [Theory]
    [InlineData(ValidChannelId, ValidChannelId)]
    [InlineData("@mkbhd", "@mkbhd")]
    [InlineData("https://www.youtube.com/channel/UCuAXFkgsw1L7xaCfnd5JJOw", ValidChannelId)]
    [InlineData("https://www.youtube.com/@mkbhd", "@mkbhd")]
    [InlineData("https://www.youtube.com/@mkbhd/videos", "@mkbhd")]
    [InlineData("https://www.youtube.com/c/LinusTechTips", "LinusTechTips")]
    [InlineData("https://www.youtube.com/user/mkbhd", "mkbhd")]
    [InlineData("https://www.youtube.com/u/mkbhd", "mkbhd")]
    [InlineData("www.youtube.com/@mkbhd", "@mkbhd")]
    public void ParseWithValidInputReturnsExpectedReference(string input, string expected)
    {
        var reference = ChannelReference.Parse(input);
        Assert.Equal(expected, reference.Value);
    }

    [Theory]
    [InlineData("@mkbhd", "@mkbhd")]
    [InlineData("https://www.youtube.com/@mkbhd", "@mkbhd")]
    public void TryParseWithValidInputReturnsTrueAndSetsResult(string input, string expected)
    {
        var success = ChannelReference.TryParse(input, out var reference);
        Assert.True(success);
        Assert.Equal(expected, reference.Value);
    }

    [Theory]
    [InlineData("@mkbhd", "@mkbhd")]
    [InlineData("https://www.youtube.com/@mkbhd", "@mkbhd")]
    public void SpanParseAndTryParseWithValidInputSucceeds(string input, string expected)
    {
        var refFromParse = ChannelReference.Parse(input.AsSpan(), null);
        Assert.Equal(expected, refFromParse.Value);

        var success = ChannelReference.TryParse(input.AsSpan(), null, out var refFromTryParse);
        Assert.True(success);
        Assert.Equal(expected, refFromTryParse.Value);
    }

    [Fact]
    public void ParseWithNullThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ChannelReference.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("@")]
    [InlineData("https://www.youtube.com/channel/")] // missing id
    public void ParseWithInvalidInputThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => ChannelReference.Parse(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParseWithInvalidInputReturnsFalse(string? input)
    {
        var success = ChannelReference.TryParse(input, out var reference);
        Assert.False(success);
        Assert.Equal(string.Empty, reference.Value);
    }

    [Fact]
    public void DefaultValueHasEmptyStringValue()
    {
        var defaultRef = default(ChannelReference);
        Assert.Equal(string.Empty, defaultRef.Value);
        Assert.Equal(string.Empty, defaultRef.ToString());
    }

    [Fact]
    public void EqualityBehaveCorrectly()
    {
        var ref1 = new ChannelReference("@mkbhd");
        var ref2 = new ChannelReference("@mkbhd");
        var ref3 = new ChannelReference("@other");

        Assert.Equal(ref1, ref2);
        Assert.True(ref1 == ref2);
        Assert.False(ref1 != ref2);
        Assert.NotEqual(ref1, ref3);
        Assert.Equal(ref1.GetHashCode(), ref2.GetHashCode());
    }
}