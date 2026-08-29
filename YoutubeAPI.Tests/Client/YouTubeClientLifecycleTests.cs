using Xunit;

namespace YoutubeAPI.Tests.Client;

public sealed class YouTubeClientLifecycleTests
{
    [Fact]
    public void ConstructorInitializesGroupedClients()
    {
        using var client = new YouTubeClient();

        Assert.NotNull(client.Videos);
        Assert.NotNull(client.Search);
        Assert.NotNull(client.Suggestions);
        Assert.NotNull(client.Channels);
        Assert.NotNull(client.Playlists);
        Assert.NotNull(client.Comments);
        Assert.NotNull(client.Feeds);
        Assert.NotNull(client.Account);
        Assert.NotNull(client.Ratings);
    }

    [Fact]
    public void DisposeIsIdempotentAndDoesNotThrow()
    {
        var client = new YouTubeClient();

        // Calling Dispose once
        client.Dispose();

        // Calling Dispose multiple times must not throw
        client.Dispose();
        client.Dispose();
    }
}