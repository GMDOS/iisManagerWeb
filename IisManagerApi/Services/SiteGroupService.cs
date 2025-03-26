using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using IisManagerApi.Models;
using Microsoft.Extensions.Configuration;

namespace IisManagerApi.Services
{
    public class SiteGroupService
    {
        private readonly string _groupsFilePath;
        private readonly IConfiguration _configuration;
        private readonly IWebSiteService _webSiteService;

        public SiteGroupService(IConfiguration configuration, IWebSiteService webSiteService)
        {
            _configuration = configuration;
            _webSiteService = webSiteService;
            _groupsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sitegroups.json");
            EnsureGroupsFileExists();
        }

        private void EnsureGroupsFileExists()
        {
            if (!File.Exists(_groupsFilePath))
            {
                File.WriteAllText(_groupsFilePath, JsonSerializer.Serialize(new List<SiteGroup>()));
            }
        }

        public List<SiteGroup> GetAllGroups()
        {
            var json = File.ReadAllText(_groupsFilePath);
            return JsonSerializer.Deserialize<List<SiteGroup>>(json);
        }

        public SiteGroup GetGroup(string id)
        {
            var groups = GetAllGroups();
            return groups.FirstOrDefault(g => g.Id == id);
        }

        public SiteGroup CreateGroup(SiteGroup group)
        {
            var groups = GetAllGroups();
            group.Id = Guid.NewGuid().ToString();
            groups.Add(group);
            SaveGroups(groups);
            return group;
        }

        public void UpdateGroup(SiteGroup group)
        {
            var groups = GetAllGroups();
            var index = groups.FindIndex(g => g.Id == group.Id);
            if (index != -1)
            {
                groups[index] = group;
                SaveGroups(groups);
            }
        }

        public void DeleteGroup(string id)
        {
            var groups = GetAllGroups();
            groups.RemoveAll(g => g.Id == id);
            SaveGroups(groups);
        }

        private void SaveGroups(List<SiteGroup> groups)
        {
            var json = JsonSerializer.Serialize(groups, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_groupsFilePath, json);
        }

        public async Task DeployGroup(string groupId, Stream zipStream)
        {
            var group = GetGroup(groupId);
            if (group == null)
                throw new Exception("Grupo não encontrado");

            // Criar diretório temporário para extração
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            try
            {
                // Extrair arquivo ZIP
                using (var archive = new ZipArchive(zipStream))
                {
                    archive.ExtractToDirectory(tempPath);
                }

                // Parar todos os sites do grupo
                foreach (var siteId in group.SiteIds)
                {
                    await _webSiteService.StopSite(siteId);
                }

                // Copiar arquivos para cada site
                foreach (var siteId in group.SiteIds)
                {
                    var site = await _webSiteService.GetSite(siteId);
                    if (site != null)
                    {
                        var sitePath = site.PhysicalPath;
                        CopyDirectory(tempPath, sitePath);
                    }
                }

                // Iniciar todos os sites do grupo
                foreach (var siteId in group.SiteIds)
                {
                    await _webSiteService.StartSite(siteId);
                }
            }
            finally
            {
                // Limpar diretório temporário
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        private void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);

            foreach (var file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            }

            foreach (var directory in Directory.GetDirectories(source))
            {
                CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
            }
        }
    }
} 