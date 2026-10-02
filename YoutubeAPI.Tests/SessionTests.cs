using System.Net;
using System.Text;
using System.Text.Json;
using YoutubeAPI.Models.Continuations;
using Xunit;
using YoutubeAPI.Exceptions;
using YoutubeAPI.Infrastructure;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Tests;

public class SessionTests
{
    [Theory]
    [InlineData("Authorization: Bearer secret-token", "secret-token")]
    [InlineData("Cookie: SAPISID=secret-cookie", "secret-cookie")]
    [InlineData("key=secret-key&continuation=secret-continuation", "secret-key")]
    [InlineData("SAPISIDHASH 123_secret-hash", "123_secret-hash")]
    public void SanitizeRemovesSecretMaterial(string input, string secret)
    {
        var sanitized = InnerTubeSession.Sanitize(input);

        Assert.DoesNotContain(secret, sanitized, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", sanitized, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProfileBootstrapFailureCanBeRetried()
    {
        var authentication = YouTubeCookieAuthentication.FromNetscape(
            ".youtube.com	TRUE	/	TRUE	2147483647	SAPISID	test-sapisid\n");
        var handler = new RetryingAccountMenuHandler();
        using var httpClient = new HttpClient(handler);
        using var session = new InnerTubeSession(
            new YouTubeClientOptions { Authentication = authentication },
            httpClient);
        var account = new AccountHandler(session);

        await Assert.ThrowsAsync<YouTubeProtocolException>(() => account.GetProfileAsync(CancellationToken.None));
        var profile = await account.GetProfileAsync(CancellationToken.None);

        Assert.Equal("Test User", profile.DisplayName);
        Assert.Equal(2, handler.AccountMenuRequests);
    }

    [Fact]
    public async Task AccountMenuFailureDiagnosticsDescribeStructureWithoutPayloadValues()
    {
        const string secret = "diagnostic-secret-sentinel";
        var authentication = YouTubeCookieAuthentication.FromNetscape(
            $".youtube.com\tTRUE\t/\tTRUE\t2147483647\tSAPISID\t{secret}\n");
        using var httpClient = new HttpClient(new SignedOutAccountMenuHandler(secret));
        using var session = new InnerTubeSession(
            new YouTubeClientOptions { Authentication = authentication, AuthUser = 2, PageId = "private-page" },
            httpClient);

        var exception = await Assert.ThrowsAsync<AuthenticationExpiredException>(() =>
            new AccountHandler(session).GetProfileAsync(CancellationToken.None));
        var message = exception.Message;

        Assert.Contains("popup", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("signInEndpointPresent=true", message, StringComparison.Ordinal);
        Assert.Contains("unknownPopupRenderer", message, StringComparison.Ordinal);
        Assert.Contains("responseContext.mainAppWebResponseContext.loggedOut=true", message, StringComparison.Ordinal);
        Assert.Contains("responseStatus=200", message, StringComparison.Ordinal);
        Assert.Contains("authUser=2", message, StringComparison.Ordinal);
        Assert.Contains("pageIdPresent=true", message, StringComparison.Ordinal);
        Assert.Contains("authorizationPresent=true", message, StringComparison.Ordinal);
        Assert.Contains("SAPISID", message, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, message, StringComparison.Ordinal);
        Assert.DoesNotContain("private-page", message, StringComparison.Ordinal);
        Assert.DoesNotContain("malicious", message, StringComparison.Ordinal);
    }

    private sealed class SignedOutAccountMenuHandler(string secret) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Post)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<html></html>", Encoding.UTF8, "text/html")
                });

