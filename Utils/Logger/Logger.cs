using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Logger;

public sealed class Logger : IDisposable
{
    public Logger(LoggerConfiguration configuration)
    {
        _configuration = configuration;
        CreateNewFile();
        _processingThread = new Thread(() => ProcessLogQueue(_cts.Token));
        _processingThread.Start();
    }

    private readonly LoggerConfiguration _configuration;
    private StreamWriter? _currentStream;
    private FileStream? _currentFile;
    private List<string> _files = [];
    
    private readonly Thread _processingThread;
    private readonly ConcurrentQueue<string> _logQueue = new();
    private readonly ManualResetEventSlim _logEvent = new(false);
    private readonly CancellationTokenSource _cts = new();

    public List<string> LogFiles => [.._files];
    
    private void CreateNewFile()
    {
        var nameFormat = (_configuration.FileNameFormat ?? $"Launch-{DateTime.Now:yyyy-M-d}-{{0}}") + ".log";
        string filename = nameFormat.Replace("{0}", $"{DateTime.Now:HHmmssfff}");
        string filePath = Path.Combine(_configuration.StoreFolder, filename);
        _files.Add(filePath);
        var lastWriter = _currentStream;
        var lastFile = _currentFile;
        Directory.CreateDirectory(_configuration.StoreFolder);
        _currentFile = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        _currentStream = new StreamWriter(_currentFile);
        Task.Run(() =>
        {
            lastWriter?.Close();
            lastWriter?.Dispose();
            lastFile?.Close();
            lastFile?.Dispose();
            if (!_configuration.AutoDeleteOldFile)
                return;
            var logFiles = Directory.GetFiles(_configuration.StoreFolder);
            var needToDelete = logFiles.Select(x => new FileInfo(x))
                .OrderBy(x => x.CreationTime)
                .Take(logFiles.Length - _configuration.MaxKeepOldFile)
                .ToList();
            foreach (var logFile in needToDelete)
                logFile.Delete();
        });
    }

    public void Trace(string message) => Log($"[{GetTimeFormatted()}] [TRA] {message}");
    public void Debug(string message) => Log($"[{GetTimeFormatted()}] [DBG] {message}");
    public void Info(string message) => Log($"[{GetTimeFormatted()}] [INFO] {message}");
    public void Warn(string message) => Log($"[{GetTimeFormatted()}] [WARN] {message}");
    public void Error(string message) => Log($"[{GetTimeFormatted()}] [ERR!] {message}");
    public void Fatal(string message) => Log($"[{GetTimeFormatted()}] [FTL!] {message}");
    
    private static string GetTimeFormatted() => $"{DateTime.Now:HH:mm:ss.fff}";
    
    public void Log(string message)
    {
        if (_disposed) return;
        _logQueue.Enqueue(message);
        _logEvent.Set();
    }
    
    private static readonly Regex PatternNewLine = new(@"\r\n|\n|\r");

    private void ProcessLogQueue(CancellationToken token)
    {
        const int maxBatchCount = 100;
        try
        {
            StringBuilder batch = new();
            long currentBatchCount = 0;
            while (true) // 循环一次写入一次日志
            {
                while (true) // 循环一次从队列里拿一条待打印的日志
                {
                    _logEvent.Wait(millisecondsTimeout: 600);
                    if (!_logQueue.TryDequeue(out var message))
                    {
                        // 日志队列为空时
                        if (currentBatchCount != 0) // 有待写入的日志 => 写入一次
                            break;
                        _logEvent.Reset();
                        if (token.IsCancellationRequested) // 已被 Dispose => 结束运行
                            throw new OperationCanceledException();
                        continue; // 否则 => 接着等待下一次 Log() 调用
                    }
#if DEBUG
                    message = PatternNewLine.Replace(message, "\r\n");
                    Console.WriteLine(message);
#endif
                    batch.AppendLine(message);
                    if (++currentBatchCount >= maxBatchCount) // 行数达到缓冲上限 => 写入一次
                        break;
                }
                DoWrite(batch.ToString());
                batch.Clear();
                currentBatchCount = 0;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            // 出错了先干到标准输出流中吧 Orz
            Console.WriteLine($"[{GetTimeFormatted()}] [ERROR] An error occured while processing log queue: {e.Message}");
            throw;
        }
    }

    private void DoWrite(string ctx)
    {
        try
        {
            if (_configuration.SegmentMode == LoggerSegmentMode.BySize && _currentFile?.Length >= _configuration.MaxFileSize)
            {
                CreateNewFile();
            }
            _currentStream?.Write(ctx);
            _currentStream?.Flush();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[{GetTimeFormatted()}] [ERROR] An error occured while writing log file: {e.Message}");
            throw;
        }
    }

    private bool _disposed;
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _logEvent.Set();
        _processingThread.Join();
        _logEvent.Dispose();
        _currentStream?.Dispose();
        _currentFile?.Dispose();
    }
}