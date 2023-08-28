namespace PaperlessLoader.Config.Model;

public class PllProfile
{
    public string Name { get; set; }
    public string AppendString { get; set; }
    public string InputDateRegex { get; set; }
    public string InputDateFormat { get; set; }
    public string OutputDateFormat { get; set; }
    public List<string> Tags { get; set; }

    public PllProfile()
    {
        Name = string.Empty;
        AppendString = string.Empty;
        InputDateRegex = string.Empty;
        InputDateFormat = string.Empty;
        OutputDateFormat = string.Empty;
        Tags = new();
    }
}