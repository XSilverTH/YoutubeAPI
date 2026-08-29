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

}