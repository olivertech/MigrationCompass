using System.Configuration;

namespace Bridge.Library;

public class Bridge
{
    public string? Read() => ConfigurationManager.AppSettings["mode"];
}