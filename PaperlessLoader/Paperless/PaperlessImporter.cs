using System.Text.RegularExpressions;
using PaperlessLoader.Config.Model;
using PaperlessLoader.Extensions;

namespace PaperlessLoader.Paperless;

public partial class PaperlessImporter
{
    private PaperlessConnector _connector;
    
    [GeneratedRegex(@"\d{4}-(?:0[1-9]|1[0-2])-(?:0[1-9]|[1-2]\d|3[01])")]
    private static partial Regex DateStringRegex();
    
    [GeneratedRegex(@"(?:0[1-9]|[1-2]\d|3[01])-(?:0[1-9]|1[0-2])-\d{4}")]
    private static partial Regex DateStringRegexGerman();
    
    [GeneratedRegex(@"(?:0[1-9]|1[0-2])-(?:0[1-9]|[1-2]\d|3[01])-\d{4}")]
    private static partial Regex DateStringRegexUS();

    public PaperlessImporter(string apiUrl, string token)
    {
        _connector = new PaperlessConnector(apiUrl, token);
    }
    
    public async Task ImportDocuments(string path, bool enableRenaming, bool enableDeletion, bool useMacOsTags)
    {
        try
        {
            if (enableRenaming) RenameFilesInDirectory(path);
            
            foreach (var file in Directory.GetFiles(path))
            {
                string documentId;
                if (useMacOsTags)
                {
                    var macTagReader = new MacTagReader();
                    var fileTags = macTagReader.ReadTagsFromMetadata(file);
                    documentId = await _connector.ImportDocumentAsync(file, fileTags);
                }
                else
                {
                    documentId = await _connector.ImportDocumentAsync(file);    
                }
                if (enableDeletion && !string.IsNullOrEmpty(documentId))
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    public async Task ImportDocumentsUsingProfile(string path, PllProfile profile, bool enableRenaming, bool enableDeletion)
    {
        if (enableRenaming) RenameFilesInDirectory(path, profile);
        await ImportDocumentsWithTags(path, profile.Tags, enableDeletion);
    }
    
    private async Task ImportDocumentsWithTags(string path, IReadOnlyCollection<string> fileTags, bool enableDeletion)
    {
        try
        {
            foreach (var file in Directory.GetFiles(path))
            {
                var documentId = await _connector.ImportDocumentAsync(file, fileTags);
                if (enableDeletion && !string.IsNullOrEmpty(documentId))
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    /// <summary>
    /// Try to get the date for all files in the passed directory based on passed format
    /// </summary>
    /// <param name="directory">Path where the files are stored to be renamed</param>
    /// <param name="profile">Import profile including settings for renaming</param>
    private void RenameFilesInDirectory(string directory, PllProfile profile)
    {
        foreach (var file in Directory.GetFiles(directory))
        {
            var originalFileName = Path.GetFileName(file);
            if (!TryExtractDateWithFormat(file, profile.InputDateRegex, profile.OutputDateFormat,
                    out var cleansedFileName)) continue;
            var extension = Path.GetExtension(file);

            var newFileName = string.IsNullOrEmpty(profile.AppendString) ? 
                $"{cleansedFileName} - {originalFileName}" : 
                $"{cleansedFileName} - {profile.AppendString}{extension}";
        
            var directoryPath = Path.GetDirectoryName(file);
            var newFilePath = string.IsNullOrEmpty(directoryPath) ? 
                newFileName : Path.Combine(directoryPath, newFileName);

            try
            {
                File.Move(file, newFilePath);
                Console.WriteLine($"Renamed {originalFileName} to {newFileName}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while renaming {originalFileName}: {e.Message}");
            }
        } 
    }
    
    /// <summary>
    /// Try to get the date for all files in the passed directory based on some predefined formats
    /// </summary>
    /// <param name="directory">Path where the files are stored to be renamed</param>
    private void RenameFilesInDirectory(string directory)
    {
        foreach (var file in Directory.GetFiles(directory))
        {
            var originalFileName = Path.GetFileName(file);
            var cleansedFileName = ExtractDateIfExisting(file);

            var newFileName = $"{cleansedFileName} - {originalFileName}";
        
            var directoryPath = Path.GetDirectoryName(file);
            var newFilePath = string.IsNullOrEmpty(directoryPath) ? 
                newFileName : Path.Combine(directoryPath, newFileName);

            try
            {
                File.Move(file, newFilePath);
                Console.WriteLine($"Renamed {originalFileName} to {newFileName}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while renaming {originalFileName}: {e.Message}");
            }
        } 
    }

    /// <summary>
    /// Try to get the date in the file name based on passed format
    /// </summary>
    /// <param name="fileName">File that should be renamed</param>
    /// <param name="inputDatePattern">Date format in the file name</param>
    /// <param name="outputDateFormat">Date format that should be used during renaming</param>
    /// <param name="result">Renamed file name</param>
    /// <returns>Returns true if a date has been found and parsed successfully</returns>
    private bool TryExtractDateWithFormat(string fileName, string inputDatePattern, string outputDateFormat, out string result)
    {
        try
        {
            var regexResult = Regex.Match(fileName, inputDatePattern);
            if (regexResult.Success && DateTime.TryParse(regexResult.Value, out var date))
            {
                result = date.ToString(outputDateFormat);
                return true;
            }
            else
            {
                // No Date found or parse failed, return original value
                result = Path.GetFileNameWithoutExtension(fileName);
                return false;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            result = Path.GetFileNameWithoutExtension(fileName);
            return false;
        }
    }

    /// <summary>
    /// Try to get the date in the file name based on some predefined formats
    /// </summary>
    /// <remarks>
    /// Replaces '.' and ' ' with '-' and parses formats yyyy-mm-dd / dd-mm-yyyy / mm-dd-yyyy
    /// </remarks>
    /// <param name="fileName">File that should be renamed</param>
    /// <returns>Renamed file name</returns>
    private string ExtractDateIfExisting(string fileName)
    {
        try
        {
            // Normalize various chars to dash char
            var normalizedFileName = Path.GetFileNameWithoutExtension(fileName)
                .Replace('_', '-')
                .Replace('.', '-')
                .Replace(' ', '-');
                
            // Check if Date is in file name
            
            // Check yyyy-mm-dd
            var regexResult = DateStringRegex().Match(normalizedFileName);
            if (regexResult.Success) return regexResult.Value;
            
            // Check dd-mm-yyyy
            regexResult = DateStringRegexGerman().Match(normalizedFileName);
            if (regexResult.Success)
            {
                var valueSplit = regexResult.Value.Split('-');
                return $"{valueSplit[2]}-{valueSplit[1]}-{valueSplit[0]}";
            }
            
            // Check mm-dd-yyyy
            regexResult = DateStringRegexUS().Match(normalizedFileName);
            if (regexResult.Success)
            {
                var valueSplit = regexResult.Value.Split('-');
                return $"{valueSplit[2]}-{valueSplit[0]}-{valueSplit[1]}";
            }
            
            // No Date found, return original value
            return Path.GetFileNameWithoutExtension(fileName);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Path.GetFileNameWithoutExtension(fileName);
        }
    }
}