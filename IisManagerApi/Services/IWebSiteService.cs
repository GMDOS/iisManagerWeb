using IisManagerApi.Models;

namespace IisManagerApi.Services
{
    public interface IWebSiteService
    {
        Task<List<WebSite>> GetAllSites();
        Task<WebSite> GetSite(string id);
        Task StartSite(string id);
        Task StopSite(string id);
    }
} 