            const string hostileName = "malicious\nAuthorization: bearer-secret";
            var content = JsonSerializer.Serialize(new
            {
                actions = new object[]
                {
                    new Dictionary<string, object> { ["unrecognizedAction"] = new Dictionary<string, string> { [hostileName] = secret } },
                    new
                    {
                        openPopupAction = new
                        {
                            popup = new
                            {
                                unknownPopupRenderer = new { unexpectedValue = secret },
                                multiPageMenuRenderer = new
                                {
                                    sections = new[]
                                    {
                                        new
                                        {
                                            multiPageMenuSectionRenderer = new
                                            {
                                                items = new[]
                                                {
                                                    new
                                                    {
                                                        compactLinkRenderer = new
                                                        {
                                                            navigationEndpoint = new
                                                            {
                                                                signInEndpoint = new
                                                                {
                                                                    urlEndpoint = new
                                                                    {
                                                                        url = $"https://private.invalid/?token={secret}"
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                header = new
                                {
                                    unknownHeaderRenderer = new { text = new { simpleText = secret } }
                                }
                            }
                        }
                    }
                },
                responseContext = new { mainAppWebResponseContext = new { loggedOut = true } },
                privateScalar = secret
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
        }
    }

    [Fact]
    public async Task AccountProfileRejectsLoggedOutEvenWhenProfileDataIsPresent()
    {
        var authentication = YouTubeCookieAuthentication.FromNetscape(
            ".youtube.com\tTRUE\t/\tTRUE\t2147483647\tSAPISID\ttest-sapisid\n");
        using var httpClient = new HttpClient(new HomeResponseHandler(
            """{"responseContext":{"loggedOut":true},"actions":[{"openPopupAction":{"popup":{"multiPageMenuRenderer":{"header":{"accountHeaderRenderer":{"accountName":{"simpleText":"Stale profile"}}}}}}}]}"""));
        using var session = new InnerTubeSession(
            new YouTubeClientOptions { Authentication = authentication }, httpClient);

        await Assert.ThrowsAsync<AuthenticationExpiredException>(() =>
            new AccountHandler(session).GetProfileAsync(CancellationToken.None));
    }

    [Fact]
    public async Task AuthenticatedHomeRejectsExplicitLoggedOutOnInitialAndContinuationPages()
    {
        var authentication = YouTubeCookieAuthentication.FromNetscape(
            ".youtube.com\tTRUE\t/\tTRUE\t2147483647\tSAPISID\ttest-sapisid\n");
        using var initialClient = new HttpClient(new HomeResponseHandler(
            """{"responseContext":{"loggedOut":true}}"""));
        using var initialSession = new InnerTubeSession(
            new YouTubeClientOptions { Authentication = authentication }, initialClient);
        var initialFeeds = new FeedsHandler(initialSession);
        await Assert.ThrowsAsync<AuthenticationExpiredException>(() =>
            initialFeeds.GetHomePageAsync(CancellationToken.None));

        using var continuationClient = new HttpClient(new HomeResponseHandler(
            """{"responseContext":{"mainAppWebResponseContext":{"loggedOut":true}}}"""));
        using var continuationSession = new InnerTubeSession(
            new YouTubeClientOptions { Authentication = authentication }, continuationClient);
        var continuationFeeds = new FeedsHandler(continuationSession);
        await Assert.ThrowsAsync<AuthenticationExpiredException>(() =>
            continuationFeeds.GetHomePageAsync(new HomeContinuation("home-token"), CancellationToken.None));
    }

    [Theory]
    [InlineData("""{"responseContext":{"loggedOut":false}}""")]
    [InlineData("""{"responseContext":{"loggedOut":"true"}}""")]
    [InlineData("{}")]
    public async Task AuthenticatedHomeDoesNotTreatFalseOrUnknownLoggedOutAsExpired(string response)
    {
        var authentication = YouTubeCookieAuthentication.FromNetscape(
            ".youtube.com\tTRUE\t/\tTRUE\t2147483647\tSAPISID\ttest-sapisid\n");
        using var httpClient = new HttpClient(new HomeResponseHandler(response));
        using var session = new InnerTubeSession(
            new YouTubeClientOptions { Authentication = authentication }, httpClient);

        var page = await new FeedsHandler(session).GetHomePageAsync(CancellationToken.None);

        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task AnonymousHomeRemainsUsableWhenResponseReportsLoggedOut()
    {
        using var httpClient = new HttpClient(new HomeResponseHandler(
            """{"responseContext":{"mainAppWebResponseContext":{"loggedOut":true}}}"""));
        using var session = new InnerTubeSession(new YouTubeClientOptions(), httpClient);

        var page = await new FeedsHandler(session).GetHomePageAsync(CancellationToken.None);

        Assert.Empty(page.Items);
    }

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
    public async Task AccountProfileAcceptsRenamedAccountHeaderRenderer()
    {
        var authentication = YouTubeCookieAuthentication.FromNetscape(
            ".youtube.com\tTRUE\t/\tTRUE\t2147483647\tSAPISID\ttest-sapisid\n");
        var handler = new AccountMenuHandler();
        using var httpClient = new HttpClient(handler);
        using var session = new InnerTubeSession(
            new YouTubeClientOptions { Authentication = authentication },
            httpClient);


        var profile = await new AccountHandler(session).GetProfileAsync(CancellationToken.None);

        Assert.Equal("Test User", profile.DisplayName);
        Assert.Equal("@testuser", profile.Handle);
    }
    private sealed class RetryingAccountMenuHandler : HttpMessageHandler
    {
        public int AccountMenuRequests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post)
            {
                AccountMenuRequests++;
                var content = AccountMenuRequests == 1
                    ? "{}"
                    : """{"actions":[{"openPopupAction":{"popup":{"multiPageMenuRenderer":{"header":{"accountHeaderRenderer":{"accountName":{"simpleText":"Test User"},"channelHandle":{"simpleText":"@testuser"}}}}}}}]}""";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content, Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html></html>", Encoding.UTF8, "text/html")
            });
        }
    }

    private sealed class AccountMenuHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"actions":[{"openPopupAction":{"popup":{"multiPageMenuRenderer":{"header":{"accountHeaderRenderer":{"accountName":{"simpleText":"Test User"},"channelHandle":{"simpleText":"@testuser"}}}}}}}]}""",
                        Encoding.UTF8,
                        "application/json")
                });

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html></html>", Encoding.UTF8, "text/html")
            });
        }
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

    [Fact]
    public async Task ChannelHandleResolutionRateLimitIsNotReportedAsNotFound()
    {
        using var httpClient = new HttpClient(new StatusHandler(HttpStatusCode.TooManyRequests));
        using var session = new InnerTubeSession(new YouTubeClientOptions(), httpClient);

        await Assert.ThrowsAsync<RateLimitedException>(() =>
            session.ResolveChannelIdAsync(ChannelReference.FromHandle("creator"), CancellationToken.None));
    }

    [Fact]
    public async Task UnavailableChannelSortDoesNotReturnNewestVideos()
    {
        using var httpClient = new HttpClient(new StatusHandler(HttpStatusCode.OK, "{}"));
        using var session = new InnerTubeSession(new YouTubeClientOptions(), httpClient);

        await Assert.ThrowsAsync<ResourceUnavailableException>(() =>
            new ChannelsHandler(session).GetVideosPageAsync(
                ChannelReference.FromId(new ChannelId("UC1234567890123456789012")),
                Models.Enums.ChannelVideoSort.Popular,
                CancellationToken.None));
    }

    [Fact]
    public async Task TranscriptCaptionRetrievalFailureIsNotReportedAsEmptyTranscript()
    {
        using var httpClient = new HttpClient(new StatusHandler(HttpStatusCode.ServiceUnavailable, "{}"));
        using var session = new InnerTubeSession(new YouTubeClientOptions(), httpClient);

        await Assert.ThrowsAsync<YouTubeRequestException>(() =>
            new VideosHandler(session).GetTranscriptAsync(
                new VideoId("dQw4w9WgXcQ"), new TranscriptTrackId("en"), CancellationToken.None));
    }

    private sealed class StatusHandler(HttpStatusCode statusCode, string content = "") : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
        }
    }
    private sealed class HomeResponseHandler(string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    request.Method == HttpMethod.Post ? content : "<html></html>",
                    Encoding.UTF8,
                    request.Method == HttpMethod.Post ? "application/json" : "text/html")
            });
        }
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