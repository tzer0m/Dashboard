using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;

namespace Dashboard.Pages;

/// <summary>
/// Proxy endpoint that fetches and returns the favicon for a given URL.
/// </summary>
public partial class FaviconModel(IHttpClientFactory httpClientFactory, ILogger<FaviconModel> logger, IMemoryCache memoryCache) : PageModel
{
    /// <summary>
    /// Gets the HTTP client used to fetch web content.
    /// </summary>
    private readonly HttpClient HttpClient = httpClientFactory.CreateClient();

    /// <summary>
    /// Logger for logging information and errors.
    /// </summary>
    private readonly ILogger<FaviconModel> Logger = logger;

    /// <summary>
    /// Memory cache for caching fetched favicons to improve performance and reduce repeated network requests.
    /// </summary>
    private readonly IMemoryCache MemoryCache = memoryCache;

    /// <summary>
    /// Removed the rel link from the HTML content to avoid duplicate favicon links.
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"<link[^>]*rel=[""'][^""']*icon[^""']*[""'][^>]*href=[""']([^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex IconRelFirst();

    /// <summary>
    /// Removes the href attribute from the rel link in the HTML content to avoid duplicate favicon links.
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"<link[^>]*href=[""']([^""']+)[""'][^>]*rel=[""'][^""']*icon[^""']*[""']", RegexOptions.IgnoreCase)]
    private static partial Regex IconHrefFirst();

    /// <summary>
    /// Fetches the favicon for the given URL, checking HTML meta tags then falling back to /favicon.ico.
    /// Caches the result for 24 hours.
    /// </summary>
    /// <param name="url">The service URL to fetch the favicon for.</param>
    public async Task<IActionResult> OnGetAsync(string url)
    {
        // Validate the URL parameter
        if (string.IsNullOrEmpty(url)) 
            return NotFound();

        // Check if the favicon is already cached
        string cacheKey = $"favicon:{url}";
        if (MemoryCache.TryGetValue(cacheKey, out (byte[] bytes, string contentType) cached))
            return File(cached.bytes, cached.contentType);

        // Fetch the favicon from the specified URL
        try
        {
            Uri baseUri = new(url);
            string origin = baseUri.GetLeftPart(UriPartial.Authority);

            // Fetch the HTML content of the page
            HttpResponseMessage htmlResponse = await HttpClient.GetAsync(url);
            if (htmlResponse.IsSuccessStatusCode)
            {
                // Read the HTML content as a string
                string html = await htmlResponse.Content.ReadAsStringAsync();
                Match match = IconRelFirst().Match(html);
                if (!match.Success) match = IconHrefFirst().Match(html);

                // If a favicon link is found in the HTML, fetch it; otherwise, fall back to /favicon.ico
                if (match.Success)
                {
                    string faviconPath = match.Groups[1].Value;
                    string faviconUrl = faviconPath.StartsWith("http") ? faviconPath : origin + "/" + faviconPath.TrimStart('/');
                    HttpResponseMessage faviconResponse = await HttpClient.GetAsync(faviconUrl);
                    if (faviconResponse.IsSuccessStatusCode)
                    {
                        byte[] bytes = await faviconResponse.Content.ReadAsByteArrayAsync();
                        string contentType = faviconResponse.Content.Headers.ContentType?.MediaType ?? "image/x-icon";
                        MemoryCache.Set(cacheKey, (bytes, contentType), TimeSpan.FromHours(24));
                        return File(bytes, contentType);
                    }
                }
            }

            // Fallback to /favicon.ico if no favicon link is found in the HTML
            HttpResponseMessage icoResponse = await HttpClient.GetAsync($"{origin}/favicon.ico");
            if (icoResponse.IsSuccessStatusCode)
            {
                byte[] bytes = await icoResponse.Content.ReadAsByteArrayAsync();
                MemoryCache.Set(cacheKey, (bytes, "image/x-icon"), TimeSpan.FromHours(24));
                return File(bytes, "image/x-icon");
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Failed to fetch favicon for {url}: {message}", url, ex.Message);
        }

        // If all attempts fail, return a 404 Not Found response
        return NotFound();
    }
}