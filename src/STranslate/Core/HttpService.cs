using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using STranslate.Helpers;
using STranslate.Plugin;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Web;

namespace STranslate.Core;

public class HttpService : IHttpService
{
    private static readonly Settings _settings = Ioc.Default.GetRequiredService<Settings>();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpService(IHttpClientFactory httpClientFactory, ILogger<HttpService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    #region GET 请求

    public async Task<string> GetAsync(string url, CancellationToken cancellationToken = default)
    {
        return await GetAsync(Constant.HttpClientName, url, null, cancellationToken);
    }

    public async Task<string> GetAsync(string url, Options? options = null, CancellationToken cancellationToken = default)
    {
        return await GetAsync(Constant.HttpClientName, url, options, cancellationToken);
    }

    public async Task<T?> GetAsync<T>(string url, Options? options = null, CancellationToken cancellationToken = default)
    {
        var json = await GetAsync(url, options, cancellationToken);
        if (string.IsNullOrEmpty(json)) return default;
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    public async Task<byte[]> GetAsBytesAsync(string url, CancellationToken cancellationToken = default)
    {
        return await GetAsBytesAsync(Constant.HttpClientName, url, null, cancellationToken);
    }

    public async Task<byte[]> GetAsBytesAsync(string url, Options? options = null, CancellationToken cancellationToken = default)
    {
        return await GetAsBytesAsync(Constant.HttpClientName, url, options, cancellationToken);
    }
    public async Task<Stream> GetAsStreamAsync(string url, CancellationToken cancellationToken = default)
    {
        return await GetAsStreamAsync(Constant.HttpClientName, url, null, cancellationToken);
    }

    public async Task<Stream> GetAsStreamAsync(string url, Options? options = null, CancellationToken cancellationToken = default)
    {
        return await GetAsStreamAsync(Constant.HttpClientName, url, options, cancellationToken);
    }

    public async Task<string> GetAsync(string serviceName, string url, Options? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient(serviceName);
            ConfigureClientTimeout(client, options?.Timeout);

            var finalUrl = BuildUrlWithQuery(url, options?.QueryParams);
            using var request = new HttpRequestMessage(HttpMethod.Get, finalUrl);

            AddHeaders(request, options?.Headers);

            _logger?.LogTrace("Sending GET request to {Url} using service {ServiceName}", finalUrl, serviceName);

            using var response = await client.SendAsync(request, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response, cancellationToken);

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GET request failed for URL: {Url}, Service: {ServiceName}", url, serviceName);
            throw;
        }
    }

    public async Task<byte[]> GetAsBytesAsync(string serviceName, string url, Options? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient(serviceName);
            ConfigureClientTimeout(client, options?.Timeout);

            var finalUrl = BuildUrlWithQuery(url, options?.QueryParams);
            using var request = new HttpRequestMessage(HttpMethod.Get, finalUrl);

            AddHeaders(request, options?.Headers);

            _logger?.LogTrace("Sending GET request for bytes to {Url} using service {ServiceName}", finalUrl, serviceName);

            using var response = await client.SendAsync(request, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response, cancellationToken);

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GET request for bytes failed for URL: {Url}, Service: {ServiceName}", url, serviceName);
            throw;
        }
    }

