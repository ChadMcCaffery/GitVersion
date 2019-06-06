using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GitVersion;

public static class FileHelper
{
    private static readonly Dictionary<string, Func<string, string, bool>> versionAttributeFinders = new Dictionary<string, Func<string, string, bool>>()
    {
        { ".cs", CSharpFileContainsVersionAttribute },
        { ".vb", VisualBasicFileContainsVersionAttribute }
    };

    public static string TempPath;

    static FileHelper()
    {
        TempPath = Path.Combine(Path.GetTempPath(), "GitVersionTask");
        Directory.CreateDirectory(TempPath);
    }

    public static void DeleteTempFiles()
    {
        if (!Directory.Exists(TempPath))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(TempPath))
        {
            if (File.GetLastWriteTime(file) < DateTime.Now.AddDays(-1))
            {
                try
                {
                    File.Delete(file);
                }
                catch (UnauthorizedAccessException)
                {
                    //ignore contention
                }
            }
        }
    }

    public static string GetFileExtension(string language)
    {
        switch (language)
        {
            case "C#":
                return "cs";
            case "F#":
                return "fs";
            case "VB":
                return "vb";
            default:
                throw new ArgumentException($"Unknown language detected: '{language}'");
        }
    }

    public static void CheckForInvalidFiles(IEnumerable<string> compileFiles, string projectFile)
    {
        foreach (var compileFile in GetInvalidFiles(compileFiles, projectFile))
        {
            throw new WarningException("File contains assembly version attributes which conflict with the attributes generated by GitVersion " + compileFile);
        }
    }

    private static bool FileContainsVersionAttribute(string compileFile, string projectFile)
    {
        var compileFileExtension = Path.GetExtension(compileFile);

        if (versionAttributeFinders.TryGetValue(compileFileExtension, out var languageSpecificFileContainsVersionAttribute))
        {
            return languageSpecificFileContainsVersionAttribute(compileFile, projectFile);
        }

        throw new WarningException("File with name containing AssemblyInfo could not be checked for assembly version attributes which conflict with the attributes generated by GitVersion " + compileFile);
    }

    private static bool CSharpFileContainsVersionAttribute(string compileFile, string projectFile)
    {
        var combine = Path.Combine(Path.GetDirectoryName(projectFile), compileFile);
        var allText = File.ReadAllText(combine);

        var blockComments = @"/\*(.*?)\*/";
        var lineComments = @"//(.*?)\r?\n";
        var strings = @"""((\\[^\n]|[^""\n])*)""";
        var verbatimStrings = @"@(""[^""]*"")+";

        var noCommentsOrStrings = Regex.Replace(allText,
            blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
            me => me.Value.StartsWith("//") ? Environment.NewLine : "",
            RegexOptions.Singleline);

        return Regex.IsMatch(noCommentsOrStrings, @"(?x) # IgnorePatternWhitespace

\[\s*assembly\s*:\s*                    # The [assembly: part

(System\s*\.\s*Reflection\s*\.\s*)?     # The System.Reflection. part (optional)

Assembly(File|Informational)?Version    # The attribute AssemblyVersion, AssemblyFileVersion, or AssemblyInformationalVersion

\s*\(\s*\)\s*\]                         # End brackets ()]");
    }

    private static bool VisualBasicFileContainsVersionAttribute(string compileFile, string projectFile)
    {
        var combine = Path.Combine(Path.GetDirectoryName(projectFile), compileFile);
        var allText = File.ReadAllText(combine);

        var lineComments = @"'(.*?)\r?\n";
        var strings = @"""((\\[^\n]|[^""\n])*)""";

        var noCommentsOrStrings = Regex.Replace(allText,
            lineComments + "|" + strings,
            me => me.Value.StartsWith("'") ? Environment.NewLine : "",
            RegexOptions.Singleline);

        return Regex.IsMatch(noCommentsOrStrings, @"(?x) # IgnorePatternWhitespace

\<\s*Assembly\s*:\s*                    # The <Assembly: part

(System\s*\.\s*Reflection\s*\.\s*)?     # The System.Reflection. part (optional)

Assembly(File|Informational)?Version    # The attribute AssemblyVersion, AssemblyFileVersion, or AssemblyInformationalVersion

\s*\(\s*\)\s*\>                         # End brackets ()>");
    }

    private static IEnumerable<string> GetInvalidFiles(IEnumerable<string> compileFiles, string projectFile)
    {
        return compileFiles
            .Where(compileFile => compileFile.Contains("AssemblyInfo"))
            .Where(s => FileContainsVersionAttribute(s, projectFile));
    }
}