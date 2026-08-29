using Xunit;
using YoutubeAPI.Models.Enums;
using YoutubeAPI.Models.Playlists;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.LiveTests;

[Trait("Category", "Mutation")]
public class MutationLiveTests
{
    [Fact]
    public async Task SubscribeAndUnsubscribeChannelLive()
    {
        if (!LiveTestEnvironment.IsMutationEnabled())
            return;

        using var client = LiveTestEnvironment.CreateAuthenticatedClient();
        if (client == null)
            return;

        var channelIdStr = Environment.GetEnvironmentVariable("YOUTUBE_MUTATION_CHANNEL_ID")
                           ?? LiveTestEnvironment.KnownChannelReference;

        if (!ChannelId.TryParse(channelIdStr, out var channelId))
            return;

        try
        {
            await client.Account.SubscribeAsync(channelId);
        }
        finally
        {
            try
            {
                await client.Account.UnsubscribeAsync(channelId);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public async Task SetAndClearRatingLive()
    {
        if (!LiveTestEnvironment.IsMutationEnabled())
            return;

        using var client = LiveTestEnvironment.CreateAuthenticatedClient();
        if (client == null)
            return;

        var videoId = new VideoId(LiveTestEnvironment.KnownPublicVideoId);

        var originalRating = VideoRating.None;
        try
        {
            originalRating = await client.Ratings.GetAsync(videoId);
        }
        catch
        {
            // Ignore if unable to read original rating
        }

        try
        {
            await client.Ratings.SetAsync(videoId, VideoRating.Like);
        }
        finally
        {
            try
            {
                await client.Ratings.SetAsync(videoId,
                    originalRating == VideoRating.Like ? VideoRating.None : originalRating);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public async Task PlaylistLifecycleCreateAddDuplicateRemoveOccurrenceDeleteLive()
    {
        if (!LiveTestEnvironment.IsMutationEnabled())
            return;

        using var client = LiveTestEnvironment.CreateAuthenticatedClient();
        if (client == null)
            return;

        var videoId = new VideoId(LiveTestEnvironment.KnownPublicVideoId);
        var playlistTitle = $"Test_Live_{Guid.NewGuid():N}";
        PlaylistId? createdPlaylistId = null;

        try
        {
            createdPlaylistId = await client.Playlists.CreateAsync(
                new CreatePlaylistRequest(playlistTitle, "Disposable live test playlist"));

            Assert.NotNull(createdPlaylistId);
            Assert.False(string.IsNullOrWhiteSpace(createdPlaylistId.Value.Value));

            // Add the same video twice to verify duplicate occurrences
            await client.Playlists.AddVideoAsync(createdPlaylistId.Value, videoId);
            await client.Playlists.AddVideoAsync(createdPlaylistId.Value, videoId);

            // Reload playlist items to obtain occurrence IDs
            var itemsPage = await client.Playlists.GetItemsPageAsync(createdPlaylistId.Value);
            Assert.NotNull(itemsPage);
            Assert.True(itemsPage.Items.Count >= 2);

            var itemToRemove = itemsPage.Items.FirstOrDefault(item => item.Id != null);
            if (itemToRemove?.Id != null)
            {
                await client.Playlists.RemoveItemAsync(createdPlaylistId.Value, itemToRemove.Id.Value);

                var updatedPage = await client.Playlists.GetItemsPageAsync(createdPlaylistId.Value);
                Assert.NotNull(updatedPage);
                Assert.True(updatedPage.Items.Count < itemsPage.Items.Count);
            }
        }
        finally
        {
            if (createdPlaylistId.HasValue)
                try
                {
                    await client.Playlists.DeleteAsync(createdPlaylistId.Value);
                }
                catch
                {
                    // Best effort cleanup
                }
        }
    }

}