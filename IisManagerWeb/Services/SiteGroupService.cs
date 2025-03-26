using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using IisManagerWeb.Models;

namespace IisManagerWeb.Services
{
    public class SiteGroupService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private readonly string _groupsFilePath;

        public SiteGroupService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _groupsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "groups.json");
        }

        public async Task<List<SiteGroup>> GetAllGroups()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/sitegroups");
                if (response.IsSuccessStatusCode)
                {
                    var groups = await response.Content.ReadFromJsonAsync<List<SiteGroup>>();
                    if (groups != null)
                    {
                        // Preencher a lista de sites para cada grupo
                        foreach (var group in groups)
                        {
                            group.Sites = await GetSitesForGroup(group.SiteIds);
                        }
                        return groups;
                    }
                }
                return new List<SiteGroup>();
            }
            catch (Exception)
            {
                // Se houver erro na API, tenta ler do arquivo local
                if (File.Exists(_groupsFilePath))
                {
                    var json = await File.ReadAllTextAsync(_groupsFilePath);
                    var groups = JsonSerializer.Deserialize<List<SiteGroup>>(json);
                    if (groups != null)
                    {
                        // Preencher a lista de sites para cada grupo
                        foreach (var group in groups)
                        {
                            group.Sites = await GetSitesForGroup(group.SiteIds);
                        }
                        return groups;
                    }
                }
                return new List<SiteGroup>();
            }
        }

        private async Task<List<WebSite>> GetSitesForGroup(List<string> siteIds)
        {
            var sites = new List<WebSite>();
            foreach (var siteId in siteIds)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"{_baseUrl}/api/iis/{siteId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var site = await response.Content.ReadFromJsonAsync<WebSite>();
                        if (site != null)
                        {
                            sites.Add(site);
                        }
                    }
                }
                catch
                {
                    // Ignora erros ao buscar sites individuais
                }
            }
            return sites;
        }

        public async Task<SiteGroup?> GetGroup(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/sitegroups/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var group = await response.Content.ReadFromJsonAsync<SiteGroup>();
                    if (group != null)
                    {
                        group.Sites = await GetSitesForGroup(group.SiteIds);
                        return group;
                    }
                }
                return null;
            }
            catch
            {
                var groups = await GetAllGroups();
                return groups.FirstOrDefault(g => g.Id == id);
            }
        }

        public async Task<SiteGroup?> CreateGroup(SiteGroup group)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/sitegroups", group);
                if (response.IsSuccessStatusCode)
                {
                    var createdGroup = await response.Content.ReadFromJsonAsync<SiteGroup>();
                    if (createdGroup != null)
                    {
                        createdGroup.Sites = await GetSitesForGroup(createdGroup.SiteIds);
                        return createdGroup;
                    }
                }
                return null;
            }
            catch
            {
                // Se houver erro na API, salva localmente
                var groups = await GetAllGroups();
                groups.Add(group);
                await SaveGroups(groups);
                group.Sites = await GetSitesForGroup(group.SiteIds);
                return group;
            }
        }

        public async Task<bool> UpdateGroup(SiteGroup group)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/api/sitegroups/{group.Id}", group);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                return false;
            }
            catch
            {
                // Se houver erro na API, atualiza localmente
                var groups = await GetAllGroups();
                var index = groups.FindIndex(g => g.Id == group.Id);
                if (index != -1)
                {
                    groups[index] = group;
                    await SaveGroups(groups);
                    return true;
                }
                return false;
            }
        }

        public async Task<bool> DeleteGroup(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/sitegroups/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                return false;
            }
            catch
            {
                // Se houver erro na API, remove localmente
                var groups = await GetAllGroups();
                var group = groups.FirstOrDefault(g => g.Id == id);
                if (group != null)
                {
                    groups.Remove(group);
                    await SaveGroups(groups);
                    return true;
                }
                return false;
            }
        }

        private async Task SaveGroups(List<SiteGroup> groups)
        {
            var json = JsonSerializer.Serialize(groups, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_groupsFilePath, json);
        }

        public async Task DeployGroup(string id, Stream zipStream)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(zipStream), "file", "deployment.zip");

            var response = await _httpClient.PostAsync($"api/sitegroup/{id}/deploy", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeployToGroup(string groupName, IBrowserFile file)
        {
            // Criar o conteúdo para envio
            using var content = new MultipartFormDataContent();

            // Converter o IBrowserFile para um ByteArrayContent
            var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024)); // 50MB max
            content.Add(fileContent, "file", file.Name);

            // Fazer o upload para a API
            var response = await _httpClient.PostAsync($"api/sitegroups/{groupName}/deploy", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateIgnoredFiles(string groupName, List<string> ignoredFiles)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/sitegroups/{groupName}/ignored-files", ignoredFiles);
            response.EnsureSuccessStatusCode();
        }
    }
} 