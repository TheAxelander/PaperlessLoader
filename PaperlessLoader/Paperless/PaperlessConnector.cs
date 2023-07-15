using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace PaperlessLoader.Paperless;

public class PaperlessConnector
{
    private class TagsResultResponse
    {
        public List<TagObject> Results { get; set; }

        private TagsResultResponse()
        {
            Results = new();
        }
    }

    private class TagObject
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    private Dictionary<string, string> _tags;
    
    private readonly string _url;
    private readonly string _token;

    public PaperlessConnector(string url, string token)
    {
        _url = url;
        _token = token;
        _tags = new();
    }
    
    private async Task GetTags()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _token);
            
            var response = await client.GetAsync($"{_url}/api/tags/");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var tagsResultResponse = JsonConvert.DeserializeObject<TagsResultResponse>(responseContent);

            foreach (var tagObject in tagsResultResponse.Results)
            {
                _tags.Add(tagObject.Name, tagObject.Id);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Getting tags failed: {e.Message}");
        }
    }

    public async Task ImportDocument(string filePath, IEnumerable<string> tags)
    {
        if (!_tags.Any()) await GetTags();
        
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _token);

            // Step 1: Read the file content
            var fileBytes = await File.ReadAllBytesAsync(filePath);

            // Step 2: Create the MultipartFormDataContent
            var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            form.Add(fileContent, "document", Path.GetFileName(filePath));

            // Step 3: Add tags to the MultipartFormDataContent
            foreach (var tag in tags)
            {
                if (!_tags.TryGetValue(tag, out var tagId)) continue;
                var tagContent = new StringContent(tagId);
                form.Add(tagContent, "tags");
            }
            
            // Step 4: Upload the file with tags
            var response = await client.PostAsync($"{_url}/api/documents/post_document/", form);
            response.EnsureSuccessStatusCode();

            var documentId = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"File {filePath} uploaded successfully with ID {documentId}.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Upload failed: {e.Message}");
        }
    }
}