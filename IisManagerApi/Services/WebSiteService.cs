using Microsoft.Web.Administration;
using IisManagerApi.Models;

namespace IisManagerApi.Services
{
    public class WebSiteService : IWebSiteService
    {
        private readonly ServerManager _serverManager;

        public WebSiteService()
        {
            _serverManager = new ServerManager();
        }

        public async Task<List<WebSite>> GetAllSites()
        {
            var sites = _serverManager.Sites;
            return sites.Select(site => new WebSite
            {
                Id = site.Id.ToString(),
                Name = site.Name,
                PhysicalPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath,
                State = site.State.ToString(),
                ApplicationPool = site.Applications["/"].ApplicationPoolName,
                Bindings = site.Bindings.Select(b => b.BindingInformation).ToList(),
                HasError = false // TODO: Implementar verificação de erro
            }).ToList();
        }

        public async Task<WebSite> GetSite(string id)
        {
            var site = _serverManager.Sites.FirstOrDefault(s => s.Id.ToString() == id);
            if (site == null)
                return null;

            return new WebSite
            {
                Id = site.Id.ToString(),
                Name = site.Name,
                PhysicalPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath,
                State = site.State.ToString(),
                ApplicationPool = site.Applications["/"].ApplicationPoolName,
                Bindings = site.Bindings.Select(b => b.BindingInformation).ToList(),
                HasError = false // TODO: Implementar verificação de erro
            };
        }

        public async Task StartSite(string id)
        {
            var site = _serverManager.Sites.FirstOrDefault(s => s.Id.ToString() == id);
            if (site != null)
            {
                site.Start();
                _serverManager.CommitChanges();
            }
        }

        public async Task StopSite(string id)
        {
            var site = _serverManager.Sites.FirstOrDefault(s => s.Id.ToString() == id);
            if (site != null)
            {
                site.Stop();
                _serverManager.CommitChanges();
            }
        }
    }
} 