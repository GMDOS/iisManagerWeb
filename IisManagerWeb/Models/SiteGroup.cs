using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IisManagerWeb.Models
{
    public class SiteGroup
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> SiteIds { get; set; } = new();
        public string DeploymentPath { get; set; } = string.Empty;
        
        [JsonIgnore]
        public bool IsExpanded { get; set; }
        
        [JsonIgnore]
        public List<WebSite> Sites { get; set; } = new();
    }
} 