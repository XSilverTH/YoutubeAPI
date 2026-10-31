using Xunit;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Tests.ValueTypes;

public sealed class PlaylistIdTests
{
    private const string ValidPlaylist = "PLrAXtmErZgOdP_8GztsuKi9nrraNbKKp4";
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

}