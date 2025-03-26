namespace IisManagerWeb.Models;

public class IisSite
{
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PhysicalPath { get; set; } = string.Empty;
    public string ApplicationPoolName { get; set; } = string.Empty;
    public List<string> Bindings { get; set; } = new();
    public bool HasError { get; set; }
} 