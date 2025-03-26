using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using IisManagerApi.Models;

namespace IisManagerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SiteGroupsController : ControllerBase
    {
        private readonly string _groupsFilePath;
        private readonly ILogger<SiteGroupsController> _logger;

        public SiteGroupsController(ILogger<SiteGroupsController> logger)
        {
            _logger = logger;
            _groupsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "groups.json");
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
                var groups = new List<SiteGroup>();
                if (System.IO.File.Exists(_groupsFilePath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(_groupsFilePath);
                    groups = JsonSerializer.Deserialize<List<SiteGroup>>(json) ?? new List<SiteGroup>();
                }

                if (groups.Any(g => g.Name == group.Name))
                {
                    return BadRequest("Já existe um grupo com este nome");
                }

                groups.Add(group);
                var options = new JsonSerializerOptions { WriteIndented = true };
                await System.IO.File.WriteAllTextAsync(_groupsFilePath, JsonSerializer.Serialize(groups, options));
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
                if (!System.IO.File.Exists(_groupsFilePath))
                {
                    return NotFound("Nenhum grupo encontrado");
                }

                var json = await System.IO.File.ReadAllTextAsync(_groupsFilePath);
                var groups = JsonSerializer.Deserialize<List<SiteGroup>>(json) ?? new List<SiteGroup>();
                var existingGroup = groups.FirstOrDefault(g => g.Name == name);

                if (existingGroup == null)
                {
                    return NotFound("Grupo não encontrado");
                }

                groups.Remove(existingGroup);
                groups.Add(group);

                var options = new JsonSerializerOptions { WriteIndented = true };
                await System.IO.File.WriteAllTextAsync(_groupsFilePath, JsonSerializer.Serialize(groups, options));
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
                if (!System.IO.File.Exists(_groupsFilePath))
                {
                    return NotFound("Nenhum grupo encontrado");
                }

                var json = await System.IO.File.ReadAllTextAsync(_groupsFilePath);
                var groups = JsonSerializer.Deserialize<List<SiteGroup>>(json) ?? new List<SiteGroup>();
                var group = groups.FirstOrDefault(g => g.Name == name);

                if (group == null)
                {
                    return NotFound("Grupo não encontrado");
                }

                groups.Remove(group);

                var options = new JsonSerializerOptions { WriteIndented = true };
                await System.IO.File.WriteAllTextAsync(_groupsFilePath, JsonSerializer.Serialize(groups, options));
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir grupo");
                return StatusCode(500, "Erro ao excluir grupo");
            }
        }
    }
} 