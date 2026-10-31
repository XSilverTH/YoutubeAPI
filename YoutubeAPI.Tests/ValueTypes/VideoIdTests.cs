using Xunit;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Tests.ValueTypes;

public sealed class VideoIdTests
{
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

}