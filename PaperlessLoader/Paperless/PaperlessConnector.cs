using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace PaperlessLoader.Paperless;

public class PaperlessConnector
{
    private class TagsResultResponse
    {
        public string Next { get; set; }
        public List<TagObject> Results { get; set; }

        private TagsResultResponse()
        {
            Next = string.Empty;
            Results = new();
        }
    }

    private class TagObject
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public TagObject()
        {
            Id = string.Empty;
            Name = string.Empty;
        }
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
    
    public async Task<Dictionary<string, string>> GetTagsAsync()
    {
        try
        {
            var response = await BrowseThroughTagsFromServer($"{_url}/api/tags/");
            var result = response.Results.ToDictionary(i => i.Name, i => i.Id);

            while (!string.IsNullOrEmpty(response.Next))
            {
                response = await BrowseThroughTagsFromServer(response.Next);
                foreach (var responseResult in response.Results)
                {
                    result.Add(responseResult.Name, responseResult.Id);
                }
            }

            return result;
        }
        catch (Exception e)
        {
            throw new Exception($"Getting tags failed: {e.Message}");
        }
    }

    private async Task<TagsResultResponse> BrowseThroughTagsFromServer(string pageUrl)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _token);
            
            var response = await client.GetAsync(pageUrl);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TagsResultResponse>(responseContent);
            if (result == null) throw new Exception("Unable to parse response from server.");
            
            return result;
        }
        catch (Exception e)
        {
            throw new Exception($"Getting tags failed: {e.Message}");
        }
    }

    public async Task<string> CreateTagAsync(string name)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _token);
            
            var form = new MultipartFormDataContent();
            var nameContent = new StringContent(name);
            form.Add(nameContent, "name");
            
            var response = await client.PostAsync($"{_url}/api/tags/", form);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var tagResultResponse = JsonConvert.DeserializeObject<TagObject>(responseContent);

            if (tagResultResponse == null) throw new Exception("Unable to read response");
            Console.WriteLine($"Tag {name} created successfully with ID {tagResultResponse.Id}.");
            return tagResultResponse.Id;
        }
        catch (Exception e)
        {
            throw new Exception($"Getting tags failed: {e.Message}");
        }
    }

    public async Task ImportDocumentAsync(string filePath)
    {
        await ImportDocumentAsync(filePath, new List<string>());
    }

    public async Task<string> ImportDocumentAsync(string filePath, IEnumerable<string> tags)
    {
        try
        {
            if (!_tags.Any()) _tags = await GetTagsAsync();
            
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
                if (!_tags.TryGetValue(tag, out var tagId))
                {
                    try
                    {
                        tagId = await CreateTagAsync(tag);
                        _tags.Add(tag, tagId);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Unable to create new tag: {e}");
                        continue;
                    }
                }
                var tagContent = new StringContent(tagId);
                form.Add(tagContent, "tags");
            }
            
            // Step 4: Upload the file with tags
            var response = await client.PostAsync($"{_url}/api/documents/post_document/", form);
            response.EnsureSuccessStatusCode();

            var documentId = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"File {filePath} uploaded successfully with ID {documentId}.");
            return documentId;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Upload failed: {e.Message}");
            return string.Empty;
        }
    }
}