    public async Task<Stream> GetAsStreamAsync(string serviceName, string url, Options? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(serviceName);
            ConfigureClientTimeout(client, options?.Timeout);

            var finalUrl = BuildUrlWithQuery(url, options?.QueryParams);
            var request = new HttpRequestMessage(HttpMethod.Get, finalUrl);

            AddHeaders(request, options?.Headers);

            _logger?.LogTrace("Sending GET stream request to {Url} using service {ServiceName}", finalUrl, serviceName);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response, cancellationToken);

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GET stream request failed for URL: {Url}, Service: {ServiceName}", url, serviceName);
            throw;
        }
    }

    #endregion

    #region POST 请求

    public async Task<string> PostAsync(string url, object content, Options? options = null, CancellationToken cancellationToken = default)
    {
        var json = content is string str ? str : JsonSerializer.Serialize(content, _jsonOptions);
        return await PostAsync(Constant.HttpClientName, url, json, options, cancellationToken);
    }

    public async Task<T?> PostAsync<T>(string url, object content, Options? options = null, CancellationToken cancellationToken = default)
    {
        var json = await PostAsync(url, content, options, cancellationToken);
        if (string.IsNullOrEmpty(json)) return default;
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    public async Task<byte[]> PostAsBytesAsync(string url, object content, Options? options = null, CancellationToken cancellationToken = default)
    {
        return await PostAsBytesAsync(Constant.HttpClientName, url, content, options, cancellationToken);
    }

    public async Task<string> PostAsync(string serviceName, string url, object content, Options? options = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(content, _jsonOptions);
        return await PostAsync(serviceName, url, json, options, cancellationToken);
    }

    private async Task<string> PostAsync(string serviceName, string url, string? content, Options? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient(serviceName);
            ConfigureClientTimeout(client, options?.Timeout);

            var finalUrl = BuildUrlWithQuery(url, options?.QueryParams);
            using var request = new HttpRequestMessage(HttpMethod.Post, finalUrl);
            if (content != null)
                request.Content = new StringContent(content, Encoding.UTF8, options?.ContentType ?? "application/json");

            AddHeaders(request, options?.Headers);

            _logger?.LogTrace("Sending POST request to {Url} using service {ServiceName}", url, serviceName);

            using var response = await client.SendAsync(request, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response, cancellationToken);

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "POST request failed for URL: {Url}, Service: {ServiceName}", url, serviceName);
            throw;
        }
    }

    public async Task<byte[]> PostAsBytesAsync(string serviceName, string url, object content, Options? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient(serviceName);
            ConfigureClientTimeout(client, options?.Timeout);

            var finalUrl = BuildUrlWithQuery(url, options?.QueryParams);
            using var request = new HttpRequestMessage(HttpMethod.Post, finalUrl);

            if (content is byte[] bytes)
            {
                request.Content = new ByteArrayContent(bytes);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(options?.ContentType ?? "application/octet-stream");
            }
            else
            {
                var json = content is string str ? str : JsonSerializer.Serialize(content, _jsonOptions);
                request.Content = new StringContent(json, Encoding.UTF8, options?.ContentType ?? "application/json");
            }

            AddHeaders(request, options?.Headers);

            _logger?.LogTrace("Sending POST request for bytes to {Url} using service {ServiceName}", url, serviceName);

            using var response = await client.SendAsync(request, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response, cancellationToken);

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "POST request for bytes failed for URL: {Url}, Service: {ServiceName}", url, serviceName);
            throw;
        }
    }

    #endregion

    #region Form 请求

    public async Task<string> PostFormAsync(string url, Dictionary<string, string> formData, Options? options = null, CancellationToken cancellationToken = default)
    {
        return await PostFormAsync(Constant.HttpClientName, url, formData, options, cancellationToken);
    }

    public async Task<string> PostFormAsync(string serviceName, string url, Dictionary<string, string> formData, Options? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient(serviceName);
            ConfigureClientTimeout(client, options?.Timeout);

            var finalUrl = BuildUrlWithQuery(url, options?.QueryParams);
            using var request = new HttpRequestMessage(HttpMethod.Post, finalUrl);
            request.Content = new FormUrlEncodedContent(formData);

            AddHeaders(request, options?.Headers);

            _logger?.LogTrace("Sending POST form request to {Url} using service {ServiceName}", url, serviceName);

            using var response = await client.SendAsync(request, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response, cancellationToken);

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "POST form request failed for URL: {Url}, Service: {ServiceName}", url, serviceName);
            throw;
        }
    }

    #endregion

    #region 流式请求

    public async Task StreamPostAsync(string url, object content, Action<string> onDataReceived, Options? options = null, CancellationToken cancellationToken = default)
    {
        await StreamPostAsync(Constant.HttpClientName, url, content, onDataReceived, options, cancellationToken);
    }

    public async Task StreamPostAsync(string serviceName, string url, object content, Action<string> onDataReceived, Options? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient(serviceName);
            ConfigureClientTimeout(client, options?.Timeout);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            var jsonContent = content is string str ? str : JsonSerializer.Serialize(content, _jsonOptions);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, options?.ContentType ?? "application/json");

            AddHeaders(request, options?.Headers);

            _logger?.LogTrace("Sending streaming POST request to {Url} using service {ServiceName}", url, serviceName);

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response, cancellationToken);

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            // 使用 ReadLineAsync 的返回值来判断流是否结束，而不是 EndOfStream 属性
            string? line;
            while (!cancellationToken.IsCancellationRequested && (line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    onDataReceived(line);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("Stream POST request was canceled for URL: {Url}, Service: {ServiceName}", url, serviceName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Stream POST request failed for URL: {Url}, Service: {ServiceName}", url, serviceName);
            throw;
        }
    }

    #endregion

    #region 文件下载

    public async Task<string> DownloadFileAsync(string url, string savePath, string fileName, Options? options = null, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        return await DownloadFileAsync(Constant.HttpClientName, url, savePath, fileName, options, progress, cancellationToken);
    }

    public async Task<string> DownloadFileAsync(string url, object content, string savePath, string fileName, Options? options = null, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        return await DownloadFileAsync(Constant.HttpClientName, url, content, savePath, fileName, options, progress, cancellationToken);
    }

    public async Task<string> DownloadFileAsync(string serviceName, string url, string savePath, string fileName, Options? options = null, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            var fullPath = Path.Combine(savePath, fileName);

            using var client = _httpClientFactory.CreateClient(serviceName);
            ConfigureClientTimeout(client, options?.Timeout);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddHeaders(request, options?.Headers);

            _logger?.LogTrace("Starting file download from {Url} to {Path} using service {ServiceName}", url, fullPath, serviceName);

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response, cancellationToken);

            var totalBytes = response.Content.Headers.ContentLength ?? -1;

            await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);
            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            if (totalBytes > 0 && progress != null)
            {
                await CopyToWithProgressAsync(contentStream, fileStream, totalBytes, progress, cancellationToken);
            }
            else
            {
                await contentStream.CopyToAsync(fileStream, cancellationToken);
            }

            _logger?.LogInformation("File downloaded successfully: {Path}", fullPath);
            return fullPath;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "File download failed for URL: {Url}, Service: {ServiceName}", url, serviceName);
            throw;
        }
    }

    public async Task<string> DownloadFileAsync(string serviceName, string url, object content, string savePath, string fileName, Options? options = null, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            var fullPath = Path.Combine(savePath, fileName);

            using var client = _httpClientFactory.CreateClient(serviceName);
            ConfigureClientTimeout(client, options?.Timeout);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            var jsonContent = content is string str ? str : JsonSerializer.Serialize(content, _jsonOptions);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, options?.ContentType ?? "application/json");

            AddHeaders(request, options?.Headers);

            _logger?.LogTrace("Starting POST file download from {Url} to {Path} using service {ServiceName}", url, fullPath, serviceName);

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response, cancellationToken);

            var totalBytes = response.Content.Headers.ContentLength ?? -1;

            await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);
            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            if (totalBytes > 0 && progress != null)
            {
                await CopyToWithProgressAsync(contentStream, fileStream, totalBytes, progress, cancellationToken);
            }
            else
            {
                await contentStream.CopyToAsync(fileStream, cancellationToken);
            }

            _logger?.LogInformation("POST file downloaded successfully: {Path}", fullPath);
            return fullPath;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "POST file download failed for URL: {Url}, Service: {ServiceName}", url, serviceName);
            throw;
        }
    }

    #endregion

    #region 代理测试

    /// <summary>
    /// 测试代理连接
    /// </summary>
    public async Task<bool> TestProxyAsync(CancellationToken cancellationToken = default) =>
        await ProxyHelper.TestProxyConnectionAsync(_settings.Proxy, cancellationToken);

    /// <summary>
    /// 获取当前IP（用于测试代理）
    /// </summary>
    public async Task<string> GetCurrentIpAsync(CancellationToken cancellationToken = default) =>
        await ProxyHelper.GetCurrentIpAsync(_settings.Proxy, cancellationToken);

    #endregion

    #region 私有方法

    private static void ConfigureClientTimeout(HttpClient client, TimeSpan? timeout)
    {
        // 如果传入了超时时间，则使用传入的值，否则使用软件配置超时时间
        if (timeout.HasValue)
        {
            client.Timeout = timeout.Value;
        }
        else
        {
            client.Timeout = TimeSpan.FromSeconds(_settings.HttpTimeout);
        }
    }

    private static void AddHeaders(HttpRequestMessage request, Dictionary<string, string>? headers)
    {
        if (headers == null) return;

        foreach (var header in headers)
        {
            // 对于一些特殊的头部，使用 TryAddWithoutValidation 避免验证问题
            if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("User-Agent", StringComparison.OrdinalIgnoreCase) ||
                header.Key.StartsWith("X-", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            else
            {
                try
                {
                    request.Headers.Add(header.Key, header.Value);
                }
                catch
                {
                    // 如果添加失败，尝试使用无验证的方式
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }
    }

    private static string BuildUrlWithQuery(string url, Dictionary<string, string>? queryParams)
    {
        if (queryParams == null || !queryParams.Any()) return url;

        var uriBuilder = new UriBuilder(url);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        foreach (var param in queryParams)
        {
            query[param.Key] = param.Value;
        }

        uriBuilder.Query = query.ToString();
        return uriBuilder.ToString();
    }

    private async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        var outerMsg = response.ReasonPhrase ?? "HTTP request failed";
        var innerMsg = string.Empty;

        try
        {
            innerMsg = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            // 忽略读取响应内容的异常
        }

        var errorMessage = string.IsNullOrEmpty(innerMsg)
            ? $"{outerMsg}: {response.StatusCode}"
            : $"{outerMsg}: {response.StatusCode}, Details: {innerMsg}";

        throw new HttpRequestException(errorMessage, null, response.StatusCode);
    }

    private static async Task CopyToWithProgressAsync(Stream source, Stream destination, long totalBytes,
        IProgress<DownloadProgress> progress, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        long totalBytesRead = 0;
        int bytesRead;
        var stopwatch = Stopwatch.StartNew();

        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalBytesRead += bytesRead;

            var downloadProgress = new DownloadProgress
            {
                TotalBytes = totalBytes,
                DownloadedBytes = totalBytesRead,
                ElapsedTime = stopwatch.Elapsed,
                Speed = stopwatch.Elapsed.TotalSeconds > 0 ? totalBytesRead / stopwatch.Elapsed.TotalSeconds : 0
            };

            progress.Report(downloadProgress);
        }
    }

    #endregion
}
