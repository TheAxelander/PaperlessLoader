namespace PaperlessLoader.Extensions;

public class MacTagReader
{
    public string[] ReadTagsFromMetadata(string filePath)
    {
        var tags = Array.Empty<string>();

        try
        {
            // Extract tags using macOS Spotlight metadata
            var spotlightCmd = $"mdls -name kMDItemUserTags \"{filePath}\"";
            var output = RunShellCommand(spotlightCmd);

            // Parse the output to extract the tags
            var start = output.IndexOf('(');
            var end = output.LastIndexOf(')');
            if (start != -1 && end != -1 && end > start)
            {
                var tagsString = output.Substring(start + 1, end - start - 1).Trim();
                tags = tagsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < tags.Length; i++)
                {
                    tags[i] = tags[i].Trim();
                    tags[i] = tags[i].Replace("\"", string.Empty);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while reading file metadata:");
            Console.WriteLine(ex.Message);
        }

        return tags;
    }

    private string RunShellCommand(string command)
    {
        var output = string.Empty;
        try
        {
            var escapedArgs = command.Replace("\"", "\\\"");

            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{escapedArgs}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while running shell command: {command}");
            Console.WriteLine(ex.Message);
        }

        return output;
    }
}