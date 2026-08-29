using System.Buffers.Text;
using System.Text;
using Xunit;
using YoutubeAPI.Models.Continuations;
using YoutubeAPI.Models.Enums;

namespace YoutubeAPI.Tests.Continuations;

public sealed class ContinuationTests
{
    [Fact]
    public void SearchContinuationRoundtripPreservesState()
    {
        var original = new SearchContinuation("server_token_123", "test query", SearchKind.Video, "tracking_abc");
        var exported = original.Export();

        Assert.False(string.IsNullOrWhiteSpace(exported));

        var imported = SearchContinuation.Import(exported);
        Assert.Equal(original.Token, imported.Token);
        Assert.Equal(original.Query, imported.Query);
        Assert.Equal(original.Kind, imported.Kind);
        Assert.Equal(original.TrackingParams, imported.TrackingParams);
    }

    [Fact]
    public void ChannelVideosContinuationRoundtripPreservesState()
    {
        var original = new ChannelVideosContinuation("token_chan_vid", "UCuAXFkgsw1L7xaCfnd5JJOw",
            ChannelVideoSort.Popular, "tp_123");
        var exported = original.Export();

        var imported = ChannelVideosContinuation.Import(exported);
        Assert.Equal(original.Token, imported.Token);
        Assert.Equal(original.Channel, imported.Channel);
        Assert.Equal(original.Sort, imported.Sort);
        Assert.Equal(original.TrackingParams, imported.TrackingParams);
    }

    [Fact]
    public void ChannelPlaylistsContinuationRoundtripPreservesState()
    {
        var original = new ChannelPlaylistsContinuation("token_chan_pl", "UCuAXFkgsw1L7xaCfnd5JJOw", "tp_456");
        var exported = original.Export();

        var imported = ChannelPlaylistsContinuation.Import(exported);
        Assert.Equal(original.Token, imported.Token);
        Assert.Equal(original.Channel, imported.Channel);
        Assert.Equal(original.TrackingParams, imported.TrackingParams);
    }

    [Fact]
    public void PlaylistItemsContinuationRoundtripPreservesState()
    {
        var original = new PlaylistItemsContinuation("token_pl_items", "PLrAXtmErZgOdP_8GztsuKi9nrraNbKKp4", "tp_789");
        var exported = original.Export();

        var imported = PlaylistItemsContinuation.Import(exported);
        Assert.Equal(original.Token, imported.Token);
        Assert.Equal(original.PlaylistId, imported.PlaylistId);
        Assert.Equal(original.TrackingParams, imported.TrackingParams);
    }

    [Fact]
    public void OwnedPlaylistsContinuationRoundtripPreservesProfileBinding()
    {
        var original = new OwnedPlaylistsContinuation("token_owned_pl", "profile_user_1", "tp_owned");
        var exported = original.Export();

        var imported = OwnedPlaylistsContinuation.Import(exported);
        Assert.Equal(original.Token, imported.Token);
        Assert.Equal("profile_user_1", imported.ProfileId);
        Assert.Equal(original.TrackingParams, imported.TrackingParams);
    }

    [Fact]
    public void CommentThreadsContinuationRoundtripPreservesState()
    {
        var original = new CommentThreadsContinuation("token_ct", "dQw4w9WgXcQ", CommentSort.Newest, "tp_ct");
        var exported = original.Export();

        var imported = CommentThreadsContinuation.Import(exported);
        Assert.Equal(original.Token, imported.Token);
        Assert.Equal(original.VideoId, imported.VideoId);
        Assert.Equal(original.Sort, imported.Sort);
        Assert.Equal(original.TrackingParams, imported.TrackingParams);
    }

    [Fact]
    public void CommentRepliesContinuationRoundtripPreservesState()
    {
        var original = new CommentRepliesContinuation("token_cr", "Ugx12345", "tp_cr");
        var exported = original.Export();

        var imported = CommentRepliesContinuation.Import(exported);
        Assert.Equal(original.Token, imported.Token);
        Assert.Equal(original.Target, imported.Target);
        Assert.Equal(original.TrackingParams, imported.TrackingParams);
    }

