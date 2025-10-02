namespace PCL.Core.Utils;

public static class UrlConverter
{
    public static string HandleCurseForgeDownloadUrl(string url) =>
        url.Replace("-service.overwolf.wtf", ".forgecdn.net")
            .Replace("://media.", "://edge.")
            .Replace("://mediafilez.", "://edge.");
}