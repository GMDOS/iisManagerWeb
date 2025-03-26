using Microsoft.AspNetCore.Mvc;
using Microsoft.Web.Administration;
using IisManagerApi.Models;
using System.Text.Json;

namespace IisManagerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IisController : ControllerBase
{
    private readonly ILogger<IisController> _logger;

    public IisController(ILogger<IisController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetSites()
    {
        try
        {
            using var serverManager = new ServerManager();
            var sites = serverManager.Sites
                .Select(site => new WebSite
                {
                    Id = site.Id.ToString(),
                    Name = site.Name,
                    State = site.State.ToString(),
                    PhysicalPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath,
                    ApplicationPool = site.Applications["/"].ApplicationPoolName,
                    Bindings = site.Bindings.Select(b => b.BindingInformation).ToList(),
                    HasError = false // TODO: Implementar verificação de erro
                })
                .ToList();

            return Ok(sites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter sites");
            return StatusCode(500, "Erro ao obter sites");
        }
    }

    [HttpGet("{id}")]
    public ActionResult<WebSite> GetSite(string id)
    {
        try
        {
            using var serverManager = new ServerManager();
            var site = serverManager.Sites.FirstOrDefault(s => s.Id.ToString() == id);
            
            if (site == null)
                return NotFound();

            var webSite = new WebSite
            {
                Id = site.Id.ToString(),
                Name = site.Name,
                State = site.State.ToString(),
                PhysicalPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath,
                ApplicationPool = site.Applications["/"].ApplicationPoolName,
                Bindings = site.Bindings.Select(b => b.BindingInformation).ToList(),
                HasError = false // TODO: Implementar verificação de erro
            };

            return Ok(webSite);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter site {Id}", id);
            return StatusCode(500, "Erro ao obter site");
        }
    }

    [HttpGet("{siteName}/status")]
    public IActionResult GetSiteStatus(string siteName)
    {
        try
        {
            using var serverManager = new ServerManager();
            var site = serverManager.Sites[siteName];

            if (site == null)
                return NotFound($"Site {siteName} não encontrado");

            // Verifica se o site está respondendo
            var isResponding = CheckSiteResponse(site);
            return Ok(new { IsResponding = isResponding });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar status do site {SiteName}", siteName);
            return StatusCode(500, "Erro ao verificar status do site");
        }
    }

    [HttpPost("{id}/start")]
    public IActionResult StartSite(string id)
    {
        try
        {
            using var serverManager = new ServerManager();
            var site = serverManager.Sites.FirstOrDefault(s => s.Id.ToString() == id);
            
            if (site == null)
                return NotFound();

            site.Start();
            serverManager.CommitChanges();

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar site {Id}", id);
            return StatusCode(500, "Erro ao iniciar site");
        }
    }

    [HttpPost("{id}/stop")]
    public IActionResult StopSite(string id)
    {
        try
        {
            using var serverManager = new ServerManager();
            var site = serverManager.Sites.FirstOrDefault(s => s.Id.ToString() == id);
            
            if (site == null)
                return NotFound();

            site.Stop();
            serverManager.CommitChanges();

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao parar site {Id}", id);
            return StatusCode(500, "Erro ao parar site");
        }
    }

    [HttpPost("{siteName}/restart")]
    public IActionResult RestartSite(string siteName)
    {
        try
        {
            using var serverManager = new ServerManager();
            var site = serverManager.Sites[siteName];

            if (site == null)
                return NotFound($"Site {siteName} não encontrado");

            site.Stop();
            site.Start();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao reiniciar site {SiteName}", siteName);
            return StatusCode(500, "Erro ao reiniciar site");
        }
    }

    private bool CheckSiteResponse(Site site)
    {
        try
        {
            if (site.State != ObjectState.Started)
                return false;

            var binding = site.Bindings.FirstOrDefault();
            if (binding == null)
                return false;

            var url = $"http://{binding.BindingInformation}";
            using var client = new HttpClient();
            var response = client.GetAsync(url).Result;
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
} 