namespace Dashboard.Services;

/// <summary>
/// Sends push notifications via the Ting endpoint on api.tzer0m.co.uk.
/// </summary>
internal static class TingClient
{
    /// <summary>
    /// Sends a Ting notification with the given title and body.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to send the request.</param>
    /// <param name="apiKey">The API key for the Ting endpoint.</param>
    /// <param name="logger">Logger used to log failures.</param>
    /// <param name="title">The notification title.</param>
    /// <param name="body">The notification body.</param>
    public static async Task SendAsync(HttpClient httpClient, string apiKey, ILogger logger, string title, string body)
    {
        try
        {
            string url = $"https://api.tzer0m.co.uk/Ting?title={Uri.EscapeDataString(title)}&body={Uri.EscapeDataString(body)}";
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", apiKey);

            HttpResponseMessage response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Ting notification failed with status {StatusCode}", (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Ting notification failed: {Message}", ex.Message);
        }
    }
}