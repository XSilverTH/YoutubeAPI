using Xunit;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Tests.ValueTypes;

public sealed class CommentIdTests
{
    private const string ValidTopCommentId = "Ugx12345abcde67890fghij";
    private const string ValidReplyCommentId = "Ugx12345abcde67890fghij.987654321";
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&lc=Ugx12345abcde67890fghij", ValidTopCommentId)]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ&lc=Ugx12345abcde67890fghij.987654321", ValidReplyCommentId)]
    public void ParseWithValidUrlExtractsCommentId(string input, string expectedId)
    {
        var id = CommentId.Parse(input);
        Assert.Equal(expectedId, id.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("has a space in id")]
    [InlineData("has/a/slash")]
    [InlineData("has?a?question")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")] // missing lc parameter
    public void ParseWithInvalidInputThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => CommentId.Parse(input));
    }

}