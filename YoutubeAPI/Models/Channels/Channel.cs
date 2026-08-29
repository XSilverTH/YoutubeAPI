using YoutubeAPI.Models.Common;

namespace YoutubeAPI.Models.Channels;

/// <summary>
///     Represents full metadata for a YouTube channel including description and banners.
/// </summary>
/// <param name="Summary">The core summary information for the channel.</param>
/// <param name="Description">The full textual channel description.</param>
/// <param name="Banners">The available channel banner images across resolutions.</param>
public sealed record Channel(
    ChannelSummary Summary,
    string Description,
    IReadOnlyList<Thumbnail> Banners);