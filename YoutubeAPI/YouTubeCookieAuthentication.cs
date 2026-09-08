using System.Globalization;
using System.Net;
using System.Text;
using YoutubeAPI.Exceptions;

namespace YoutubeAPI;

/// <summary>
///     Represents immutable authentication credentials constructed from YouTube cookies.
/// </summary>
public sealed class YouTubeCookieAuthentication
{
    private readonly Cookie[] _cookies;

    private YouTubeCookieAuthentication(Cookie[] cookies)
    {
        _cookies = cookies;
    }

    /// <summary>
    ///     Gets the collection of parsed cookies as defensive clones. Authentication cookies are retained
    ///     even when their exported browser expiration timestamp has passed; YouTube decides whether the
    ///     credential itself is still valid.
    /// </summary>
    public IReadOnlyList<Cookie> Cookies
    {
        get
        {
            var clones = new Cookie[_cookies.Length];
            for (var i = 0; i < _cookies.Length; i++) clones[i] = CloneCookie(_cookies[i]);
            return Array.AsReadOnly(clones);
        }
    }

    /// <summary>
    ///     Gets whether the cookie collection contains the required authentication cookies
    ///     (<c>SAPISID</c> or <c>__Secure-3PAPISID</c>).
    /// </summary>
    public bool HasAuthenticationCookies => !string.IsNullOrEmpty(Sapisid) || !string.IsNullOrEmpty(Secure3Papisid);

    internal IReadOnlyList<Cookie> InternalCookies => _cookies;

    internal string? Sapisid =>
        FindAuthenticationCookie("SAPISID")?.Value;

    internal string? Secure3Papisid =>
        FindAuthenticationCookie("__Secure-3PAPISID")?.Value;

    private Cookie? FindAuthenticationCookie(string name)
    {
        return _cookies.FirstOrDefault(c =>
            c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(c.Value) &&
            IsYouTubeDomain(c.Domain));
    }

    private static bool IsYouTubeDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return false;

        var normalized = domain.TrimStart('.');
        return normalized.Equals("youtube.com", StringComparison.OrdinalIgnoreCase) ||
               normalized.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase);
    }

    internal string? GetSapisidOrSecure()
    {
        return Sapisid ?? Secure3Papisid;
    }

    internal string GetRequiredSapisid()
    {
        var sapisid = GetSapisidOrSecure();
        if (string.IsNullOrEmpty(sapisid))
            throw new AuthenticationRequiredException(
                "Authentication cookie 'SAPISID' or '__Secure-3PAPISID' is required for this operation.");
        return sapisid;
    }

    /// <summary>
    ///     Creates authentication credentials by cloning an existing collection of cookies.
    /// </summary>
    /// <param name="cookies">The cookies to clone.</param>
    /// <returns>A new <see cref="YouTubeCookieAuthentication" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cookies" /> is null.</exception>
    public static YouTubeCookieAuthentication FromCookies(IEnumerable<Cookie?> cookies)
    {
        ArgumentNullException.ThrowIfNull(cookies);
        var now = DateTime.UtcNow;
        return new YouTubeCookieAuthentication(
        [
            .. cookies.Where(c => c is not null &&
                                  (c.Expires == DateTime.MinValue ||
                                   c.Expires >= now ||
                                   IsAuthenticationCookie(c.Name))).Select(c => CloneCookie(c!))
        ]);
    }

    /// <summary>
    ///     Creates authentication credentials from Netscape-formatted cookie file text.
    /// </summary>
    /// <param name="netscapeContent">The string content formatted as Netscape cookies.</param>
    /// <returns>A new <see cref="YouTubeCookieAuthentication" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="netscapeContent" /> is null.</exception>
    /// <exception cref="FormatException">Thrown when a non-comment line is malformed.</exception>
    public static YouTubeCookieAuthentication FromNetscape(string netscapeContent)
    {
        ArgumentNullException.ThrowIfNull(netscapeContent);
        using var reader = new StringReader(netscapeContent);
        var cookies = ParseNetscape(reader);
        return new YouTubeCookieAuthentication([.. cookies]);
    }

    /// <summary>
    ///     Creates authentication credentials by reading Netscape-formatted cookie rows eagerly from a stream.
    /// </summary>
    /// <param name="stream">The input stream to read from.</param>
    /// <returns>A new <see cref="YouTubeCookieAuthentication" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream" /> is null.</exception>
    /// <exception cref="FormatException">Thrown when a non-comment line is malformed.</exception>
    public static YouTubeCookieAuthentication FromNetscape(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var cookies = ParseNetscape(reader);
        return new YouTubeCookieAuthentication([.. cookies]);
    }

    private static bool IsAuthenticationCookie(string name)
    {
        return name.Equals("SAPISID", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("__Secure-3PAPISID", StringComparison.OrdinalIgnoreCase);
    }

    private static List<Cookie> ParseNetscape(TextReader reader)
    {
        var list = new List<Cookie>();
        var lineNumber = 0;
        var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            var httpOnly = false;
            if (trimmed.StartsWith("#HttpOnly_", StringComparison.OrdinalIgnoreCase))
            {
                httpOnly = true;
                trimmed = trimmed["#HttpOnly_".Length..];
            }
            else if (trimmed.StartsWith('#'))
            {
                continue;
            }

            var parts = trimmed.Split('\t');
            if (parts.Length != 7)
            {
                var wsParts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (wsParts.Length == 7)
                    parts = wsParts;
                else
                    throw new FormatException(
                        $"Malformed Netscape cookie row at line {lineNumber}. Expected 7 fields, found {parts.Length}.");
            }

            var domain = parts[0];
            var path = parts[2];

            bool secure;
            if (parts[3].Equals("TRUE", StringComparison.OrdinalIgnoreCase))
                secure = true;
            else if (parts[3].Equals("FALSE", StringComparison.OrdinalIgnoreCase))
                secure = false;
            else
                throw new FormatException($"Malformed Netscape secure flag at line {lineNumber}: '{parts[3]}'.");

            if (!long.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var expiryUnix))
                throw new FormatException($"Malformed Netscape cookie expiry at line {lineNumber}: '{parts[4]}'.");

            var name = parts[5];
            var value = parts[6];

            // Auth tokens are opaque credentials; browser expiry metadata is not authoritative for them.
            if (expiryUnix > 0 && expiryUnix < nowUnix && !IsAuthenticationCookie(name)) continue;

            try
            {
                var cookie = new Cookie(name, value, path, domain)
                {
                    Secure = secure,
                    HttpOnly = httpOnly
                };

                if (expiryUnix > 0) cookie.Expires = DateTimeOffset.FromUnixTimeSeconds(expiryUnix).UtcDateTime;

                list.Add(cookie);
            }
            catch (Exception ex)
            {
                throw new FormatException($"Malformed Netscape cookie at line {lineNumber}: {ex.Message}", ex);
            }
        }

        return list;
    }

    private static Cookie CloneCookie(Cookie c)
    {
        return new Cookie(c.Name, c.Value, c.Path, c.Domain)
        {
            Secure = c.Secure,
            HttpOnly = c.HttpOnly,
            Expires = c.Expires,
            Discard = c.Discard,
            Comment = c.Comment,
            CommentUri = c.CommentUri,
            Version = c.Version
        };
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return "[Redacted]";
    }
}