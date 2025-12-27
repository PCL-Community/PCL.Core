using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Utils;

public static class FileDeleteHelper
{
    /// <summary>
    /// Delete a file and wait.
    /// </summary>
    /// <param name="filePath">The file to delete.</param>
    /// <param name="timeout"></param>
    /// <param name="checkInterval"></param>
    /// <param name="ct"></param>
    /// <exception cref="ArgumentException">Throws if file path is invalied.</exception>
    /// <exception cref="IOException">Throws if fialed to delete file.</exception>
    /// <exception cref="TimeoutException">Throws if time out.</exception>
    /// <exception cref="OperationCanceledException">Throws if operation caceled.</exception>
    public static async Task DeleteFileAndWaitAsync(
        string filePath,
        TimeSpan? timeout = null,
        TimeSpan? checkInterval = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("File path is invalied.", nameof(filePath));
        }

        var exactFilePath = Path.GetFullPath(filePath);


        if (!File.Exists(exactFilePath))
        {
            return;
        }

        var realTimeout = timeout ?? TimeSpan.FromSeconds(10);
        var realCheckInterval = checkInterval ?? TimeSpan.FromMilliseconds(200);


        try
        {
            File.Delete(exactFilePath);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to initiate delete for file: {exactFilePath}", ex);
        }

        var startTime = DateTime.UtcNow;

        while (File.Exists(exactFilePath))
        {
            ct.ThrowIfCancellationRequested();

            if (DateTime.UtcNow - startTime > realTimeout)
            {
                throw new TimeoutException($"Time out waitting for file deletion: {exactFilePath}");
            }

            await Task.Delay(realCheckInterval, ct).ConfigureAwait(false);
        }
    }
}