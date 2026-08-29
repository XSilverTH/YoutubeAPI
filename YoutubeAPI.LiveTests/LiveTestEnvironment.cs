namespace YoutubeAPI.LiveTests;

internal static class LiveTestEnvironment
{
    public const string KnownPublicVideoId = "dQw4w9WgXcQ";
    public const string KnownChannelReference = "UC4QobU6STFB0P71PMvOGN5A";
    public const string KnownPlaylistId = "PLrAXtmErZgOdP_8GztsuKi9nrraNbKKp4";

    public static bool IsPublicEnabled()
    {
        var runLive = Environment.GetEnvironmentVariable("YOUTUBE_RUN_LIVE_TESTS")
                      ?? Environment.GetEnvironmentVariable("YOUTUBE_LIVE_TESTS")
                      ?? Environment.GetEnvironmentVariable("YOUTUBE_RUN_PUBLIC_TESTS")
                      ?? Environment.GetEnvironmentVariable("YOUTUBE_PUBLIC_LIVE_TESTS");

        if (string.Equals(runLive, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(runLive, "true", StringComparison.OrdinalIgnoreCase))
            return true;

        var cookiesPath = Environment.GetEnvironmentVariable("YOUTUBE_COOKIES_FILE");
        return !string.IsNullOrEmpty(cookiesPath) && File.Exists(cookiesPath);
    }

    private static string? GetCookiesFilePath()
    {
        var path = Environment.GetEnvironmentVariable("YOUTUBE_COOKIES_FILE");
        if (string.IsNullOrWhiteSpace(path)) return null;

        return File.Exists(path) ? path : null;
    }

    public static bool IsAuthenticatedEnabled()
    {
        return GetCookiesFilePath() != null;
    }

    public static bool IsMutationEnabled()
    {
        if (!IsAuthenticatedEnabled()) return false;

        var mut = Environment.GetEnvironmentVariable("YOUTUBE_RUN_MUTATION_TESTS");
        return string.Equals(mut, "1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(mut, "true", StringComparison.OrdinalIgnoreCase);
    }

    public static YouTubeClient? CreateAuthenticatedClient()
    {
        var path = GetCookiesFilePath();
        if (path == null) return null;

        var content = File.ReadAllText(path);
        var auth = YouTubeCookieAuthentication.FromNetscape(content);
        var options = new YouTubeClientOptions { Authentication = auth };
        return new YouTubeClient(options);
    }
}