using System;
using System.IO;
using System.Text;

namespace PCL.Core.Utils;

public sealed class FileHandle(string filePath, FileStream stream, Action? releaseCallback)
    : IDisposable
{
    public readonly string FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    private readonly FileStream _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    private bool _disposed = false;

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
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        CheckDisposed();
        _stream.SetLength(0);
        _stream.Write(data, 0, data.Length);
        _stream.Flush();
    }

    public void WriteAllText(string data, Encoding? encoding = null)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));

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
        releaseCallback?.Invoke();
        releaseCallback = null;
    }
}