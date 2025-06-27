using System;
using System.IO;

namespace PCL.Core.Helper;

public class ByteHelper(Stream stream)
{
    private Stream _byteStream = stream;
    
    public long Length => _byteStream.Length;

    public string GetReadableLength() => GetReadableLength(this.Length);
    
    public static string GetReadableLength(long length)
    {
        string[] unit = ["B", "KB", "MB", "GB", "TB", "PB"];
        long displayCount = length * 100;
        int displayUnit = 0;
        while (displayCount >= 102400)
        {
            displayCount >>= 10;
            displayUnit++;
        }

        if (displayUnit > unit.Length)
            throw new IndexOutOfRangeException("Why there is no enough unit to show :(");
        var displayText = displayCount.ToString();
        displayText = displayText.Insert(displayText.Length - 2, ".");
        return $"{displayText} {unit[displayUnit]}";
    }
}