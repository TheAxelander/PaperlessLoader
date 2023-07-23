namespace PaperlessLoader.Config.Model;

public class PllProfile
{
    public string Name { get; set; }
    public string AppendString { get; set; }
    public List<string> Tags { get; set; }

    public PllProfile()
    {
        Name = string.Empty;
        AppendString = string.Empty;
        Tags = new();
    }
}