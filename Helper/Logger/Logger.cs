using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Xml;

namespace PCL.Core.Helper.Logger;

public sealed class Logger : IDisposable
{
    public Logger(LoggerConfiguration configuration)
    {
        _configuration = configuration;
        CreateNewFile();
        Task.Run(() => ProcessLogQueue(_cts.Token));
    }

    private readonly LoggerConfiguration _configuration;
    private StreamWriter? _currentStream;
    private FileStream? _currentFile;
    private int _fileIndex = 0;
    
    private readonly ConcurrentQueue<string> _logQueue = new();
    private readonly ManualResetEventSlim _logEvent = new(false);
    private readonly CancellationTokenSource _cts = new();

    private void CreateNewFile()
    {
        var nameFormat = (_configuration.FileNameFormat ?? $"{DateTime.Now:yyyy-M-d}-{{0}}") + ".log";
        string filename;
        string filePath;
        do
        {
            filename = nameFormat.Replace("{0}", _fileIndex++.ToString());
            filePath = Path.Combine(_configuration.StoreFolder, filename);
            if (_fileIndex >= int.MaxValue)
                throw new Exception("WTF are you doing!!!");
        } while (File.Exists(filePath));
        var lastWriter = _currentStream;
        var lastFile = _currentFile;
        Directory.CreateDirectory(_configuration.StoreFolder);
        _currentFile = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        _currentStream = new StreamWriter(_currentFile);
        lastWriter?.Dispose();
        lastFile?.Dispose();
        Task.Run(() =>
        {
            var logFiles = Directory.GetFiles(_configuration.StoreFolder);
            var needToDelete = logFiles.Select(x => new FileInfo(x))
                .OrderBy(x => x.CreationTime)
                .Take(logFiles.Length - _configuration.MaxKeepOldFile)
                .ToList();
            foreach (var logFile in needToDelete)
                logFile.Delete();
        });
    }

    public void Test(string message) => Log($"[{GetTimeFormatted()}] [TEST] {message}");
    public void Debug(string message) => Log($"[{GetTimeFormatted()}] [DEBUG] {message}");
    public void Info(string message) => Log($"[{GetTimeFormatted()}] [INFO] {message}");
    public void Warn(string message) => Log($"[{GetTimeFormatted()}] [WARN] {message}");
    public void Error(string message) => Log($"[{GetTimeFormatted()}] [ERROR] {message}");
    private static string GetTimeFormatted() => $"{DateTime.Now:HH:mm:ss.fff}";
    public void Log(string message)
    {
        if (_disposed) return;
        _logQueue.Enqueue(message);
        _logEvent.Set();
    }

    private void ProcessLogQueue(CancellationToken token)
    {
        const int maxBatchCount = 50;
        var batch = new StringBuilder();
        long currentBatchCount = 0;
        while (!token.IsCancellationRequested)
        {
            try
            {
                _logEvent.Wait(token);
                while (_logQueue.TryDequeue(out var message))
                {
                    batch.AppendLine(message);
                    Console.WriteLine(message);
                    currentBatchCount++;
                    if (currentBatchCount >= maxBatchCount || _logQueue.IsEmpty)
                    {
                        DoWrite(batch.ToString());
                        batch.Clear();
                        currentBatchCount = 0;
                    }
                }
            }
            catch (OperationCanceledException) {}
            catch (Exception e)
            {
                // 出错了先干到标准输出流中吧 Orz
                Console.WriteLine($"[{GetTimeFormatted()}] [ERROR] An error occured while processing log queue: {e.Message}");
                throw;
            }
            finally
            {
                batch.Clear();
                currentBatchCount = 0;
            }
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
        while (!_logQueue.IsEmpty) //等待日志写入完成
        {
            Thread.Sleep(100);
        }
        _cts.Cancel();
        _logEvent.Dispose();
        _currentStream?.Dispose();
    }
}