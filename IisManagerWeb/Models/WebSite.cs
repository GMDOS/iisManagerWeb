namespace IisManagerWeb.Models
{
    public class WebSite
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string PhysicalPath { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ApplicationPool { get; set; } = string.Empty;
        public List<string> Bindings { get; set; } = new();
        public bool HasError { get; set; }
    }
} 