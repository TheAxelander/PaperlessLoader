using Cocona;
using PaperlessLoader.Config;
using PaperlessLoader.Paperless;

var config = ConfigReader.Current.GetConfig();
var app = CoconaApp.Create();

#region tags

/*
 
pll tags
 
Commands:
  list    List all available tags
  add     Create tag
  
pll tags add    Create tag

Arguments:
  0: name    Name of the new tag (Required)  
*/
app.AddSubCommand("tags", x =>
    {
        x.AddCommand("list", async () => await ListTags())
            .WithDescription("List all available tags");
        x.AddCommand("add", async (
                [Argument(Description = "Name of the new tag")]string name) => 
                await CreateTag(name))
            .WithDescription("Create tag");
    })
    .WithDescription("Tag Management");

#endregion

#region document

/*
 
pll document import     Import documents

Arguments:
  0: path    Folder path of files to be imported (Required)

Options:
  -p, --profile             Name of the profile to be used for import (Required)
  -r, --rename              Rename files by prepending the date
  -d, --delete              Delete file after successful import
  --include-mac-os-tags     macOS only: Include file tags during import
  
*/
app.AddSubCommand("document", x =>
    {
        x.AddCommand("import", async (
                    [Argument(Description = "Folder path of files to be imported")]string path,
                    [Option('p', Description = "Name of the profile to be used for import")]string? profile,
                    [Option('r', Description = "Rename files by prepending the date")]bool rename,
                    [Option('d', Description = "Delete file after successful import")]bool delete,
                    [Option(Description = "macOS only: Include file tags during import")]bool useMacOsTags) => 
                await ImportDocumentsAsync(path, profile, rename, delete, useMacOsTags))
            .WithDescription("Import documents");
    })
    .WithDescription("Document Management");

#endregion

app.Run();

async Task ListTags()
{
    try
    {
        var connector = new PaperlessConnector(config.Server.ApiUrl, config.Server.Token);

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

async Task CreateTag(string name)
{
    try
    {
        var connector = new PaperlessConnector(config.Server.ApiUrl, config.Server.Token);
        await connector.CreateTagAsync(name);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
}

async Task ImportDocumentsAsync(string path, string? profileName, bool enableRenaming, bool enableDeletion, bool useMacOsTags)
{
    var importer = new PaperlessImporter(config.Server.ApiUrl, config.Server.Token);
    if (profileName == null)
    {
        await importer.ImportDocuments(path, enableRenaming, enableDeletion, useMacOsTags);
    }
    else
    {
        var profile = config.Profiles.Single(i => i.Name == profileName);
        await importer.ImportDocumentsUsingProfile(path, profile, enableRenaming, enableDeletion);
    }
}
