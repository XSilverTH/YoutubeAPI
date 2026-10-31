using Xunit;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Tests.ValueTypes;

public sealed class ChannelIdTests
{
    private const string ValidChannelId = "UCuAXFkgsw1L7xaCfnd5JJOw";
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

}