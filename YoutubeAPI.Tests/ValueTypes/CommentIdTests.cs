using Xunit;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Tests.ValueTypes;

public sealed class CommentIdTests
{
    private const string ValidTopCommentId = "Ugx12345abcde67890fghij";
    private const string ValidReplyCommentId = "Ugx12345abcde67890fghij.987654321";

    [Theory]
    [InlineData(ValidTopCommentId)]
    [InlineData(ValidReplyCommentId)]
    [InlineData("Ugzkz7b8v0-c-Z5q8-d4AaABAg")]
    [InlineData("Ugzkz7b8v0-c-Z5q8-d4AaABAg.9abcde_FGHI")]
    public void ConstructorWithValidRawIdSetsValue(string rawId)
    {
        var id = new CommentId(rawId);
        Assert.Equal(rawId, id.Value);
        Assert.Equal(rawId, id.ToString());
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&lc=Ugx12345abcde67890fghij", ValidTopCommentId)]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ&lc=Ugx12345abcde67890fghij.987654321", ValidReplyCommentId)]
    public void ParseWithValidUrlExtractsCommentId(string input, string expectedId)
    {
        var id = CommentId.Parse(input);
        Assert.Equal(expectedId, id.Value);
    }

    [Theory]
    [InlineData(ValidTopCommentId, ValidTopCommentId)]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&lc=Ugx12345abcde67890fghij", ValidTopCommentId)]
    public void TryParseWithValidInputReturnsTrueAndSetsResult(string input, string expectedId)
    {
        var success = CommentId.TryParse(input, out var id);
        Assert.True(success);
        Assert.Equal(expectedId, id.Value);
    }

    [Theory]
    [InlineData(ValidTopCommentId, ValidTopCommentId)]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&lc=Ugx12345abcde67890fghij", ValidTopCommentId)]
    public void SpanParseAndTryParseWithValidInputSucceeds(string input, string expectedId)
    {
        var idFromParse = CommentId.Parse(input.AsSpan(), null);
        Assert.Equal(expectedId, idFromParse.Value);

        var success = CommentId.TryParse(input.AsSpan(), null, out var idFromTryParse);
        Assert.True(success);
        Assert.Equal(expectedId, idFromTryParse.Value);
    }

    [Fact]
    public void ParseWithNullThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CommentId.Parse(null!));
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("has a space")]
    public void TryParseWithInvalidInputReturnsFalse(string? input)
    {
        var success = CommentId.TryParse(input, out var id);
        Assert.False(success);
        Assert.Equal(string.Empty, id.Value);
    }

    [Fact]
    public void ConversionsImplicitAndExplicitWorkAsExpected()
    {
        var id = new CommentId(ValidTopCommentId);
        string stringVal = id;
        Assert.Equal(ValidTopCommentId, stringVal);

        var fromExplicit = (CommentId)ValidTopCommentId;
        Assert.Equal(ValidTopCommentId, fromExplicit.Value);
    }

    [Fact]
    public void DefaultValueHasEmptyStringValue()
    {
        var defaultId = default(CommentId);
        Assert.Equal(string.Empty, defaultId.Value);
        Assert.Equal(string.Empty, defaultId.ToString());
    }

    [Fact]
    public void EqualityAndComparisonBehaveCorrectly()
    {
        var id1 = new CommentId(ValidTopCommentId);
        var id2 = new CommentId(ValidTopCommentId);
        var id3 = new CommentId(ValidReplyCommentId);

        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
        Assert.NotEqual(id1, id3);
        Assert.Equal(0, id1.CompareTo(id2));
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }
}