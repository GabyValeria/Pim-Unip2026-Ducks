namespace Ducks.Desktop.Core.Models
{
    public class AiPromptRequestDto
    {
        public string Contexto { get; set; } = string.Empty;
        public string TarefaAdmin { get; set; } = string.Empty;
    }

    public class AiResponseDto
    {
        public string ResultadoGerado { get; set; } = string.Empty;
        public bool Sucesso { get; set; }
        public string MensagemErro { get; set; } = string.Empty;
    }
}