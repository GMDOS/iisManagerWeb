using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IO.Compression;
using IisManagerApi.Models;
using Microsoft.Web.Administration;

namespace IisManagerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SiteGroupsController : ControllerBase
    {
        private readonly string _groupsFilePath;
        private readonly ILogger<SiteGroupsController> _logger;
        private readonly ServerManager _serverManager;

        public SiteGroupsController(ILogger<SiteGroupsController> logger)
        {
            _logger = logger;
            _groupsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "groups.json");
            _serverManager = new ServerManager();
        }

        [HttpGet]
        public async Task<ActionResult<List<SiteGroup>>> GetGroups()
        {
            try
            {
                if (!System.IO.File.Exists(_groupsFilePath))
                {
                    return new List<SiteGroup>();
                }

                var json = await System.IO.File.ReadAllTextAsync(_groupsFilePath);
                var groups = JsonSerializer.Deserialize<List<SiteGroup>>(json) ?? new List<SiteGroup>();
                return Ok(groups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar grupos");
                return StatusCode(500, "Erro ao carregar grupos");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateGroup(SiteGroup group)
        {
            try
            {
                var groups = await GetGroupsFromFile();

                if (groups.Any(g => g.Name == group.Name))
                {
                    return BadRequest("Já existe um grupo com este nome");
                }

                groups.Add(group);
                await SaveGroupsToFile(groups);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar grupo");
                return StatusCode(500, "Erro ao criar grupo");
            }
        }

        [HttpPut("{name}")]
        public async Task<ActionResult> UpdateGroup(string name, SiteGroup group)
        {
            try
            {
                var groups = await GetGroupsFromFile();
                var existingGroup = groups.FirstOrDefault(g => g.Name == name);

                if (existingGroup == null)
                {
                    return NotFound("Grupo não encontrado");
                }

                groups.Remove(existingGroup);
                groups.Add(group);

                await SaveGroupsToFile(groups);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar grupo");
                return StatusCode(500, "Erro ao atualizar grupo");
            }
        }

        [HttpDelete("{name}")]
        public async Task<ActionResult> DeleteGroup(string name)
        {
            try
            {
                var groups = await GetGroupsFromFile();
                var group = groups.FirstOrDefault(g => g.Name == name);

                if (group == null)
                {
                    return NotFound("Grupo não encontrado");
                }

                groups.Remove(group);
                await SaveGroupsToFile(groups);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir grupo");
                return StatusCode(500, "Erro ao excluir grupo");
            }
        }

        [HttpPost("{name}/deploy")]
        public async Task<ActionResult> DeployToGroup(string name, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Nenhum arquivo foi enviado");
                }

                if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("O arquivo deve ser um ZIP");
                }

                var groups = await GetGroupsFromFile();
                var group = groups.FirstOrDefault(g => g.Name == name);

                if (group == null)
                {
                    return NotFound("Grupo não encontrado");
                }

                // Criar pasta temporária para extração
                var tempPath = Path.Combine(Path.GetTempPath(), "IisManagerDeploy_" + Guid.NewGuid());
                Directory.CreateDirectory(tempPath);

                try
                {
                    // Extrair arquivo ZIP para pasta temporária
                    using (var zipStream = file.OpenReadStream())
                    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                    {
                        archive.ExtractToDirectory(tempPath);
                    }

                    // Obter todos os sites do grupo
                    var sites = _serverManager.Sites
                        .Where(site => group.SiteIds.Contains(site.Name))
                        .ToList();

                    if (!sites.Any())
                    {
                        return BadRequest("O grupo não contém sites válidos");
                    }

                    // Parar todos os sites
                    foreach (var site in sites)
                    {
                        if (site.State == ObjectState.Started)
                        {
                            site.Stop();
                            _logger.LogInformation($"Site {site.Name} parado para deploy");
                        }
                    }

                    _serverManager.CommitChanges();

                    // Copiar arquivos para cada site
                    foreach (var site in sites)
                    {
                        var sitePath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath;
                        
                        // Substituir variáveis de ambiente, se houver
                        if (sitePath.Contains("%"))
                        {
                            sitePath = Environment.ExpandEnvironmentVariables(sitePath);
                        }

                        if (!Directory.Exists(sitePath))
                        {
                            _logger.LogWarning($"Caminho físico não encontrado para o site {site.Name}: {sitePath}");
                            continue;
                        }

                        CopyDirectory(tempPath, sitePath, group.IgnoredFiles);
                        _logger.LogInformation($"Arquivos copiados para o site {site.Name}");
                    }

                    // Iniciar todos os sites
                    foreach (var site in sites)
                    {
                        if (site.State != ObjectState.Started)
                        {
                            site.Start();
                            _logger.LogInformation($"Site {site.Name} iniciado após deploy");
                        }
                    }

                    _serverManager.CommitChanges();
                    return Ok("Deploy realizado com sucesso");
                }
                finally
                {
                    // Limpar pasta temporária
                    if (Directory.Exists(tempPath))
                    {
                        Directory.Delete(tempPath, true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante o deploy");
                return StatusCode(500, $"Erro durante o deploy: {ex.Message}");
            }
        }

        [HttpPut("{name}/ignored-files")]
        public async Task<ActionResult> UpdateIgnoredFiles(string name, [FromBody] List<string> ignoredFiles)
        {
            try
            {
                var groups = await GetGroupsFromFile();
                var group = groups.FirstOrDefault(g => g.Name == name);

                if (group == null)
                {
                    return NotFound("Grupo não encontrado");
                }

                group.IgnoredFiles = ignoredFiles;
                await SaveGroupsToFile(groups);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar arquivos ignorados");
                return StatusCode(500, "Erro ao atualizar arquivos ignorados");
            }
        }

        private async Task<List<SiteGroup>> GetGroupsFromFile()
        {
            if (!System.IO.File.Exists(_groupsFilePath))
            {
                return new List<SiteGroup>();
            }

            var json = await System.IO.File.ReadAllTextAsync(_groupsFilePath);
            return JsonSerializer.Deserialize<List<SiteGroup>>(json) ?? new List<SiteGroup>();
        }

        private async Task SaveGroupsToFile(List<SiteGroup> groups)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            await System.IO.File.WriteAllTextAsync(_groupsFilePath, JsonSerializer.Serialize(groups, options));
        }

        private void CopyDirectory(string sourceDir, string destinationDir, List<string> ignoredFiles)
        {
            // Criar diretórios de destino, se não existirem
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            // Copiar todos os arquivos
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                
                // Verificar se o arquivo está na lista de ignorados
                if (ignoredFiles.Any(pattern => IsFileMatchingPattern(fileName, pattern)))
                {
                    _logger.LogInformation($"Arquivo ignorado: {fileName}");
                    continue;
                }

                var destFile = Path.Combine(destinationDir, fileName);
                System.IO.File.Copy(file, destFile, true);
            }

            // Copiar todos os subdiretórios e seus conteúdos
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(directory);
                
                // Verificar se o diretório está na lista de ignorados
                if (ignoredFiles.Any(pattern => IsFileMatchingPattern(dirName, pattern)))
                {
                    _logger.LogInformation($"Diretório ignorado: {dirName}");
                    continue;
                }

                var destDir = Path.Combine(destinationDir, dirName);
                CopyDirectory(directory, destDir, ignoredFiles);
            }
        }

        private bool IsFileMatchingPattern(string fileName, string pattern)
        {
            // Suporte para padrões simples com asterisco
            if (pattern.Contains("*"))
            {
                var prefix = pattern.Substring(0, pattern.IndexOf('*'));
                var suffix = pattern.Substring(pattern.IndexOf('*') + 1);

                return (string.IsNullOrEmpty(prefix) || fileName.StartsWith(prefix)) &&
                       (string.IsNullOrEmpty(suffix) || fileName.EndsWith(suffix));
            }

            // Correspondência exata
            return fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }
    }
} 