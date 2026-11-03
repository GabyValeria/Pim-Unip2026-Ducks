using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ducks.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/settings")]
public class AdminSettingsController : ControllerBase
{
    private static Dictionary<string, string> _parametrosGlobais = new()
    {
        { "XP_RECOMPENSA_AULA", "100" },
        { "XP_BONUS_DESAFIO_DIARIO", "250" },
        { "PRAZO_MODERACAO_HORAS", "24" },
        { "RETENCAO_LOGS_DIAS", "90" }
    };

    [HttpGet]
    public IActionResult GetSettings()
    {
        return Ok(_parametrosGlobais);
    }

    [HttpPut]
    public IActionResult UpdateSetting([FromBody] UpdateSettingDto dto)
    {
        if (_parametrosGlobais.ContainsKey(dto.Chave))
        {
            _parametrosGlobais[dto.Chave] = dto.Valor;
            return Ok(new { sucesso = true, mensagem = "Parâmetro global atualizado." });
        }
        return NotFound(new { mensagem = "Chave de configuração não encontrada." });
    }
}

public class UpdateSettingDto
{
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
}