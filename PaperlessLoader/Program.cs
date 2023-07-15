string _apiUrl;
string _token;

ReadConfig();
Console.WriteLine("Hello, World!");

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