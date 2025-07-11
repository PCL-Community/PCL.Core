namespace PCL.Core.Model.Files;

/// <summary>
/// Match a <see cref="FileItem"/>.
/// </summary>
/// <param name="item">the item to match</param>
/// <returns>match result</returns>
public delegate bool FileMatch(FileItem item);

public static class FileMatchExtension
{
    public static FileMatchPair<TValue> Pair<TValue>(this FileMatch match, TValue value) => new(match, value);
}
