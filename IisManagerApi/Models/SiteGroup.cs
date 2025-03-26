using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IisManagerApi.Models
{
    public class SiteGroup
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> SiteIds { get; set; } = new List<string>();
        public string DeploymentPath { get; set; } = string.Empty;
        public List<string> IgnoredFiles { get; set; } = new List<string>();
    }
} 