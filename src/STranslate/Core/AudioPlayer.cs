using NAudio.Wave;
using Microsoft.Extensions.Logging;
using STranslate.Plugin;
using System.IO;

namespace STranslate.Core;

public class AudioPlayer : IAudioPlayer
{
    private readonly ILogger<AudioPlayer> _logger;
    private readonly IHttpService _httpService;
    private WaveOutEvent? _waveOut;
    private MemoryStream? _audioStream;
    private Mp3FileReader? _mp3Reader;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    public AudioPlayer(ILogger<AudioPlayer> logger, IHttpService httpService)
    {
        _logger = logger;
        _httpService = httpService;
    }

    /// <summary>
    /// 当前是否正在播放
    /// </summary>
    public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;

    /// <summary>
    /// 播放状态改变事件
    /// </summary>
    public event EventHandler<PlaybackState>? PlaybackStateChanged;

    /// <summary>
    /// 播放进度事件
    /// </summary>
    public event EventHandler<TimeSpan>? PlaybackPositionChanged;

    public async Task PlayAsync(byte[]? audioData, CancellationToken cancellationToken = default)
    {
        if (audioData == null || audioData.Length == 0)
        {
            _logger.LogWarning("音频数据为空");
            return;
        }
        try
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            // 创建内存流
            _audioStream = new MemoryStream(audioData);
            // 创建MP3读取器
            _mp3Reader = new Mp3FileReader(_audioStream);
            // 创建播放设备
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_mp3Reader);
            // 注册事件
            _waveOut.PlaybackStopped += OnPlaybackStopped;
            // 开始播放
            _waveOut.Play();
            OnPlaybackStateChanged(PlaybackState.Playing);
            _logger.LogInformation("音频播放开始");
            // 启动进度监控
            await MonitorPlaybackProgressAsync(_cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogTrace("音频播放被取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "播放音频时发生错误");
            await StopAsync();
            throw;
        }
    }

    public async Task PlayAsync(string audioUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            _logger.LogWarning("音频URL为空");
            return;
        }

        byte[]? audioData = null;
        try
        {
            _logger.LogInformation("开始下载音频: {AudioUrl}", audioUrl);

            audioData = await _httpService.GetAsBytesAsync(audioUrl, cancellationToken);

            _logger.LogInformation("音频下载完成: {Length} bytes", audioData?.Length ?? 0);
        }
        catch (OperationCanceledException)
        {
            _logger.LogTrace("音频下载被取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载音频时发生错误: {AudioUrl}", audioUrl);
        }

        await PlayAsync(audioData, cancellationToken);
    }

    /// <summary>
    /// 停止播放
    /// </summary>
    public async Task StopAsync()
    {
        try
        {
            _cancellationTokenSource?.Cancel();

            if (_waveOut != null)
            {
                _waveOut.PlaybackStopped -= OnPlaybackStopped;
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }

            _mp3Reader?.Dispose();
            _mp3Reader = null;

            _audioStream?.Dispose();
            _audioStream = null;

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            OnPlaybackStateChanged(PlaybackState.Stopped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止音频播放时发生错误");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 暂停播放
    /// </summary>
    public void Pause()
    {
        if (_waveOut?.PlaybackState == PlaybackState.Playing)
        {
            _waveOut.Pause();
            OnPlaybackStateChanged(PlaybackState.Paused);
            _logger.LogTrace("音频播放已暂停");
        }
    }

    /// <summary>
    /// 恢复播放
    /// </summary>
    public void Resume()
    {
        if (_waveOut?.PlaybackState == PlaybackState.Paused)
        {
            _waveOut.Play();
            OnPlaybackStateChanged(PlaybackState.Playing);
            _logger.LogTrace("音频播放已恢复");
        }
    }

    /// <summary>
    /// 监控播放进度
    /// </summary>
    private async Task MonitorPlaybackProgressAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested &&
                   _mp3Reader != null &&
                   _waveOut?.PlaybackState == PlaybackState.Playing)
            {
                var currentPosition = _mp3Reader.CurrentTime;
                PlaybackPositionChanged?.Invoke(this, currentPosition);

                await Task.Delay(100, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogTrace("音频播放被取消(Monitor)");
            await StopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "监控播放进度时发生错误");
        }
    }

    /// <summary>
    /// 播放停止事件处理
    /// </summary>
    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        OnPlaybackStateChanged(PlaybackState.Stopped);

        if (e.Exception != null)
        {
            _logger.LogError(e.Exception, "音频播放意外停止");
        }
        else
        {
            _logger.LogTrace("音频播放正常结束");
        }

        // 清理资源
        _ = Task.Run(async () => await StopAsync());
    }

    /// <summary>
    /// 触发播放状态改变事件
    /// </summary>
    private void OnPlaybackStateChanged(PlaybackState state)
    {
        PlaybackStateChanged?.Invoke(this, state);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _ = Task.Run(async () => await StopAsync());
            _disposed = true;
        }
    }
}