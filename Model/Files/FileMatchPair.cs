namespace PCL.Core.Model.Files;

public class FileMatchPair<TValue>(FileMatch match, TValue value)
{
    public bool Match(FileItem item) => match(item);
    public TValue Value => value;
}
