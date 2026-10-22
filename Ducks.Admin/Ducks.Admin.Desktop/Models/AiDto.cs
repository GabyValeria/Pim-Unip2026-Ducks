namespace Ducks.Admin.Desktop.Models;

public record AiRequestDto(string Prompt, string Contexto);
public record AiResponseDto(string Resposta, string TokenUso);