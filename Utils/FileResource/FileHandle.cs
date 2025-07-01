using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Utils.FileResource;

public sealed class FileHandle(string filePath, FileStream stream, Action releaseCallback) : IDisposable
{
    private bool _disposed = false;

    public string FilePath => filePath;
    public FileStream UnderlyingStream => !_disposed ? stream : throw new ObjectDisposedException(nameof(FileHandle));

    public byte[] ReadAllBytes()
    {
        CheckDisposed();
        stream.Seek(0, SeekOrigin.Begin);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public string ReadAllText(Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        CheckDisposed();
        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream, encoding, true, 1024, true);
        return reader.ReadToEnd();
    }

    public void WriteAllBytes(byte[] data)
    {
        CheckDisposed();
        stream.SetLength(0);
        stream.Write(data, 0, data.Length);
        stream.Flush();
    }

    public void WriteAllText(string data, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        CheckDisposed();
        stream.SetLength(0);
        byte[] bytes = encoding.GetBytes(data);
        stream.Write(bytes, 0, bytes.Length);
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
        stream.Dispose();
        releaseCallback.Invoke();
        releaseCallback = null!;
    }
}