namespace YoutubeAPI.Models.Common;

/// <summary>
///     Represents an image thumbnail with its resolution dimensions.
/// </summary>
/// <param name="Url">The direct URL of the thumbnail image.</param>
/// <param name="Width">The width of the thumbnail in pixels.</param>
/// <param name="Height">The height of the thumbnail in pixels.</param>
public sealed record Thumbnail(
    Uri Url,
    int Width,
    int Height);