using Xunit;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Tests.ValueTypes;

public sealed class VideoIdTests
{
    [Theory]
    [InlineData("dQw4w9WgXcQ")]
    [InlineData("9bZkp7q19f0")]
    [InlineData("kJQP7kiw5Fk")]
    [InlineData("_a1-B2_c3-D")]
    [InlineData("-----------")]
    [InlineData("___________")]
    [InlineData("12345678901")]
    [InlineData("abcdefghijk")]
    [InlineData("ABCDEFGHIJK")]
    public void ConstructorWithValidRawIdSetsValue(string rawId)
    {
        var id = new VideoId(rawId);
        Assert.Equal(rawId, id.Value);
        Assert.Equal(rawId, id.ToString());
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("http://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://music.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://gaming.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://tv.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("http://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/v/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/live/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/clip/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/clips/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?feature=shared&v=dQw4w9WgXcQ&t=42s", "dQw4w9WgXcQ")]
    [InlineData("  https://www.youtube.com/watch?v=dQw4w9WgXcQ  ", "dQw4w9WgXcQ")]
    [InlineData("www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void ParseWithValidUrlExtractsVideoId(string input, string expectedId)
    {
        var id = VideoId.Parse(input);
        Assert.Equal(expectedId, id.Value);
    }

    [Theory]
    [InlineData("dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void TryParseWithValidInputReturnsTrueAndSetsResult(string input, string expectedId)
    {
        var success = VideoId.TryParse(input, out var id);
        Assert.True(success);
        Assert.Equal(expectedId, id.Value);
    }

    [Theory]
    [InlineData("dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void SpanParseAndTryParseWithValidInputSucceeds(string input, string expectedId)
    {
        var idFromParse = VideoId.Parse(input.AsSpan(), null);
        Assert.Equal(expectedId, idFromParse.Value);

        var success = VideoId.TryParse(input.AsSpan(), null, out var idFromTryParse);
        Assert.True(success);
        Assert.Equal(expectedId, idFromTryParse.Value);
    }

    [Fact]
    public void ParseWithNullThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => VideoId.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("dQw4w9WgXc")] // 10 chars
    [InlineData("dQw4w9WgXcQQ")] // 12 chars
    [InlineData("dQw4w9WgXc!")] // invalid char
    [InlineData("dQw4w9WgXc@")] // invalid char
    [InlineData("dQw4w9WgXc#")] // invalid char
    [InlineData("dQw4w9Wg XcQ")] // space in raw ID
    [InlineData("https://www.youtube.com/watch")] // missing v
    [InlineData("https://www.youtube.com/watch?v=")] // empty v
    [InlineData("https://www.youtube.com/watch?v=invalid_lenx")] // 12 chars
    [InlineData("https://www.youtube.com/unknown/dQw4w9WgXcQ")] // unknown path prefix
    public void ParseWithInvalidInputThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => VideoId.Parse(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("dQw4w9WgXc")]
    [InlineData("dQw4w9WgXcQQ")]
    [InlineData("dQw4w9WgXc!")]
    [InlineData("https://www.youtube.com/watch")]
    public void TryParseWithInvalidInputReturnsFalse(string? input)
    {
        var success = VideoId.TryParse(input, out var id);
        Assert.False(success);
        Assert.Equal(string.Empty, id.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("way_too_long_video_id_here")]
    [InlineData("invalid!id*")]
    public void ConstructorWithInvalidRawIdThrowsArgumentException(string rawId)
    {
        Assert.Throws<ArgumentException>(() => new VideoId(rawId));
    }

    [Fact]
    public void ConversionsImplicitAndExplicitWorkAsExpected()
    {
        var id = new VideoId("dQw4w9WgXcQ");
        string stringVal = id;
        Assert.Equal("dQw4w9WgXcQ", stringVal);

        var fromExplicit = (VideoId)"dQw4w9WgXcQ";
        Assert.Equal("dQw4w9WgXcQ", fromExplicit.Value);
    }

    [Fact]
    public void DefaultValueHasEmptyStringValue()
    {
        var defaultId = default(VideoId);
        Assert.Equal(string.Empty, defaultId.Value);
        Assert.Equal(string.Empty, defaultId.ToString());
    }

    [Fact]
    public void EqualityAndComparisonBehaveCorrectly()
    {
        var id1 = new VideoId("aaaaaaaaaaa");
        var id2 = new VideoId("aaaaaaaaaaa");
        var id3 = new VideoId("bbbbbbbbbbb");

        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
        Assert.NotEqual(id1, id3);
        Assert.True(id1.CompareTo(id3) < 0);
        Assert.True(id3.CompareTo(id1) > 0);
        Assert.Equal(0, id1.CompareTo(id2));
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }
}