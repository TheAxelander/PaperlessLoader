namespace PaperlessLoader.Config.Model;

public class PllServer
{
    public string ApiUrl { get; set; }
    public string Token { get; set; }

    public PllServer()
    {
        ApiUrl = string.Empty;
        Token = string.Empty;
    }
}