    [Fact]
    public void HomeContinuationRoundtripPreservesState()
    {
        var original = new HomeContinuation("token_home", "tp_home");
        var exported = original.Export();

        var imported = HomeContinuation.Import(exported);
        Assert.Equal(original.Token, imported.Token);
        Assert.Equal(original.TrackingParams, imported.TrackingParams);
    }

    [Fact]
    public void SubscriptionsContinuationRoundtripPreservesProfileBinding()
    {
        var original = new SubscriptionsContinuation("token_subs", "profile_subs_1", "tp_subs");
        var exported = original.Export();

        var imported = SubscriptionsContinuation.Import(exported);
        Assert.Equal(original.Token, imported.Token);
        Assert.Equal("profile_subs_1", imported.ProfileId);
        Assert.Equal(original.TrackingParams, imported.TrackingParams);
    }

    [Fact]
    public void SubscribedChannelsContinuationRoundtripPreservesProfileBinding()
    {
        var original = new SubscribedChannelsContinuation("token_sub_channels", "profile_sc_1", "tp_sc");
        var exported = original.Export();

        var imported = SubscribedChannelsContinuation.Import(exported);
        Assert.Equal(original.Token, imported.Token);
        Assert.Equal("profile_sc_1", imported.ProfileId);
        Assert.Equal(original.TrackingParams, imported.TrackingParams);
    }

    [Fact]
    public void HistoryContinuationRoundtripPreservesProfileBinding()
    {
        var original = new HistoryContinuation("token_hist", "profile_hist_1", "tp_hist");
        var exported = original.Export();

        var imported = HistoryContinuation.Import(exported);
        Assert.Equal(original.Token, imported.Token);
        Assert.Equal("profile_hist_1", imported.ProfileId);
        Assert.Equal(original.TrackingParams, imported.TrackingParams);
    }

    [Fact]
    public void ImportWithWrongRouteThrowsFormatException()
    {
        var search = new SearchContinuation("token_search");
        var searchExported = search.Export();

        // Trying to import search token as History continuation must fail
        Assert.Throws<FormatException>(() => HistoryContinuation.Import(searchExported));

        // Trying to import search token as Home continuation must fail
        Assert.Throws<FormatException>(() => HomeContinuation.Import(searchExported));

        // Trying to import search token as Subscriptions continuation must fail
        Assert.Throws<FormatException>(() => SubscriptionsContinuation.Import(searchExported));

        // Trying to import search token as ChannelVideos continuation must fail
        Assert.Throws<FormatException>(() => ChannelVideosContinuation.Import(searchExported));
    }

    [Fact]
    public void ImportWithNullThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SearchContinuation.Import(null!));
        Assert.Throws<ArgumentNullException>(() => HistoryContinuation.Import(null!));
        Assert.Throws<ArgumentNullException>(() => HomeContinuation.Import(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not_valid_base64_url!@#$")]
    [InlineData("Zm9v")] // base64 for "foo", not a JSON object
    public void ImportWithMalformedInputThrowsFormatException(string invalidInput)
    {
        Assert.Throws<FormatException>(() => SearchContinuation.Import(invalidInput));
    }

    [Fact]
    public void ContinuationEnvelopeWithUnsupportedVersionThrowsFormatException()
    {
        // Construct envelope payload with version 99
        const string json = "{\"v\":99,\"r\":\"search\",\"t\":\"tok\"}";
        var b64 = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(json));

        Assert.Throws<FormatException>(() => ContinuationEnvelope.Decode(b64));
    }

    [Fact]
    public void ContinuationEnvelopeWithMissingRouteOrTokenThrowsFormatException()
    {
        // Missing token
        const string jsonNoToken = "{\"v\":1,\"r\":\"search\"}";
        var b64NoToken = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(jsonNoToken));
        Assert.Throws<FormatException>(() => ContinuationEnvelope.Decode(b64NoToken));

        // Missing route
        const string jsonNoRoute = "{\"v\":1,\"t\":\"tok\"}";
        var b64NoRoute = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(jsonNoRoute));
        Assert.Throws<FormatException>(() => ContinuationEnvelope.Decode(b64NoRoute));
    }
}