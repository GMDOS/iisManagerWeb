using System.Collections.Generic;

namespace IisManagerApi.Models
{
    public class SiteGroup
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> SiteIds { get; set; }
        public string DeploymentPath { get; set; }
    }
} 