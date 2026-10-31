using Xunit;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Tests.ValueTypes;

public sealed class ChannelReferenceTests
{
    private const string ValidChannelId = "UCuAXFkgsw1L7xaCfnd5JJOw";


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
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("@")]
    [InlineData("https://www.youtube.com/channel/")] // missing id
    public void ParseWithInvalidInputThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => ChannelReference.Parse(input));
    }

}