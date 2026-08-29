using Xunit;
using YoutubeAPI.Models.Continuations;
using YoutubeAPI.Models.Search;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.LiveTests;

[Trait("Category", "Public")]
public class PublicLiveTests
{
    [Fact]
    public async Task GetVideoAsyncLiveReturnsNonBlankMetadata()
    {
        if (!LiveTestEnvironment.IsPublicEnabled())
            return;

        using var client = new YouTubeClient();
        var video = await client.Videos.GetAsync(new VideoId(LiveTestEnvironment.KnownPublicVideoId));
        Assert.NotNull(video);
        Assert.Equal(LiveTestEnvironment.KnownPublicVideoId, video.Summary.Id.Value);
        Assert.False(string.IsNullOrWhiteSpace(video.Summary.Title));
        Assert.NotNull(video.Summary.Channel);
        Assert.False(string.IsNullOrWhiteSpace(video.Summary.Channel.Title));
        Assert.NotNull(video.Summary.Duration);
        Assert.NotEmpty(video.Summary.Thumbnails);
    }

    [Fact]
    public async Task GetTranscriptTracksAsyncLiveReturnsTracksAndCues()
    {
        if (!LiveTestEnvironment.IsPublicEnabled())
            return;

        using var client = new YouTubeClient();
        var tracks = await client.Videos.GetTranscriptTracksAsync(new VideoId(LiveTestEnvironment.KnownPublicVideoId));
        Assert.NotNull(tracks);
        if (tracks.Count > 0)
        {
            var firstTrack = tracks[0];
            Assert.False(string.IsNullOrWhiteSpace(firstTrack.LanguageCode));
            var transcript =
                await client.Videos.GetTranscriptAsync(new VideoId(LiveTestEnvironment.KnownPublicVideoId),
                    firstTrack.Id);
            Assert.NotNull(transcript);
            Assert.Equal(LiveTestEnvironment.KnownPublicVideoId, transcript.VideoId.Value);
            Assert.NotNull(transcript.Cues);
        }
    }

    [Fact]
    public async Task SearchGetPageAsyncLiveReturnsTypedResultsAndResumesContinuation()
    {
        if (!LiveTestEnvironment.IsPublicEnabled())
            return;

        using var client = new YouTubeClient();
        var page = await client.Search.GetPageAsync(new SearchRequest("dotnet native aot"));
        Assert.NotNull(page);
        Assert.NotEmpty(page.Items);

        var firstItem = page.Items[0];
        Assert.True(firstItem is VideoSearchResult or ChannelSearchResult or PlaylistSearchResult);

        if (page.Next != null)
        {
            var exported = page.Next.Export();
            Assert.False(string.IsNullOrWhiteSpace(exported));

            var imported = SearchContinuation.Import(exported);
            Assert.NotNull(imported);

            var page2 = await client.Search.GetPageAsync(imported);
            Assert.NotNull(page2);
            Assert.NotNull(page2.Items);
        }
    }

    [Fact]
    public async Task ChannelsGetAsyncLiveReturnsChannelMetadata()
    {
        if (!LiveTestEnvironment.IsPublicEnabled())
            return;

        using var client = new YouTubeClient();
        var channel = await client.Channels.GetAsync(ChannelReference.Parse(LiveTestEnvironment.KnownChannelReference));
        Assert.NotNull(channel);
        Assert.False(string.IsNullOrWhiteSpace(channel.Summary.Title));
        Assert.NotNull(channel.Summary.Url);
    }

    [Fact]
    public async Task ChannelsGetVideosPageAsyncLivePreservesOrderAndSort()
    {
        if (!LiveTestEnvironment.IsPublicEnabled())
            return;

        using var client = new YouTubeClient();
        var page = await client.Channels.GetVideosPageAsync(
            ChannelReference.Parse(LiveTestEnvironment.KnownChannelReference));
        Assert.NotNull(page);
        Assert.NotEmpty(page.Items);

        foreach (var video in page.Items)
        {
            Assert.False(string.IsNullOrWhiteSpace(video.Title));
            Assert.False(string.IsNullOrWhiteSpace(video.Id.Value));
        }

        if (page.Next != null)
        {
            var exported = page.Next.Export();
            var imported = ChannelVideosContinuation.Import(exported);
            var nextPage = await client.Channels.GetVideosPageAsync(imported);
            Assert.NotNull(nextPage);
        }
    }

