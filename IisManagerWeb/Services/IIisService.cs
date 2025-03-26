using IisManagerWeb.Models;

namespace IisManagerWeb.Services
{
    public interface IIisService
    {
        Task<List<WebSite>> GetAllSites();
        Task<WebSite?> GetSite(string id);
        Task StartSite(string id);
        Task StopSite(string id);
    }
} 