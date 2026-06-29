namespace Fca.Mobile.Services;

/// <summary>
/// FCA academy media — same blob CDN contract as fca-bid-tracker SWA
/// (<c>academyMediaUrl.js</c> / <c>FCA_ACADEMY_MEDIA_CDN</c>).
/// </summary>
public sealed class FcaMediaConfig
{
    public const string DefaultAcademyCdnBase =
        "https://auricruxartifacts10046.blob.core.windows.net/fca-academy-media";

    public string AcademyMediaCdnBase { get; init; } = DefaultAcademyCdnBase;

    public static FcaMediaConfig Current { get; } = new();

    public string ResolveAcademyMediaUrl(string? pathOrUrl)
    {
        if (string.IsNullOrWhiteSpace(pathOrUrl))
            return pathOrUrl ?? string.Empty;
        if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return pathOrUrl;

        var cdn = AcademyMediaCdnBase.TrimEnd('/');
        var rel = pathOrUrl.StartsWith('/') ? pathOrUrl[1..] : pathOrUrl;
        return $"{cdn}/{rel}";
    }
}
