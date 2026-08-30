using System.Text.Json;
using Xunit;
using YoutubeAPI.Infrastructure;

namespace YoutubeAPI.Tests;

public class ParserTests
{
    [Theory]
    [InlineData("3:45", 0, 3, 45)]
    [InlineData("1:02:30", 1, 2, 30)]
    [InlineData("15", 0, 0, 15)]
    public void ParseDurationValidInputsReturnsExpectedTimeSpan(string input, int hours, int minutes, int seconds)
    {
        var duration = InnerTubeElement.ParseDuration(input);
        Assert.NotNull(duration);
        Assert.Equal(new TimeSpan(hours, minutes, seconds), duration.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    public void ParseDurationInvalidInputsReturnsNull(string? input)
    {
        Assert.Null(InnerTubeElement.ParseDuration(input));
    }

    [Theory]
    [InlineData("408M views", 408_000_000)]
    [InlineData("1.2M views", 1_200_000)]
    [InlineData("500K subscribers", 500_000)]
    [InlineData("1,234,567 views", 1_234_567)]
    [InlineData("985 replies", 985)]
    public void ParseCountValidInputsReturnsExpectedLong(string input, long expected)
    {
        var count = InnerTubeElement.ParseCount(input);
        Assert.NotNull(count);
        Assert.Equal(expected, count.Value);
    }

    [Fact]
    public void GetTextHandlesRunsAndSimpleTextAndContent()
    {
        using var runsDoc = JsonDocument.Parse("{\"runs\": [{\"text\": \"Hello \"}, {\"text\": \"World\"}]}");
        Assert.Equal("Hello World", runsDoc.RootElement.GetText());

        using var simpleDoc = JsonDocument.Parse("{\"simpleText\": \"Direct Title\"}");
        Assert.Equal("Direct Title", simpleDoc.RootElement.GetText());

        using var contentDoc = JsonDocument.Parse("{\"content\": \"Modern Content\"}");
        Assert.Equal("Modern Content", contentDoc.RootElement.GetText());
    }

    [Fact]
    public void GetThumbnailsHandlesVariousThumbnailProperties()
    {
        using var thumbDoc =
            JsonDocument.Parse(
                "{\"thumbnails\": [{\"url\": \"//yt3.ggpht.com/photo.jpg\", \"width\": 100, \"height\": 100}]}");
        var list = thumbDoc.RootElement.GetThumbnails();
        Assert.Single(list);
        Assert.Equal("https://yt3.ggpht.com/photo.jpg", list[0].Url.ToString());
        Assert.Equal(100, list[0].Width);
        Assert.Equal(100, list[0].Height);
    }

    [Fact]
    public void ParseVideoSummaryUsesNextBylineWhenOwnerTextHasNoText()
    {
        using var doc = JsonDocument.Parse("""
            {
              "videoId": "dQw4w9WgXcQ",
              "title": {"simpleText": "Video"},
              "ownerText": {"runs": []},
              "longBylineText": {"runs": [{
                "text": "Channel Name",
                "navigationEndpoint": {"browseEndpoint": {"browseId": "UC1234567890123456789012"}}
              }]}
            }
            """);

        var summary = SearchHandler.ParseVideoSummary(doc.RootElement);

        Assert.NotNull(summary);
        Assert.Equal("Channel Name", summary.Channel.Title);
        Assert.Equal("UC1234567890123456789012", summary.Channel.Id.Value);
    }

    [Fact]
    public void ParseVideoSummaryUsesGridVideoBylineFields()
    {
        using var doc = JsonDocument.Parse("""
            {
              "videoId": "dQw4w9WgXcQ",
              "title": {"simpleText": "Video"},
              "shortBylineText": {"runs": [{
                "text": "Grid Channel",
                "navigationEndpoint": {"browseEndpoint": {"browseId": "UC1234567890123456789012"}}
              }]}
            }
            """);

        var summary = SearchHandler.ParseVideoSummary(doc.RootElement);

        Assert.NotNull(summary);
        Assert.Equal("Grid Channel", summary.Channel.Title);
        Assert.Equal("UC1234567890123456789012", summary.Channel.Id.Value);
    }

    [Fact]
    public void ParseLockupViewModelExtractsChannelMetadata()
    {
        using var doc = JsonDocument.Parse("""
            {
              "contentType": "LOCKUP_CONTENT_TYPE_VIDEO",
              "contentId": "dQw4w9WgXcQ",
              "metadata": {
                "lockupMetadataViewModel": {
                  "title": {"content": "Video"},
                  "metadata": {
                    "contentMetadataViewModel": {
                      "metadataRows": [{
                        "metadataParts": [{
                          "text": {"content": "Lockup Channel"},
                          "endpoint": {"browseEndpoint": {"browseId": "UC1234567890123456789012"}}
                        }]
                      }, {
                        "metadataParts": [{"text": {"content": "3 days ago"}}]
                      }]
                    }
                  }
                }
              }
            }
            """);

        var result = Assert.IsType<YoutubeAPI.Models.Search.VideoSearchResult>(
            SearchHandler.ParseLockupViewModel(doc.RootElement));

        Assert.Equal("Lockup Channel", result.Video.Channel.Title);
        Assert.Equal("UC1234567890123456789012", result.Video.Channel.Id.Value);
        Assert.Equal("3 days ago", result.Video.PublishedText);
        Assert.NotNull(result.Video.PublishedAt);
    }


    [Fact]
    public void ParsePlaybackProgressReadsClassicRendererAndSeparateResumePosition()
    {
        using var doc = JsonDocument.Parse("""
            {
              "videoId": "dQw4w9WgXcQ",
              "isWatched": false,
              "thumbnailOverlays": [{
                "thumbnailOverlayResumePlaybackRenderer": {
                  "percentDurationWatched": 42
                }
              }],
              "playbackContext": {
                "contentPlaybackContext": {
                  "resumePlaybackPositionMs": "123000"
                }
              }
            }
            """);

        var progress = InnerTubeElement.ParsePlaybackProgress(doc.RootElement);

        Assert.NotNull(progress);
        Assert.True(progress.HasProgress);
        Assert.Equal(0.42, progress.WatchedFraction);
        Assert.Equal(42, progress.WatchedPercentage);
        Assert.True(progress.HasResumePosition);
        Assert.Equal(TimeSpan.FromSeconds(123), progress.ResumePosition);
        Assert.False(progress.IsCompleted);
    }


    [Fact]
    public void ParsePlaybackProgressReadsCurrentVideoEndpointResumeTime()
    {
        using var doc = JsonDocument.Parse(
            """{"currentVideoEndpoint":{"watchEndpoint":{"startTimeSeconds":87.5}}}""");

        var progress = InnerTubeElement.ParsePlaybackProgress(doc.RootElement);

        Assert.NotNull(progress);
        Assert.False(progress.HasProgress);
        Assert.True(progress.HasResumePosition);
        Assert.Equal(TimeSpan.FromSeconds(87.5), progress.ResumePosition);
    }

    [Fact]
    public void ParsePlaybackProgressReadsNavigationResumeTimeAndWatchedLabel()
    {
        using var doc = JsonDocument.Parse("""
            {
              "navigationEndpoint": {
                "watchEndpoint": {
                  "startTimeSeconds": 64
                }
              },
              "thumbnailOverlays": [{
                "thumbnailOverlayPlaybackStatusRenderer": {
                  "texts": [{"simpleText": "WATCHED"}]
                }
              }]
            }
            """);

        var progress = InnerTubeElement.ParsePlaybackProgress(doc.RootElement);

        Assert.NotNull(progress);
        Assert.True(progress.HasResumePosition);
        Assert.Equal(TimeSpan.FromSeconds(64), progress.ResumePosition);
        Assert.True(progress.IsCompleted);
    }
    [Fact]
    public void ParsePlaybackProgressReadsModernViewModel()
    {
        using var doc = JsonDocument.Parse("""
            {
              "lockupViewModel": {
                "contentImage": {
                  "thumbnailViewModel": {
                    "overlays": [{
                      "thumbnailBottomOverlayViewModel": {
                        "progressBar": {
                          "thumbnailOverlayProgressBarViewModel": {
                            "startPercent": 67
                          }
                        }
                      }
                    }]
                  }
                }
              }
            }
            """);

        var progress = InnerTubeElement.ParsePlaybackProgress(doc.RootElement);

        Assert.NotNull(progress);
        Assert.True(progress.HasProgress);
        Assert.Equal(0.67, progress.WatchedFraction);
        Assert.False(progress.HasResumePosition);
    }

    [Fact]
    public void ParsePlaybackProgressDistinguishesCompletedVideoWithoutResumePosition()
    {
        using var doc = JsonDocument.Parse("""
            {
              "videoId": "dQw4w9WgXcQ",
              "isWatched": true,
              "thumbnailOverlays": [{
                "thumbnailOverlayResumePlaybackRenderer": {
                  "percentDurationWatched": 100
                }
              }]
            }
            """);

        var progress = InnerTubeElement.ParsePlaybackProgress(doc.RootElement);

        Assert.NotNull(progress);
        Assert.True(progress.HasProgress);
        Assert.Equal(1, progress.WatchedFraction);
        Assert.True(progress.IsCompleted);
        Assert.False(progress.HasResumePosition);
        Assert.Null(progress.ResumePosition);
    }

    [Fact]
    public void ParsePlaybackProgressReturnsNullWhenResponseHasNoUserState()
    {
        using var doc = JsonDocument.Parse("""
            {
              "videoId": "dQw4w9WgXcQ",
              "videoDetails": { "title": "Public video" },
              "thumbnailOverlays": [{
                "thumbnailOverlayTimeStatusRenderer": {
                  "text": { "simpleText": "3:45" }
                }
              }]
            }
            """);

        Assert.Null(InnerTubeElement.ParsePlaybackProgress(doc.RootElement));
    }

    [Fact]
    public void ParsePlaybackProgressIgnoresMalformedAndUnexpectedValues()
    {
        using var doc = JsonDocument.Parse("""
            {
              "isWatched": "yes",
              "thumbnailOverlays": [{
                "thumbnailOverlayResumePlaybackRenderer": {
                  "percentDurationWatched": "not-a-number"
                }
              }],
              "playbackContext": {
                "contentPlaybackContext": {
                  "resumePlaybackPositionMs": -1
                }
              }
            }
            """);

        Assert.Null(InnerTubeElement.ParsePlaybackProgress(doc.RootElement));
    }

    [Fact]
    public void ParsePlaybackProgressUsesTheSameRulesForClassicAndModernFeedShapes()
    {
        using var classic = JsonDocument.Parse(
            """{"thumbnailOverlays":[{"thumbnailOverlayResumePlaybackRenderer":{"percentDurationWatched":25}}]}""");
        using var modern = JsonDocument.Parse(
            """{"contentImage":{"thumbnailViewModel":{"overlays":[{"thumbnailBottomOverlayViewModel":{"progressBar":{"thumbnailOverlayProgressBarViewModel":{"startPercent":25}}}}]}}}""");

        var classicProgress = InnerTubeElement.ParsePlaybackProgress(classic.RootElement);
        var modernProgress = InnerTubeElement.ParsePlaybackProgress(modern.RootElement);

        Assert.NotNull(classicProgress);
        Assert.NotNull(modernProgress);
        Assert.Equal(classicProgress.WatchedFraction, modernProgress.WatchedFraction);
    }
}
