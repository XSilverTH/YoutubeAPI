using System.Net;
using Xunit;
using YoutubeAPI.Exceptions;

namespace YoutubeAPI.Tests.Exceptions;

public sealed class ExceptionHierarchyTests
{

    [Fact]
    public void RateLimitedExceptionPreservesProperties()
    {
        var retryAfter = TimeSpan.FromSeconds(30);
        var inner = new InvalidOperationException("inner error");

        // 1. Default constructor
        var ex1 = new RateLimitedException();
        Assert.Null(ex1.RetryAfter);

        // 2. Message and retryAfter
        var ex2 = new RateLimitedException("Too many requests", retryAfter);
        Assert.Equal("Too many requests", ex2.Message);
        Assert.Equal(retryAfter, ex2.RetryAfter);

        // 3. Message, inner, and retryAfter
        var ex3 = new RateLimitedException("Too many requests", inner, retryAfter);
        Assert.Equal("Too many requests", ex3.Message);
        Assert.Same(inner, ex3.InnerException);
        Assert.Equal(retryAfter, ex3.RetryAfter);
    }

    [Fact]
    public void YouTubeRequestExceptionPreservesProperties()
    {
        var inner = new HttpRequestException("HTTP failed");

        // 1. Default constructor
        var ex1 = new YouTubeRequestException();
        Assert.Null(ex1.Operation);
        Assert.Null(ex1.StatusCode);

        // 2. Full constructor
        var ex2 = new YouTubeRequestException("Request failed", "Search", HttpStatusCode.BadGateway, inner);
        Assert.Equal("Request failed", ex2.Message);
        Assert.Equal("Search", ex2.Operation);
        Assert.Equal(HttpStatusCode.BadGateway, ex2.StatusCode);
        Assert.Same(inner, ex2.InnerException);
    }


}