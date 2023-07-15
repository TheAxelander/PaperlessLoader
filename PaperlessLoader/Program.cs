using Cocona;
using PaperlessLoader.Extensions;
using PaperlessLoader.Paperless;

string _apiUrl = string.Empty;
string _token = string.Empty;

ReadConfig();
var app = CoconaApp.Create();

app.AddSubCommand("tags", x =>
    {
        x.AddCommand("list", async () => await ListTags())
            .WithDescription("List all available tags");
    })
    .WithDescription("Tag Management");
app.AddSubCommand("document", x =>
    {
        x.AddCommand("import", async (
                    [Argument(Description = "Folder path of files to be imported")]string path, 
                    [Option(Description = "macOS only: Include file tags during import")]bool includeMacOsTags) => 
                await ImportDocuments(path, includeMacOsTags))
            .WithDescription("Import documents");
    })
    .WithDescription("Document Management");

app.Run();

async Task ListTags()
{
    try
    {
        var connector = new PaperlessConnector(_apiUrl, _token);

        var tags = await connector.GetTagsAsync();
        if (!tags.Any())
        {
            Console.WriteLine("No tags available.");
            return;
        }

        foreach (var tag in tags)
        {
            Console.WriteLine($"{tag.Value} : {tag.Key}");
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
}

async Task ImportDocuments(string path, bool includeMacOsTags)
{
    try
    {
        var connector = new PaperlessConnector(_apiUrl, _token);

        foreach (var file in Directory.GetFiles(path))
        {
            if (includeMacOsTags)
            {
                var macTagReader = new MacTagReader();
                var fileTags = macTagReader.ReadTagsFromMetadata(file);
                await connector.ImportDocumentAsync(file, fileTags);
            }
            else
            {
                await connector.ImportDocumentAsync(file);
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
}

void ReadConfig()
{
    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "config.env");
        
    if (!File.Exists(filePath))
    {
        throw new Exception("config.env file not available");
    }

    foreach (var line in File.ReadAllLines(filePath))
    {
        var parts = line.Split('=', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) continue;

        switch (parts[0])
        {
            case "APIURL":
                _apiUrl = parts[1];
                break;
            case "TOKEN":
                _token = parts[1];
                break;
        }
    }
}