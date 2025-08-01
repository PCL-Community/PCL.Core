using System.Collections.Generic;
using System.Linq;

namespace PCL.Core.Model.Files;

public class FileMatchPair<TValue>(FileMatch match, TValue value)
{
    public bool Match(FileItem item) => match(item);
    public TValue Value => value;
}

public static class FileMatchPairExtension
{
    public static IEnumerable<TSource> MatchAll<TSource>(this IEnumerable<FileMatchPair<TSource>> pairs, FileItem item)
        => pairs.Where(pair => pair.Match(item)).Select(pair => pair.Value);
    
    public static TSource? MatchFirst<TSource>(this IEnumerable<FileMatchPair<TSource>> pairs, FileItem item)
    {
        var pair = pairs.FirstOrDefault(pair => pair.Match(item));
        return pair == null ? default : pair.Value;
    } 
}
