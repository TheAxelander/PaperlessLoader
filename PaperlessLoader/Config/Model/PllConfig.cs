namespace PaperlessLoader.Config.Model;

public class PllConfig
{
    public PllServer Server { get; set; }
    public List<PllProfile> Profiles { get; set; }

    public PllConfig()
    {
        Server = new();
        Profiles = new();
    }
}