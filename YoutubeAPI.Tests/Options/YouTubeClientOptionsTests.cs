using System.Net;
using Xunit;

namespace YoutubeAPI.Tests.Options;

public sealed class YouTubeClientOptionsTests
{
    [Fact]
    public void DefaultOptionsHaveExpectedDefaultValues()
    {
        var options = new YouTubeClientOptions();

        Assert.Equal("en", options.Language);
        Assert.Equal("US", options.Region);
        Assert.Null(options.Authentication);
        Assert.Null(options.VisitorData);
        Assert.Null(options.RolloutToken);
        Assert.Null(options.ProofOfOriginToken);
        Assert.Equal(0, options.AuthUser);
        Assert.Null(options.PageId);
        Assert.Same(TimeProvider.System, options.TimeProvider);
    }

}