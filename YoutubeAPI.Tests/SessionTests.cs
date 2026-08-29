using System.Net;
using System.Text.Json;
using Xunit;
using YoutubeAPI.Exceptions;
using YoutubeAPI.Infrastructure;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Tests;

public class SessionTests
{
    [Fact]
    public void UnauthenticatedSessionThrowsOnEnsureAuthenticated()
    {
        using var session = new InnerTubeSession(new YouTubeClientOptions());
        Assert.Throws<AuthenticationRequiredException>(session.EnsureAuthenticated);
    }


    [Fact]
    public async Task UnauthenticatedClientAccountOperationsThrow()
    {
        using var client = new YouTubeClient();
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => client.Account.GetProfileAsync());
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() =>
            client.Account.SubscribeAsync(new ChannelId("UC1234567890123456789012")));
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() =>
            client.Account.UnsubscribeAsync(new ChannelId("UC1234567890123456789012")));
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => client.Feeds.GetSubscriptionsPageAsync());
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => client.Feeds.GetSubscribedChannelsPageAsync());
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => client.Feeds.GetHistoryPageAsync());
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => client.Playlists.GetMinePageAsync());
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() =>
            client.Ratings.GetAsync(new VideoId("dQw4w9WgXcQ")));
    }
    [Fact]
    public async Task SearchRequestWritesJsonPayload()
    {
        var handler = new CapturingHandler();
        using var httpClient = new HttpClient(handler);
        using var session = new InnerTubeSession(new YouTubeClientOptions(), httpClient);
        var search = new SearchHandler(session);

        await search.GetPageAsync(new Models.Search.SearchRequest("test"), CancellationToken.None);

        Assert.NotNull(handler.SearchBody);
        using var payload = JsonDocument.Parse(handler.SearchBody);
        Assert.Equal("test", payload.RootElement.GetProperty("query").GetString());
        Assert.Equal("WEB", payload.RootElement.GetProperty("context").GetProperty("client").GetProperty("clientName").GetString());
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? SearchBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post)
            {
                SearchBody = await request.Content!.ReadAsStringAsync(cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html></html>", System.Text.Encoding.UTF8, "text/html")
            };
        }
    }

}