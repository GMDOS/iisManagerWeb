namespace IisManagerApi.Models
{
    public class WebSite
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string PhysicalPath { get; set; }
        public string State { get; set; }
        public string ApplicationPool { get; set; }
        public List<string> Bindings { get; set; } = new();
        public bool HasError { get; set; }
    }
} 