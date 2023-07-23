using System.Text.RegularExpressions;
using PaperlessLoader.Config.Model;
using PaperlessLoader.Extensions;

namespace PaperlessLoader.Paperless;

public partial class PaperlessImporter
{
    private PaperlessConnector _connector;
    
    [GeneratedRegex(@"\b\d{4}-(?:0[1-9]|1[0-2])-(?:0[1-9]|[1-2]\d|3[01])\b")]
    private static partial Regex DateStringRegex();
    
    [GeneratedRegex(@"\b(?:0[1-9]|[1-2]\d|3[01])\-(?:0[1-9]|1[0-2])\-\d{4}\b")]
    private static partial Regex DateStringRegexGerman();
    
    [GeneratedRegex(@"\b(?:0[1-9]|1[0-2])\-(?:0[1-9]|[1-2]\d|3[01])\-\d{4}\b")]
    private static partial Regex DateStringRegexUS();

    public PaperlessImporter(string apiUrl, string token)
    {
        _connector = new PaperlessConnector(apiUrl, token);
    }
    
    public async Task ImportDocuments(string path, bool useMacOsTags)
    {
        try
        {
            foreach (var file in Directory.GetFiles(path))
            {
                if (useMacOsTags)
                {
                    var macTagReader = new MacTagReader();
                    var fileTags = macTagReader.ReadTagsFromMetadata(file);
                    await _connector.ImportDocumentAsync(file, fileTags);
                }
                else
                {
                    await _connector.ImportDocumentAsync(file);    
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    public async Task ImportDocumentsUsingProfile(string path, PllProfile profile, bool enableDeletion)
    {
        RenameFilesInDirectory(path, profile.AppendString);
        await ImportDocumentsWithTags(path, profile.Tags, enableDeletion);
    }
    
    private void RenameFilesInDirectory(string directory, string appendString)
    {
        foreach (var file in Directory.GetFiles(directory))
        {
            var originalFileName = Path.GetFileName(file);
            var cleansedFileName = ExtractDateIfExisting(file);
            var extension = Path.GetExtension(file);

            var newFileName = string.IsNullOrEmpty(appendString) ? 
                $"{cleansedFileName} - {originalFileName}" : 
                $"{cleansedFileName} - {appendString}{extension}";
        
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