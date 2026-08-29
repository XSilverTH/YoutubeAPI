using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using YoutubeAPI.Exceptions;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Infrastructure;

internal sealed partial class InnerTubeSession : IDisposable
{
    private const string DefaultApiKey = "AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8";
    private const string DefaultClientVersion = "2.20260828.01.00";

    private const string DefaultUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36";

    private const string BaseYouTubeUrl = "https://www.youtube.com";
    private const string InnerTubeApiBase = "https://www.youtube.com/youtubei/v1/";
    private readonly Lock _bootstrapLock = new();

    private readonly bool _ownsHttpClient;
    private Task<BootstrapInfo>? _bootstrapTask;
    private bool _disposed;

    public InnerTubeSession(YouTubeClientOptions? options = null, HttpClient? httpClient = null)
    {
        Options = options ?? new YouTubeClientOptions();
        if (httpClient != null)
        {
            HttpClient = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            var handler = new SocketsHttpHandler
            {
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.All,
                AllowAutoRedirect = true
            };
            HttpClient = new HttpClient(handler, true);
            _ownsHttpClient = true;
        }
    }

    public YouTubeClientOptions Options { get; }

    public HttpClient HttpClient { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_ownsHttpClient) HttpClient.Dispose();
    }

    [GeneratedRegex("\"INNERTUBE_API_KEY\"\\s*:\\s*\"([^\"]+)\"|\"innertubeApiKey\"\\s*:\\s*\"([^\"]+)\"")]
    private static partial Regex ApiKeyRegex();

    [GeneratedRegex("\"INNERTUBE_CONTEXT_CLIENT_VERSION\"\\s*:\\s*\"([^\"]+)\"|\"clientVersion\"\\s*:\\s*\"([^\"]+)\"")]
    private static partial Regex ClientVersionRegex();

    [GeneratedRegex("\"VISITOR_DATA\"\\s*:\\s*\"([^\"]+)\"|\"visitorData\"\\s*:\\s*\"([^\"]+)\"")]
    private static partial Regex VisitorDataRegex();

    private async Task<BootstrapInfo> GetBootstrapInfoAsync(CancellationToken cancellationToken = default)
    {
        Task<BootstrapInfo> task;
        lock (_bootstrapLock)
        {
            if (_bootstrapTask == null || _bootstrapTask.IsFaulted || _bootstrapTask.IsCanceled)
                _bootstrapTask = FetchBootstrapInfoAsync(CancellationToken.None);
            task = _bootstrapTask;
        }

        return await task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public void InvalidateBootstrap()
    {
        lock (_bootstrapLock)
        {
            _bootstrapTask = null;
        }
    }

    private async Task<BootstrapInfo> FetchBootstrapInfoAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseYouTubeUrl}/?hl={Options.Language}");
            request.Headers.UserAgent.ParseAdd(DefaultUserAgent);
            request.Headers.Add("Cookie", BuildCookieHeader(request.RequestUri!));

            using var response = await HttpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new BootstrapInfo(DefaultApiKey, DefaultClientVersion, Options.VisitorData);

            var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var apiKeyMatch = ApiKeyRegex().Match(html);
            var apiKey = apiKeyMatch.Success
                ? apiKeyMatch.Groups[1].Success ? apiKeyMatch.Groups[1].Value : apiKeyMatch.Groups[2].Value
                : DefaultApiKey;

            var versionMatch = ClientVersionRegex().Match(html);
            var clientVersion = versionMatch.Success
                ? versionMatch.Groups[1].Success ? versionMatch.Groups[1].Value : versionMatch.Groups[2].Value
                : DefaultClientVersion;

            var visitorMatch = VisitorDataRegex().Match(html);
            var visitorData = Options.VisitorData;
            if (string.IsNullOrEmpty(visitorData) && visitorMatch.Success)
                visitorData = visitorMatch.Groups[1].Success
                    ? visitorMatch.Groups[1].Value
                    : visitorMatch.Groups[2].Value;

            return new BootstrapInfo(apiKey, clientVersion, visitorData);
        }
        catch
        {
            return new BootstrapInfo(DefaultApiKey, DefaultClientVersion, Options.VisitorData);
        }
    }

    public void EnsureAuthenticated()
    {
        if (Options.Authentication == null)
            throw new AuthenticationRequiredException("This operation requires cookie authentication.");

        Options.Authentication.GetRequiredSapisid();
    }

    public void ValidateContinuationProfile(string? profileId)
    {
        if (string.IsNullOrEmpty(profileId))
            return;

        EnsureAuthenticated();
    }

    public async Task<JsonDocument> PostInnerTubeAsync(
        string endpoint,
        Action<Utf8JsonWriter> writePayload,
        CancellationToken cancellationToken = default)
    {
        var bootstrap = await GetBootstrapInfoAsync(cancellationToken).ConfigureAwait(false);
        var url = $"{InnerTubeApiBase}{endpoint}?key={bootstrap.ApiKey}&prettyPrint=false";

        using var memoryStream = new MemoryStream();
        await using var writer = new Utf8JsonWriter(memoryStream,
            new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        writer.WriteStartObject();
        writer.WritePropertyName("context");
        WriteContextObject(writer, bootstrap);

        writePayload(writer);

        writer.WriteEndObject();

        writer.Flush();
        var jsonBytes = memoryStream.ToArray();

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new ByteArrayContent(jsonBytes);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        ApplyHeaders(request, bootstrap);

        HttpResponseMessage response;
        try
        {
            response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new YouTubeRequestException(
                $"Failed to communicate with YouTube endpoint '{endpoint}': {Sanitize(ex.Message)}", endpoint, null,
                ex);
        }

        using (response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.TooManyRequests:
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta;
                    throw new RateLimitedException("YouTube rate limit exceeded (HTTP 429).", retryAfter);
                }
                case HttpStatusCode.Unauthorized when Options.Authentication != null:
                    throw new AuthenticationExpiredException(
                        "The provided YouTube session has expired or is invalid (HTTP 401).");
                case HttpStatusCode.Unauthorized:
                    throw new AuthenticationRequiredException(
                        "Authentication is required for this operation (HTTP 401).");
                case HttpStatusCode.Forbidden:
                    throw new PermissionDeniedException(
                        "Access to this resource or action was denied by YouTube (HTTP 403).");
                case HttpStatusCode.NotFound:
                    throw new ResourceNotFoundException(
                        $"Requested YouTube resource was not found (HTTP 404) at '{endpoint}'.");
            }

            if (!response.IsSuccessStatusCode)
                throw new YouTubeRequestException(
                    $"YouTube request to '{endpoint}' failed with status {(int)response.StatusCode} ({response.StatusCode}).",
                    endpoint,
                    response.StatusCode);

            JsonDocument document;
            try
            {
                var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new YouTubeProtocolException(
                    $"Failed to parse JSON response from YouTube endpoint '{endpoint}': {Sanitize(ex.Message)}", ex);
            }

            CheckResponseAlerts(document.RootElement, endpoint);
            return document;
        }
    }

    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms",
        Justification = "Required by YouTube SAPISIDHASH protocol")]
    private void ApplyHeaders(HttpRequestMessage request, BootstrapInfo bootstrap)
    {
        request.Headers.UserAgent.ParseAdd(DefaultUserAgent);
        request.Headers.Accept.ParseAdd("*/*");
        request.Headers.AcceptLanguage.ParseAdd($"{Options.Language}-{Options.Region},{Options.Language};q=0.9");
        request.Headers.Add("Origin", BaseYouTubeUrl);
        request.Headers.Add("Referer", $"{BaseYouTubeUrl}/");
        request.Headers.Add("X-YouTube-Client-Name", "1");
        request.Headers.Add("X-YouTube-Client-Version", bootstrap.ClientVersion);

        if (!string.IsNullOrEmpty(bootstrap.VisitorData))
            request.Headers.Add("X-Goog-Visitor-Id", bootstrap.VisitorData);

        if (Options.AuthUser != 0)
            request.Headers.Add("X-Goog-AuthUser", Options.AuthUser.ToString(CultureInfo.InvariantCulture));

        if (!string.IsNullOrEmpty(Options.PageId)) request.Headers.Add("X-Goog-PageId", Options.PageId);

        request.Headers.Add("X-Origin", BaseYouTubeUrl);

        var requestUri = request.RequestUri ?? new Uri(BaseYouTubeUrl);
        request.Headers.Add("Cookie", BuildCookieHeader(requestUri));

        if (Options.Authentication == null) return;
        var sapisid = Options.Authentication.GetSapisidOrSecure();
        if (string.IsNullOrEmpty(sapisid)) return;
        var unixSeconds = Options.TimeProvider.GetUtcNow().ToUnixTimeSeconds();
        var sapisidInput = $"{unixSeconds} {sapisid} {BaseYouTubeUrl}";
        // SHA-1 is required by YouTube's SAPISIDHASH authentication protocol.
        // ReSharper disable once InsecureHashAlgorithm
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(sapisidInput));
        var hashHex = Convert.ToHexStringLower(hash);
        request.Headers.Add("Authorization", $"SAPISIDHASH {unixSeconds}_{hashHex}");
    }

    private void WriteContextObject(Utf8JsonWriter writer, BootstrapInfo bootstrap)
    {
        writer.WriteStartObject(); // context
        writer.WriteStartObject("client"); // client
        writer.WriteString("hl", Options.Language);
        writer.WriteString("gl", Options.Region);
        writer.WriteString("clientName", "WEB");
        writer.WriteString("clientVersion", bootstrap.ClientVersion);

        if (!string.IsNullOrEmpty(bootstrap.VisitorData)) writer.WriteString("visitorData", bootstrap.VisitorData);

        if (!string.IsNullOrEmpty(Options.RolloutToken)) writer.WriteString("rolloutToken", Options.RolloutToken);

        writer.WriteNumber("timeZoneUtcOffsetMinutes", 0);

        if (!string.IsNullOrEmpty(Options.ProofOfOriginToken))
        {
            writer.WriteStartObject("serviceIntegrityDimensions");
            writer.WriteString("poToken", Options.ProofOfOriginToken);
            writer.WriteEndObject();
        }

        writer.WriteEndObject(); // client

        writer.WriteStartObject("user");
        writer.WriteBoolean("lockedSafetyMode", false);
        writer.WriteEndObject();

        writer.WriteEndObject(); // context
    }

    private void CheckResponseAlerts(JsonElement root, string endpoint)
    {
        if (!root.TryGetProperty("alerts", out var alerts) || alerts.ValueKind != JsonValueKind.Array) return;
        foreach (var alert in alerts.EnumerateArray())
            if (alert.TryGetProperty("alertRenderer", out var alertRenderer))
            {
                var type = alertRenderer.TryGetProperty("type", out var typeEl) ? typeEl.GetString() ?? "" : "";
                var text = Sanitize(alertRenderer.GetText());

                if (!type.Equals("ERROR", StringComparison.OrdinalIgnoreCase)) continue;
                if (text.Contains("comments are turned off", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("comments are disabled", StringComparison.OrdinalIgnoreCase))
                    throw new CommentsUnavailableException(text);

                if (text.Contains("sign in", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("login", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("session expired", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("logged out", StringComparison.OrdinalIgnoreCase))
                {
                    if (Options.Authentication != null) throw new AuthenticationExpiredException(text);

                    throw new AuthenticationRequiredException(text);
                }

                if (text.Contains("permission", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("forbidden", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("not authorized", StringComparison.OrdinalIgnoreCase))
                    throw new PermissionDeniedException(text);

                if (text.Contains("private", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("unavailable", StringComparison.OrdinalIgnoreCase))
                    throw new ResourceUnavailableException(text);

                if (text.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
                    throw new ResourceNotFoundException(text);

                throw new YouTubeRequestException(
                    $"YouTube returned error alert from '{endpoint}': {Sanitize(text)}", endpoint);
            }
    }

    public static string Sanitize(string message)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        return message
            .Replace("SAPISIDHASH ", "SAPISIDHASH [REDACTED] ", StringComparison.OrdinalIgnoreCase)
            .Replace("Authorization:", "Authorization: [REDACTED]", StringComparison.OrdinalIgnoreCase)
            .Replace("Cookie:", "Cookie: [REDACTED]", StringComparison.OrdinalIgnoreCase)
            .Replace("key=", "key=[REDACTED]", StringComparison.OrdinalIgnoreCase)
            .Replace("continuation=", "continuation=[REDACTED]", StringComparison.OrdinalIgnoreCase)
            .Replace("params=", "params=[REDACTED]", StringComparison.OrdinalIgnoreCase)
            .Replace("poToken=", "poToken=[REDACTED]", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex("\"(?:channelId|externalId)\"\\s*:\\s*\"(?<id>UC[A-Za-z0-9_-]{22})\"",
        RegexOptions.CultureInvariant)]
    private static partial Regex ChannelIdRegex();

    public async Task<ChannelId> ResolveChannelIdAsync(ChannelReference reference, CancellationToken cancellationToken)
    {
        if (reference.Value.StartsWith("UC", StringComparison.Ordinal) &&
            ChannelId.TryParse(reference.Value, out var channelId))
            return channelId;

        var path = reference.Value.StartsWith('@')
            ? $"/{Uri.EscapeDataString(reference.Value)}"
            : $"/c/{Uri.EscapeDataString(reference.Value)}";
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"{BaseYouTubeUrl}{path}?hl={Uri.EscapeDataString(Options.Language)}");
        request.Headers.UserAgent.ParseAdd(DefaultUserAgent);
        request.Headers.Add("Accept-Language", $"{Options.Language}-{Options.Region}");
        request.Headers.Add("Cookie", BuildCookieHeader(request.RequestUri!));

        using var response = await HttpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new ResourceNotFoundException($"Channel '{reference.Value}' could not be resolved.");

        var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var match = ChannelIdRegex().Match(html);
        if (!match.Success || !ChannelId.TryParse(match.Groups["id"].Value, out var resolved))
            throw new ResourceNotFoundException($"Channel '{reference.Value}' could not be resolved.");

        return resolved;
    }

    private string BuildCookieHeader(Uri requestUri)
    {
        var builder = new StringBuilder();
        var hasSocs = false;
        var now = Options.TimeProvider.GetUtcNow();

        if (Options.Authentication != null)
            foreach (var cookie in Options.Authentication.InternalCookies)
            {
                if (IsExpired(cookie, now))
                    continue;

                if (!SecureMatches(requestUri, cookie))
                    continue;

                if (!DomainMatches(requestUri.Host, cookie.Domain))
                    continue;

                if (!PathMatches(requestUri.AbsolutePath, cookie.Path))
                    continue;

                if (cookie.Name.Equals("SOCS", StringComparison.OrdinalIgnoreCase))
                    hasSocs = true;

                if (builder.Length > 0)
                    builder.Append("; ");

                builder.Append(cookie.Name).Append('=').Append(cookie.Value);
            }

        if (hasSocs) return builder.ToString();
        if (builder.Length > 0)
            builder.Append("; ");
        builder.Append("SOCS=CAI");

        return builder.ToString();
    }

    private static bool IsExpired(Cookie cookie, DateTimeOffset now)
    {
        if (cookie.Expires == DateTime.MinValue)
            return false;

        return cookie.Expires < now.UtcDateTime;
    }

    private static bool SecureMatches(Uri requestUri, Cookie cookie)
    {
        return !cookie.Secure || requestUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    private static bool DomainMatches(string requestHost, string? cookieDomain)
    {
        if (string.IsNullOrEmpty(cookieDomain))
            return true;

        var domain = cookieDomain.StartsWith('.') ? cookieDomain[1..] : cookieDomain;
        if (string.IsNullOrEmpty(domain))
            return true;


        return requestHost.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
               requestHost.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathMatches(string requestPath, string? cookiePath)
    {
        if (string.IsNullOrEmpty(cookiePath) || cookiePath == "/")
            return true;

        if (string.IsNullOrEmpty(requestPath))
            requestPath = "/";


        return requestPath.StartsWith(cookiePath, StringComparison.Ordinal) &&
               (cookiePath.EndsWith('/') ||
                (requestPath.Length > cookiePath.Length && requestPath[cookiePath.Length] == '/'));
    }
}

internal sealed record BootstrapInfo(string ApiKey, string ClientVersion, string? VisitorData);