    [Fact]
    public async Task ChannelsGetPlaylistsPageAsyncLiveReturnsPlaylists()
    {
        if (!LiveTestEnvironment.IsPublicEnabled())
            return;

        using var client = new YouTubeClient();
        var page = await client.Channels.GetPlaylistsPageAsync(ChannelReference.Parse("UC_x5XG1OV2P6uZZ5FSM9Ttw"));
        Assert.NotNull(page);
        if (page.Items.Count > 0)
        {
            var firstPlaylist = page.Items[0];
            Assert.False(string.IsNullOrWhiteSpace(firstPlaylist.Title));
            Assert.False(string.IsNullOrWhiteSpace(firstPlaylist.Id.Value));
        }
    }

    [Fact]
    public async Task PlaylistsGetAsyncLiveReturnsPlaylistMetadata()
    {
        if (!LiveTestEnvironment.IsPublicEnabled())
            return;

        using var client = new YouTubeClient();
        var playlist = await client.Playlists.GetAsync(new PlaylistId(LiveTestEnvironment.KnownPlaylistId));
        Assert.NotNull(playlist);
        Assert.False(string.IsNullOrWhiteSpace(playlist.Summary.Title));
        Assert.NotNull(playlist.Summary.Url);
    }

    [Fact]
    public async Task PlaylistsGetItemsPageAsyncLivePreservesOrderAndResumes()
    {
        if (!LiveTestEnvironment.IsPublicEnabled())
            return;

        using var client = new YouTubeClient();
        var page = await client.Playlists.GetItemsPageAsync(new PlaylistId(LiveTestEnvironment.KnownPlaylistId));
        Assert.NotNull(page);
        Assert.NotEmpty(page.Items);

        foreach (var item in page.Items)
        {
            Assert.False(string.IsNullOrWhiteSpace(item.DisplayTitle));
            if (item.Position.HasValue) Assert.True(item.Position.Value >= 1);
        }

        if (page.Next != null)
        {
            var exported = page.Next.Export();
            var imported = PlaylistItemsContinuation.Import(exported);
            var nextPage = await client.Playlists.GetItemsPageAsync(imported);
            Assert.NotNull(nextPage);
        }
    }

    [Fact]
    public async Task CommentsGetThreadsPageAsyncLiveReturnsThreadsAndReplies()
    {
        if (!LiveTestEnvironment.IsPublicEnabled())
            return;

        using var client = new YouTubeClient();
        var page = await client.Comments.GetThreadsPageAsync(new VideoId(LiveTestEnvironment.KnownPublicVideoId));
        Assert.NotNull(page);
        Assert.NotEmpty(page.Items);

        var firstThread = page.Items[0];
        Assert.NotNull(firstThread.TopLevel);
        Assert.False(string.IsNullOrWhiteSpace(firstThread.TopLevel.Text));
        Assert.NotNull(firstThread.TopLevel.Author);

        if (firstThread.NextReplies != null)
        {
            var exported = firstThread.NextReplies.Export();
            var imported = CommentRepliesContinuation.Import(exported);
            var repliesPage = await client.Comments.GetRepliesPageAsync(imported);
            Assert.NotNull(repliesPage);
        }
    }


    [Fact]
    public async Task FeedsGetHomePageAsyncLiveReturnsPage()
    {
        if (!LiveTestEnvironment.IsPublicEnabled())
            return;

        using var client = new YouTubeClient();
        var page = await client.Feeds.GetHomePageAsync();
        Assert.NotNull(page);
        Assert.NotNull(page.Items);
    }
}