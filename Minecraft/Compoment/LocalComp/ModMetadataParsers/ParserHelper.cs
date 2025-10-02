using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using PCL.Core.Logging;

namespace PCL.Core.Minecraft.Compoment.LocalComp.ModMetadataParsers;

internal static class ParserHelper
{
    /// <summary>
    /// Read text content from <see cref="ZipArchiveEntry"/>.
    /// </summary>
    /// <param name="entry">The ZipArchive entry that need to be readed.</param>
    /// <returns>Contents.</returns>
    public static string? ReadEntryContent(ZipArchiveEntry? entry)
    {
        if (entry == null)
        {
            return null;
        }

        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }


    /// <summary>
    /// Extrate mod logo from mod archive file to temp folder, and return the extracted file path.
    /// </summary>
    /// <returns>The extracted path of image.</returns>
    public static string? ExtractLogo(ZipArchive archive, string entryPath, string modFilePath)
    {
        if (string.IsNullOrEmpty(entryPath))
        {
            return null;
        }

        var logoEntry = archive.GetEntry(entryPath);
        if (logoEntry == null)
        {
            return null;
        }

        try
        {
            // TODO: replace by cache system
            var tempDir = Path.Combine(Path.GetTempPath(), "PCL_Cache", "ModImages");
            Directory.CreateDirectory(tempDir);

            var fileHash = _GetSimpleHash(modFilePath + logoEntry.Length);

            var logoPath = Path.Combine(tempDir, $"{fileHash}.png");

            if (File.Exists(logoPath))
            {
                return logoPath;
            }

            logoEntry.ExtractToFile(logoPath, true);
            return logoPath;
        }
        catch (Exception ex)
        {
            // i dont know is this log usage right... /cc whitecat346
            LogWrapper.Fatal(ex, $"Failed to extract logo '{entryPath}'.");
            return null;
        }
    }

    private static string _GetSimpleHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash).Replace('/', '_').Replace('+', '-')[..22];
    }
}