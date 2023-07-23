using System.Text.RegularExpressions;
using PaperlessLoader.Config.Model;
using PaperlessLoader.Extensions;

namespace PaperlessLoader.Paperless;

public partial class PaperlessImporter
{
    private PaperlessConnector _connector;
    
    [GeneratedRegex(@"\b\d{4}-(?:0[1-9]|1[0-2])-(?:0[1-9]|[1-2]\d|3[01])\b")]
    private static partial Regex DateStringRegex();

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
    
    public async Task ImportDocumentsUsingProfile(string path, PllProfile profile)
    {
        RenameFilesInDirectory(path, profile.AppendString);
        await ImportDocumentsWithTags(path, profile.Tags);
    }
    
    private void RenameFilesInDirectory(string directory, string appendString)
    {
        foreach (var file in Directory.GetFiles(directory))
        {
            var originalFileName = Path.GetFileName(file);
            var cleansedFileName = ExtractDateIfExisting(file);
            var extension = Path.GetExtension(file);
            var newFileName = $"{cleansedFileName} - {appendString}{extension}";
        
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

    private async Task ImportDocumentsWithTags(string path, IReadOnlyCollection<string> fileTags)
    {
        try
        {
            foreach (var file in Directory.GetFiles(path))
            {
                await _connector.ImportDocumentAsync(file, fileTags);
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
            // Normalize underscore and spaces to dash char
            var normalizedFileName = Path.GetFileNameWithoutExtension(fileName)
                .Replace('_', '-')
                .Replace(' ', '-');
                
            // Check if Date is in file name
            var regexResult = DateStringRegex().Match(normalizedFileName);
            
            // Return result
            return regexResult.Success ? regexResult.Value : normalizedFileName;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Path.GetFileNameWithoutExtension(fileName);
        }
    }
}