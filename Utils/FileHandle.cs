using System;
using System.IO;
using System.Text;

namespace PCL.Core.Utils;

public sealed class FileHandle : IDisposable
{
    private bool _disposed = false;
    private readonly FileStream _stream;
    private Action? _releaseCallback;

    public readonly string FilePath;

    public FileHandle(string filePath, FileStream stream, Action? releaseCallback)
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _releaseCallback = releaseCallback;
    }

    public FileStream UnderlyingStream => !_disposed ? _stream : throw new ObjectDisposedException(nameof(FileHandle));

    public byte[] ReadAllBytes()
    {
        CheckDisposed();
        _stream.Seek(0, SeekOrigin.Begin);
        using var ms = new MemoryStream();
        _stream.CopyTo(ms);
        return ms.ToArray();
    }

    public string ReadAllText(Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        CheckDisposed();
        _stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_stream, encoding, true, 1024, true);
        return reader.ReadToEnd();
    }

    public void WriteAllBytes(byte[] data)
    {
        CheckDisposed();
        _stream.SetLength(0);
        _stream.Write(data, 0, data.Length);
        _stream.Flush();
    }

    public void WriteAllText(string data, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        CheckDisposed();
        _stream.SetLength(0);
        byte[] bytes = encoding.GetBytes(data);
        _stream.Write(bytes, 0, bytes.Length);
    }

    private void CheckDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FileHandle));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _stream.Dispose();
        _releaseCallback?.Invoke();
        _releaseCallback = null;
    }
}