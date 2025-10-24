using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.Logging;
using PCL.Core.Utils;

namespace PCL.Core.Net;

public static class ModApiMirrorSourceReq
{
    /// <summary>
    /// Send HTTP GET Request with ModApiMirrorSource to get JSON result.
    /// </summary>
    /// <typeparam name="TResultJson">Target JSON DTO.</typeparam>
    /// <param name="url">Target request URL.</param>
    /// <param name="content">Send content.</param>
    /// <param name="method">HTTP method.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Converted JSON DTO</returns>
    /// <exception cref="ArgumentNullException">Throw if mirror link not exist.</exception>
    public static async Task<TResultJson?> RequestAsync<TResultJson>(
        string url, string? content = null,
        HttpMethod? method = null,
        CancellationToken? ct = null)
    {
        var cancellationToken = ct ?? CancellationToken.None;
        var dto = await _RequestHelperAsync(url, content, method, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(dto))
        {
            return default;
        }

        try
        {
            var result = JsonSerializer.Deserialize<TResultJson>(dto);
            return result;
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "Failed to deserialize JSON response.");
            return default;
        }
    }

    /// <summary>
    /// Send HTTP GET Request with ModApiMirrorSource to get <see cref="string"/> result.
    /// </summary>
    /// <param name="url">Target request URL.</param>
    /// <param name="content">Send content.</param>
    /// <param name="method">HTTP method.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>String content.</returns>
    /// <exception cref="ArgumentNullException">Throw if mirror link not exist.</exception>
    public static Task<string> RequestAsync(string url, string? content = null, HttpMethod? method = null,
        CancellationToken? ct = null)
    {
        var cancellationToken = ct ?? CancellationToken.None;
        return _RequestHelperAsync(url, content, method, cancellationToken);
    }

    private static async Task<string> _RequestHelperAsync(
        string url, string? content = null,
        HttpMethod? method = null,
        CancellationToken? ct = null)
    {
        List<KeyValuePair<string, int>>? urls = null;
        var mirrorUrl = ResourceUrlConverter.ModApiToMirror(url);
        if (!mirrorUrl.Equals(url, StringComparison.OrdinalIgnoreCase))
        {
            urls = Config.Tool.Download.CompSourceSolution switch
            {
                0 =>
                [
                    new KeyValuePair<string, int>(mirrorUrl, 5), new KeyValuePair<string, int>(mirrorUrl, 10),
                    new KeyValuePair<string, int>(url, 15)
                ],
                1 =>
                [
                    new KeyValuePair<string, int>(url, 5), new KeyValuePair<string, int>(mirrorUrl, 5),
                    new KeyValuePair<string, int>(url, 15), new KeyValuePair<string, int>(mirrorUrl, 10),
                ],
                _ =>
                [
                    new KeyValuePair<string, int>(url, 5), new KeyValuePair<string, int>(url, 10),
                    new KeyValuePair<string, int>(url, 15)
                ]
            };
        }

        if (urls is null)
        {
            throw new ArgumentNullException(nameof(url), "URL cannot be empty.");
        }

        foreach (var (key, value) in urls)
        {
            var httpReq = HttpRequestBuilder.Create(key, method ?? HttpMethod.Get);

            if (content is not null)
            {
                httpReq.WithContent(content);
            }

            var response =
                await httpReq.SendAsync().ConfigureAwait(false); // TODO: use timeout, need refactor HttpRequestBuilder

            if (!response.IsSuccess)
            {
                //throw new HttpRequestException("Failed to send HTTP request.");
                continue;
            }

            var repContent = await response.AsStringAsync().ConfigureAwait(false);

            return repContent;
        }

        return string.Empty;
    }
}