using Xunit;
using YoutubeAPI.Infrastructure;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Tests.ValueTypes;

public sealed class HostileDomainUrlTests
{
    [Theory]
    [InlineData("https://evil.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com.evil.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://evil-youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://notyoutube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://attacker@youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com:password@evil.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be.attacker.com/dQw4w9WgXcQ")]
    [InlineData("https://attacker-youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://sub.evil.youtube.com.attacker.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("ftp://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<html>")]
    [InlineData("file:///C:/video.mp4")]
    public void VideoIdTryParseWithHostileOrInvalidDomainReturnsFalse(string hostileUrl)
    {
        var success = VideoId.TryParse(hostileUrl, out var id);
        Assert.False(success);
        Assert.Equal(string.Empty, id.Value);
    }

    [Theory]
    [InlineData("https://evil.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com.evil.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://attacker@youtube.com/watch?v=dQw4w9WgXcQ")]
    public void VideoIdParseWithHostileDomainThrowsFormatException(string hostileUrl)
    {
        Assert.Throws<FormatException>(() => VideoId.Parse(hostileUrl));
    }

    [Theory]
    [InlineData("https://evil.com/channel/UCuAXFkgsw1L7xaCfnd5JJOw")]
    [InlineData("https://youtube.com.attacker.com/channel/UCuAXFkgsw1L7xaCfnd5JJOw")]
    [InlineData("https://notyoutube.com/channel/UCuAXFkgsw1L7xaCfnd5JJOw")]
    public void ChannelIdTryParseWithHostileDomainReturnsFalse(string hostileUrl)
    {
        var success = ChannelId.TryParse(hostileUrl, out var id);
        Assert.False(success);
        Assert.Equal(string.Empty, id.Value);
    }

    [Theory]
    [InlineData("https://evil.com/@mkbhd")]
    [InlineData("https://youtube.com.attacker.com/@mkbhd")]
    [InlineData("https://attacker-youtube.com/c/CustomName")]
    public void ChannelReferenceTryParseWithHostileDomainReturnsFalse(string hostileUrl)
    {
        var success = ChannelReference.TryParse(hostileUrl, out var reference);
        Assert.False(success);
        Assert.Equal(string.Empty, reference.Value);
    }

    [Theory]
    [InlineData("https://evil.com/playlist?list=PLrAXtmErZgOdP_8GztsuKi9nrraNbKKp4")]
    [InlineData("https://youtube.com.attacker.com/playlist?list=PLrAXtmErZgOdP_8GztsuKi9nrraNbKKp4")]
    public void PlaylistIdTryParseWithHostileDomainReturnsFalse(string hostileUrl)
    {
        var success = PlaylistId.TryParse(hostileUrl, out var id);
        Assert.False(success);
        Assert.Equal(string.Empty, id.Value);
    }

    [Theory]
    [InlineData("https://evil.com/watch?v=dQw4w9WgXcQ&lc=Ugx12345abcde67890fghij")]
    [InlineData("https://youtube.com.attacker.com/watch?v=dQw4w9WgXcQ&lc=Ugx12345abcde67890fghij")]
    public void CommentIdTryParseWithHostileDomainReturnsFalse(string hostileUrl)
    {
        var success = CommentId.TryParse(hostileUrl, out var id);
        Assert.False(success);
        Assert.Equal(string.Empty, id.Value);
    }

    [Theory]
    [InlineData("youtube.com", true)]
    [InlineData("www.youtube.com", true)]
    [InlineData("m.youtube.com", true)]
    [InlineData("music.youtube.com", true)]
    [InlineData("gaming.youtube.com", true)]
    [InlineData("tv.youtube.com", true)]
    [InlineData("youtu.be", true)]
    [InlineData("www.youtu.be", true)]
    [InlineData("sub.youtube.com", true)]
    [InlineData("evil.com", false)]
    [InlineData("youtube.com.evil.com", false)]
    [InlineData("notyoutube.com", false)]
    [InlineData("evil-youtube.com", false)]
    [InlineData("attacker@youtube.com", false)]
    [InlineData("attacker/youtube.com", false)]
    [InlineData("attacker\\youtube.com", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void YouTubeUrlParserIsValidYouTubeHostValidatesHostsStrictly(string host, bool expectedValid)
    {
        var isValid = YouTubeUrlParser.IsValidYouTubeHost(host);
        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public void YouTubeUrlParserParseQueryStringHandlesVariousFormats()
    {
        var empty = YouTubeUrlParser.ParseQueryString(string.Empty);
        Assert.Empty(empty);

        const string query = "?v=dQw4w9WgXcQ&list=PL123&empty=&novalue";
        var parsed = YouTubeUrlParser.ParseQueryString(query);

        Assert.Equal("dQw4w9WgXcQ", parsed["v"]);
        Assert.Equal("PL123", parsed["list"]);
        Assert.Equal(string.Empty, parsed["empty"]);
        Assert.Equal(string.Empty, parsed["novalue"]);

        // Escaped characters
        const string escapedQuery = "?title=Hello%20World&tag=C%23";
        var escapedParsed = YouTubeUrlParser.ParseQueryString(escapedQuery);
        Assert.Equal("Hello World", escapedParsed["title"]);
        Assert.Equal("C#", escapedParsed["tag"]);
    }
}