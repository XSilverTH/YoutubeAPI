using Xunit;
using YoutubeAPI.Models.Continuations;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.LiveTests;

[Trait("Category", "AuthenticatedRead")]
public class AuthenticatedLiveTests
{
    [Fact]
    public async Task GetProfileAsyncLiveReturnsAuthenticatedProfile()
    {
        if (!LiveTestEnvironment.IsAuthenticatedEnabled())
            return;

        using var client = LiveTestEnvironment.CreateAuthenticatedClient();
        Assert.NotNull(client);

        var profile = await client.Account.GetProfileAsync();
        Assert.NotNull(profile);
        Assert.False(string.IsNullOrWhiteSpace(profile.DisplayName));
    }

    [Fact]
    public async Task GetPlaybackProgressAsyncLiveReturnsNullablePlaybackState()
    {
        if (!LiveTestEnvironment.IsAuthenticatedEnabled())
            return;

        using var client = LiveTestEnvironment.CreateAuthenticatedClient();
        Assert.NotNull(client);

        var progress = await client.Videos.GetPlaybackProgressAsync(VideoId.Parse(LiveTestEnvironment.KnownPublicVideoId));
        if (progress == null)
            return;

        Assert.InRange(progress.WatchedFraction ?? 0, 0, 1);
        Assert.True(progress.ResumePosition == null || progress.ResumePosition >= TimeSpan.Zero);
    }

    [Fact]
    public async Task GetHomePageAsyncLiveReturnsPersonalizedFeed()
    {
        if (!LiveTestEnvironment.IsAuthenticatedEnabled())
            return;

        using var client = LiveTestEnvironment.CreateAuthenticatedClient();
        Assert.NotNull(client);

        var page = await client.Feeds.GetHomePageAsync();
        Assert.NotNull(page);
        Assert.NotNull(page.Items);
    }

    [Fact]
    public async Task GetSubscriptionsPageAsyncLiveReturnsSubscriptions()
    {
        if (!LiveTestEnvironment.IsAuthenticatedEnabled())
            return;

        using var client = LiveTestEnvironment.CreateAuthenticatedClient();
        Assert.NotNull(client);

        var page = await client.Feeds.GetSubscriptionsPageAsync();
        Assert.NotNull(page);
        Assert.NotNull(page.Items);
    }

    [Fact]
    public async Task GetSubscribedChannelsPageAsyncLiveReturnsSubscribedChannels()
    {
        if (!LiveTestEnvironment.IsAuthenticatedEnabled())
            return;

        using var client = LiveTestEnvironment.CreateAuthenticatedClient();
        Assert.NotNull(client);

        var page = await client.Feeds.GetSubscribedChannelsPageAsync();
        Assert.NotNull(page);
        Assert.NotNull(page.Items);
    }

    [Fact]
    public async Task GetHistoryPageAsyncLiveAndContinuationResumesWithNewClient()
    {
        if (!LiveTestEnvironment.IsAuthenticatedEnabled())
            return;

        using var client1 = LiveTestEnvironment.CreateAuthenticatedClient();
        Assert.NotNull(client1);

        var page = await client1.Feeds.GetHistoryPageAsync();
        Assert.NotNull(page);
        Assert.NotNull(page.Items);

        if (page.Next != null)
        {
            var exported = page.Next.Export();
            Assert.False(string.IsNullOrWhiteSpace(exported));

            var imported = HistoryContinuation.Import(exported);
            Assert.NotNull(imported);

            using var client2 = LiveTestEnvironment.CreateAuthenticatedClient();
            Assert.NotNull(client2);

            var page2 = await client2.Feeds.GetHistoryPageAsync(imported);
            Assert.NotNull(page2);
            Assert.NotNull(page2.Items);
        }
    }

    [Fact]
    public async Task GetMinePlaylistsPageAsyncLiveReturnsOwnedPlaylists()
    {
        if (!LiveTestEnvironment.IsAuthenticatedEnabled())
            return;

        using var client = LiveTestEnvironment.CreateAuthenticatedClient();
        Assert.NotNull(client);

        var page = await client.Playlists.GetMinePageAsync();
        Assert.NotNull(page);
        Assert.NotNull(page.Items);
    }

}